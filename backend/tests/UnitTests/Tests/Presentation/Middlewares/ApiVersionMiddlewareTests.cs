using System.Diagnostics.Metrics;
using System.Security.Claims;
using System.Text.Json;
using Core.Models.ApiVersions;
using Core.Services;
using Core.ValueObjects;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Presentation.Constants;
using Presentation.Middlewares;
using Presentation.Telemetry;
using UnitTests.TestHelpers;

namespace UnitTests.Tests.Presentation.Middlewares;

public class ApiVersionMiddlewareTests : BaseTestClass, IDisposable
{
    private const string Deprecated = "2026-10-14";
    private const string Current = "2027-09-01";

    private readonly ApiVersionUsageMeter _meter = new();
    private readonly ApiVersionService _apiVersionService = new(
    [
        Define(Deprecated, deprecatedOn: new DateOnly(2027, 9, 1), sunsetOn: new DateOnly(2028, 9, 1)),
        Define(Current)
    ]);

    private bool _nextWasCalled;

    public void Dispose()
    {
        _meter.Dispose();
    }

    [Fact]
    public async Task InvokeAsync_GivenAPerson_ServesTheCurrentVersionAndSaysSo()
    {
        // Arrange
        var (context, start) = CreateContext(UserPrincipal());

        // Act
        await CreateSut().InvokeAsync(context);
        await start();

        // Assert
        _nextWasCalled.Should().BeTrue();
        context.Response.Headers[HeaderNames.ApiVersion].ToString().Should().Be(Current);
        context.Response.Headers.Should().NotContainKey(HeaderNames.Deprecation);
        context.Response.Headers.Should().NotContainKey(HeaderNames.Sunset);
    }

    [Fact]
    public async Task InvokeAsync_GivenAClientPinnedToADeprecatedVersion_ServesItAndCarriesTheNotice()
    {
        // Arrange
        var (context, start) = CreateContext(ApiClientPrincipal(Deprecated));

        // Act
        await CreateSut().InvokeAsync(context);
        await start();

        // Assert
        context.Response.Headers[HeaderNames.ApiVersion].ToString().Should().Be(Deprecated);
        context.Response.Headers[HeaderNames.Deprecation].ToString().Should().Be("@1819756800");
        context.Response.Headers[HeaderNames.Sunset].ToString().Should().Be("Fri, 01 Sep 2028 00:00:00 GMT");
        context.Response.Headers[HeaderNames.Link].ToString().Should().Be("</api-versions>; rel=\"deprecation\"; type=\"application/json\"");
    }

    [Fact]
    public async Task InvokeAsync_GivenAClientPinnedToTheCurrentVersion_CarriesNoNotice()
    {
        // Arrange
        var (context, start) = CreateContext(ApiClientPrincipal(Current));

        // Act
        await CreateSut().InvokeAsync(context);
        await start();

        // Assert
        context.Response.Headers[HeaderNames.ApiVersion].ToString().Should().Be(Current);
        context.Response.Headers.Should().NotContainKey(HeaderNames.Deprecation);
    }

    [Fact]
    public async Task InvokeAsync_GivenATokenIssuedBeforeClientsWerePinned_ServesTheEarliestVersion()
    {
        // Arrange
        var (context, start) = CreateContext(ApiClientPrincipal(null));

        // Act
        await CreateSut().InvokeAsync(context);
        await start();

        // Assert
        context.Response.Headers[HeaderNames.ApiVersion].ToString().Should().Be(Deprecated);
    }

    [Fact]
    public async Task InvokeAsync_GivenAPinThatHasBeenRetired_ServesTheEarliestAndSaysWhichItServed()
    {
        // Arrange
        var (context, start) = CreateContext(ApiClientPrincipal("2025-01-01"));

        // Act
        await CreateSut().InvokeAsync(context);
        await start();

        // Assert
        _nextWasCalled.Should().BeTrue();
        context.Response.Headers[HeaderNames.ApiVersion].ToString().Should().Be(Deprecated);
    }

    [Fact]
    public async Task InvokeAsync_GivenAVersionHeader_ServesThatRatherThanThePin()
    {
        // Arrange
        var (context, start) = CreateContext(ApiClientPrincipal(Deprecated));
        context.Request.Headers[HeaderNames.ApiVersion] = Current;

        // Act
        await CreateSut().InvokeAsync(context);
        await start();

        // Assert
        context.Response.Headers[HeaderNames.ApiVersion].ToString().Should().Be(Current);
        context.Response.Headers.Should().NotContainKey(HeaderNames.Deprecation);
    }

    [Theory]
    [InlineData("2025-01-01")]
    [InlineData("v2")]
    [InlineData("latest")]
    public async Task InvokeAsync_GivenAVersionHeaderThatIsNotServed_RefusesRatherThanDefaulting(string requested)
    {
        // Arrange
        var (context, _) = CreateContext(ApiClientPrincipal(Current));
        context.Request.Headers[HeaderNames.ApiVersion] = requested;
        context.Response.Body = new MemoryStream();

        // Act
        await CreateSut().InvokeAsync(context);

        // Assert
        _nextWasCalled.Should().BeFalse();
        context.Response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        using var document = await JsonDocument.ParseAsync(context.Response.Body);
        document.RootElement.GetProperty("code").GetString().Should().Be("unsupported_api_version");
        document.RootElement.GetProperty("servedVersions").EnumerateArray().Select(x => x.GetString())
            .Should().BeEquivalentTo(Deprecated, Current);
    }

    [Fact]
    public async Task InvokeAsync_GivenAnAuthenticatedRequest_CountsItAgainstTheVersionItWasServedUnder()
    {
        // Arrange
        var organizationId = Guid.NewGuid();
        var (context, _) = CreateContext(ApiClientPrincipal(Deprecated, organizationId));
        var recorded = new List<Dictionary<string, object?>>();

        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, meterListener) =>
        {
            if (instrument.Name == ApiVersionUsageMeter.InstrumentName)
            {
                meterListener.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<long>((_, _, tags, _) =>
        {
            var measurement = tags.ToArray().ToDictionary(x => x.Key, x => x.Value);

            // The meter name is shared across the run, so keep only this request's measurement.
            if (Equals(measurement.GetValueOrDefault("organization.id"), organizationId.ToString()))
            {
                recorded.Add(measurement);
            }
        });
        listener.Start();

        // Act
        await CreateSut().InvokeAsync(context);

        // Assert
        recorded.Should().ContainSingle();
        recorded[0]["api.version"].Should().Be(Deprecated);
        recorded[0]["api.version.source"].Should().Be("pin");
        recorded[0]["api.version.deprecated"].Should().Be(true);
        recorded[0]["api.caller"].Should().Be("api_client");
    }

    private ApiVersionMiddleware CreateSut()
    {
        return new ApiVersionMiddleware(
            _ =>
            {
                _nextWasCalled = true;

                return Task.CompletedTask;
            },
            _apiVersionService,
            _meter);
    }

    // DefaultHttpContext drops OnStarting callbacks, so this feature keeps and fires them.
    private static (DefaultHttpContext Context, Func<Task> Start) CreateContext(ClaimsPrincipal principal)
    {
        var responseFeature = new StartableResponseFeature();
        var features = new FeatureCollection();
        features.Set<IHttpRequestFeature>(new HttpRequestFeature());
        features.Set<IHttpResponseFeature>(responseFeature);
        features.Set<IHttpResponseBodyFeature>(new StreamResponseBodyFeature(new MemoryStream()));

        var context = new DefaultHttpContext(features) { User = principal };

        return (context, responseFeature.StartAsync);
    }

    private static ClaimsPrincipal UserPrincipal()
    {
        return new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())], "Test"));
    }

    private static ClaimsPrincipal ApiClientPrincipal(string? apiVersion, Guid? organizationId = null)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new(AuthConstants.ClientIdClaim, "tmc_test"),
            new(AuthConstants.OrganizationIdClaim, (organizationId ?? Guid.NewGuid()).ToString())
        };

        if (apiVersion != null)
        {
            claims.Add(new Claim(AuthConstants.ApiVersionClaim, apiVersion));
        }

        return new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
    }

    private static ApiVersionDefinition Define(string version, DateOnly? deprecatedOn = null, DateOnly? sunsetOn = null)
    {
        return new ApiVersionDefinition
        {
            Version = new ApiVersion(version),
            ReleasedOn = new ReleasedOn(DateOnly.ParseExact(version, ApiVersion.Format)),
            DeprecatedOn = deprecatedOn.HasValue ? new DeprecatedOn(deprecatedOn.Value) : null,
            SunsetOn = sunsetOn.HasValue ? new SunsetOn(sunsetOn.Value) : null,
            Summary = new ApiVersionSummary("A version declared for a test.")
        };
    }

    private sealed class StartableResponseFeature : HttpResponseFeature
    {
        private readonly List<(Func<object, Task> Callback, object State)> _callbacks = [];

        public override void OnStarting(Func<object, Task> callback, object state)
        {
            _callbacks.Add((callback, state));
        }

        public async Task StartAsync()
        {
            foreach (var (callback, state) in Enumerable.Reverse(_callbacks))
            {
                await callback(state);
            }
        }
    }
}
