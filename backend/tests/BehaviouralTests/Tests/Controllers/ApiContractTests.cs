using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AutoFixture;
using BehaviouralTests.TestHelpers;
using Core.Constants;
using Core.Enums;
using FluentAssertions;
using Infrastructure;
using Infrastructure.Configurations;
using Infrastructure.Entities;
using Microsoft.Extensions.DependencyInjection;
using Presentation.Constants;
using Presentation.Models.ApiClients;
using Presentation.Models.ApiVersions;
using Presentation.Models.Cases;
using Presentation.Models.Common;
using Presentation.Models.Errors;
using Presentation.Models.Parties;
using Presentation.Models.Search;
using UnitTests.TestHelpers.FakeObjects.Infrastructure.Entities;
using UnitTests.TestHelpers.FakeObjects.Presentation.Models.ApiClients;

namespace BehaviouralTests.Tests.Controllers;

public class ApiContractTests(TestFixture testFixture) : IClassFixture<TestFixture>, IAsyncLifetime
{
    private readonly Fixture _fixture = AutoFixtureHelper.Create();
    private readonly HttpClient _httpClient = testFixture.CreateClient();

    private const string Email = "username@mail.com";
    private const string Password = "P@$$w0rd";
    private static readonly Guid OrganizationId = new("4d6389e9-f5d1-4325-a004-915865e3adec");
    private static readonly Guid OtherOrganizationId = new("0b0f6a52-6f0f-4f0a-8a6e-0c4a4c1c2f11");

    [Fact]
    public async Task AnyError_CarriesACodeAndTheCorrelationIdThatIsAlsoInTheHeader()
    {
        // Arrange
        await AuthenticateAsManagerAsync();

        // Act
        var response = await _httpClient.GetAsync($"/parties/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("code").GetString().Should().Be(ErrorCodes.NotFound);

        var correlationId = response.Headers.GetValues(HeaderNames.CorrelationId).Single();
        Guid.TryParse(correlationId, out _).Should().BeTrue();
        body.GetProperty("correlationId").GetString().Should().Be(correlationId);
    }

    [Fact]
    public async Task AValidationError_CarriesTheValidationCode()
    {
        // Arrange
        await AuthenticateAsManagerAsync();

        // Act
        var response = await _httpClient.GetAsync("/search?query=ab");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("code").GetString().Should().Be(ErrorCodes.ValidationFailed);
        body.GetProperty("errors").TryGetProperty("Query", out _).Should().BeTrue();
    }

    [Fact]
    public async Task ADomainError_CarriesItsOwnCode()
    {
        // Arrange
        await AuthenticateAsManagerAsync();

        // Act
        var response = await _httpClient.PostAsJsonAsync($"/organizations/{OrganizationId}/sandbox/reset", new { });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("code").GetString().Should().Be("not_available_in_live_mode");
    }

    [Fact]
    public async Task ListErrors_Anonymous_ShouldPublishEveryCodeWithItsStatus()
    {
        // Act
        var response = await _httpClient.GetAsync("/errors");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var errors = await response.Content.ReadDataFromJsonAsync<ErrorDefinitionResponseDto[]>();
        errors.Should().HaveCount(ErrorCatalogue.Errors.Count);
        errors.Should().ContainSingle(x => x.Code == "invoice_issued").Which.Status.Should().Be(409);
        errors.Should().ContainSingle(x => x.Code == ErrorCodes.RateLimited).Which.Status.Should().Be(429);
    }

    [Fact]
    public async Task GetApiVersions_Anonymous_ShouldPublishTheVersionsAndThePolicy()
    {
        // Act
        var response = await _httpClient.GetAsync("/api-versions");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var catalogue = await response.Content.ReadDataFromJsonAsync<ApiVersionCatalogueResponseDto>();
        catalogue!.Current.Should().Be(ApiVersions.Current.ToString());
        catalogue.MinimumNoticeMonths.Should().Be(ApiVersions.MinimumNoticeMonths);
        catalogue.BreakingChanges.Should().NotBeEmpty();
        catalogue.NonBreakingChanges.Should().NotBeEmpty();
        catalogue.Versions.Should().ContainSingle(x => x.Version == ApiVersions.Current.ToString())
            .Which.Status.Should().Be("Current");
    }

    [Fact]
    public async Task AnyResponse_SaysWhichVersionItWasServedUnder()
    {
        // Arrange
        await AuthenticateAsManagerAsync();

        // Act
        var response = await _httpClient.GetAsync("/parties");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.GetValues(HeaderNames.ApiVersion).Should().ContainSingle().Which.Should().Be(ApiVersions.Current.ToString());
        response.Headers.Contains(HeaderNames.Deprecation).Should().BeFalse();
        response.Headers.Contains(HeaderNames.Sunset).Should().BeFalse();
    }

    [Fact]
    public async Task ARequestNamingAVersionThatIsNotServed_ShouldBeRefusedRatherThanDefaulted()
    {
        // Arrange
        await AuthenticateAsManagerAsync();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/parties");
        request.Headers.Add(HeaderNames.ApiVersion, "1999-01-01");

        // Act
        var response = await _httpClient.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("code").GetString().Should().Be(ErrorCodes.UnsupportedApiVersion);
        body.GetProperty("servedVersions").EnumerateArray().Select(x => x.GetString())
            .Should().Contain(ApiVersions.Current.ToString());
    }

    [Fact]
    public async Task CreateApiClient_ShouldPinTheClientToTheCurrentVersion()
    {
        // Arrange
        await AuthenticateAsManagerAsync();

        // Act
        var response = await _httpClient.PostAsJsonAsync("/api-clients", FakeCreateApiClientRequestDto.CreateValid(_fixture));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var created = await response.Content.ReadDataFromJsonAsync<CreatedApiClientResourceDto>();
        created!.ApiClient.ApiVersion.Should().Be(ApiVersions.Current.ToString());
    }

    [Fact]
    public async Task UpdateApiClient_GivenAVersionThatIsNotServed_ShouldReturnBadRequestAndLeaveThePinAlone()
    {
        // Arrange
        await AuthenticateAsManagerAsync();
        var created = await CreateApiClientAsync();

        // Act
        var response = await _httpClient.PatchAsJsonAsync(
            $"/api-clients/{created.ApiClient.ApiClientId}", new { ApiVersion = "1999-01-01" });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var apiClient = await GetApiClientAsync(created.ApiClient.ApiClientId);
        apiClient.ApiClient.ApiVersion.Should().Be(ApiVersions.Current.ToString());
    }

    [Fact]
    public async Task UpdateApiClient_GivenAServedVersion_ShouldMoveThePinAndNothingElse()
    {
        // Arrange
        await AuthenticateAsManagerAsync();
        var created = await CreateApiClientAsync();

        // Act
        var response = await _httpClient.PatchAsJsonAsync(
            $"/api-clients/{created.ApiClient.ApiClientId}", new { ApiVersion = ApiVersions.V20261014 });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var apiClient = await GetApiClientAsync(created.ApiClient.ApiClientId);
        apiClient.ApiClient.ApiVersion.Should().Be(ApiVersions.V20261014);
        apiClient.ApiClient.FriendlyName.Should().Be(created.ApiClient.FriendlyName);
        apiClient.ApiClient.Scopes.Should().BeEquivalentTo(created.ApiClient.Scopes);
    }

    [Fact]
    public async Task Search_NotAuthenticated_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _httpClient.GetAsync("/search?query=acme");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Search_GivenAFragmentOfAName_ShouldFindThePartyAndTheCasesAboutIt()
    {
        // Arrange
        await AuthenticateAsManagerAsync();
        var acme = await SeedPartyAsync("IBAN-GB29-0001", "Acme Trading Ltd");
        await SeedPartyAsync("IBAN-GB29-0002", "Borealis Holdings");
        var aboutAcme = await SeedCaseAsync("CA0000000101", acme);
        await SeedCaseAsync("CA0000000102", null);

        // Act
        var response = await _httpClient.GetAsync("/search?query=acme");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var results = await response.Content.ReadDataFromJsonAsync<SearchResponseDto>();
        results!.Query.Should().Be("acme");
        results.Parties!.TotalCount.Should().Be(1);
        results.Parties.Data.Should().ContainSingle().Which.Party.PartyId.Should().Be(acme);
        results.Cases!.TotalCount.Should().Be(1);
        results.Cases.Data.Should().ContainSingle().Which.Case.CaseId.Should().Be(aboutAcme);
    }

    [Fact]
    public async Task Search_GivenAFragmentOfAnIdentifierOrACaseReference_ShouldMatchEitherColumn()
    {
        // Arrange
        await AuthenticateAsManagerAsync();
        var party = await SeedPartyAsync("ACC-778812", "Somebody Else");
        var @case = await SeedCaseAsync("CA0000778899", null);

        // Act
        var response = await _httpClient.GetAsync("/search?query=0778");
        var byIdentifier = await _httpClient.GetAsync("/search?query=7788");

        // Assert
        var results = await response.Content.ReadDataFromJsonAsync<SearchResponseDto>();
        results!.Parties!.Data.Should().BeEmpty();
        results.Cases!.Data.Should().ContainSingle().Which.Case.CaseId.Should().Be(@case);

        var both = await byIdentifier.Content.ReadDataFromJsonAsync<SearchResponseDto>();
        both!.Parties!.Data.Should().ContainSingle().Which.Party.PartyId.Should().Be(party);
        both.Cases!.Data.Select(x => x.Case.CaseId).Should().Contain(@case);
    }

    [Fact]
    public async Task Search_GivenPatternCharacters_ShouldTakeThemLiterally()
    {
        // Arrange
        await AuthenticateAsManagerAsync();
        var literal = await SeedPartyAsync("PCT-1", "50%_off Imports");
        await SeedPartyAsync("PCT-2", "500 Zoff Imports");

        // Act
        var response = await _httpClient.GetAsync("/search?query=" + Uri.EscapeDataString("50%_off"));

        // Assert
        var results = await response.Content.ReadDataFromJsonAsync<SearchResponseDto>();
        results!.Parties!.Data.Should().ContainSingle().Which.Party.PartyId.Should().Be(literal);
    }

    [Fact]
    public async Task Search_ShouldNotReachAnotherOrganisationsRecords()
    {
        // Arrange
        await AuthenticateAsManagerAsync();
        await SeedPartyAsync("IBAN-OTHER-1", "Acme Trading Ltd", OtherOrganizationId);
        await SeedCaseAsync("CA0000000900", null, OtherOrganizationId);

        // Act
        var response = await _httpClient.GetAsync("/search?query=acme");
        var byReference = await _httpClient.GetAsync("/search?query=CA0000000900");

        // Assert
        var results = await response.Content.ReadDataFromJsonAsync<SearchResponseDto>();
        results!.Parties!.Data.Should().BeEmpty();

        var cases = await byReference.Content.ReadDataFromJsonAsync<SearchResponseDto>();
        cases!.Cases!.Data.Should().BeEmpty();
    }

    [Fact]
    public async Task Search_GivenOneType_ShouldLeaveTheOtherSectionOut()
    {
        // Arrange
        await AuthenticateAsManagerAsync();
        await SeedPartyAsync("IBAN-GB29-0001", "Acme Trading Ltd");

        // Act
        var response = await _httpClient.GetAsync("/search?query=acme&types=Party");

        // Assert
        var data = await response.Content.ReadDataFromJsonAsync<JsonElement>();
        data.GetProperty("parties").GetProperty("totalCount").GetInt32().Should().Be(1);
        data.GetProperty("cases").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public async Task Search_GivenMoreMatchesThanTheLimit_ShouldSayThereAreMore()
    {
        // Arrange
        await AuthenticateAsManagerAsync();

        for (var i = 0; i < 4; i++)
        {
            await SeedPartyAsync($"IBAN-LIMIT-{i}", $"Meridian Freight {i}");
        }

        // Act
        var response = await _httpClient.GetAsync("/search?query=meridian&limit=3");

        // Assert
        var results = await response.Content.ReadDataFromJsonAsync<SearchResponseDto>();
        results!.Parties!.Data.Should().HaveCount(3);
        results.Parties.TotalCount.Should().Be(4);
        results.Parties.HasMore.Should().BeTrue();
    }

    [Fact]
    public async Task ListParties_GivenAQuery_ShouldMatchTheIdentifierOrTheName()
    {
        // Arrange
        await AuthenticateAsManagerAsync();
        var byName = await SeedPartyAsync("IBAN-1", "Northwind Supplies");
        var byIdentifier = await SeedPartyAsync("NORTHWIND-ACC-9", "A Different Name");
        await SeedPartyAsync("IBAN-3", "Unrelated");

        // Act
        var response = await _httpClient.GetAsync("/parties?query=northwind");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var parties = await response.Content.ReadFromJsonAsync<PagedResponseDto<PartyResourceDto>>();
        parties!.Data.Select(x => x.Party.PartyId).Should().BeEquivalentTo([byName, byIdentifier]);
    }

    [Fact]
    public async Task ListCases_GivenAQuery_ShouldFindCasesByTheNameOfThePartyTheyAreAbout()
    {
        // Arrange
        await AuthenticateAsManagerAsync();
        var party = await SeedPartyAsync("IBAN-1", "Northwind Supplies");
        var first = await SeedCaseAsync("CA0000000201", party);
        var second = await SeedCaseAsync("CA0000000202", party);
        await SeedCaseAsync("CA0000000203", null);

        // Act
        var response = await _httpClient.GetAsync("/cases?query=northwind");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var cases = await response.Content.ReadFromJsonAsync<PagedResponseDto<CaseResourceDto>>();
        cases!.Data.Select(x => x.Case.CaseId).Should().BeEquivalentTo([first, second]);
    }

    private async Task<Guid> SeedPartyAsync(string identifier, string displayName, Guid? organizationId = null)
    {
        var partyEntity = FakePartyEntity.CreateValid(_fixture) with
        {
            OrganizationId = organizationId ?? OrganizationId,
            Organization = null!,
            Identifier = identifier,
            DisplayName = displayName,
            Attributes = string.Empty
        };

        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();

        await context.Parties.AddAsync(partyEntity);
        await context.SaveChangesAsync();

        return partyEntity.PartyId;
    }

    private async Task<Guid> SeedCaseAsync(string friendlyName, Guid? partyId, Guid? organizationId = null)
    {
        var caseEntity = FakeCaseEntity.CreateValid(_fixture) with
        {
            OrganizationId = organizationId ?? OrganizationId,
            Organization = null!,
            FriendlyName = friendlyName,
            PartyId = partyId,
            IsDeleted = false
        };

        await DatabaseHelper.CreateCases(testFixture.Services, [caseEntity]);

        return caseEntity.CaseId;
    }

    private async Task<CreatedApiClientResourceDto> CreateApiClientAsync()
    {
        var response = await _httpClient.PostAsJsonAsync("/api-clients", FakeCreateApiClientRequestDto.CreateValid(_fixture));

        return (await response.Content.ReadDataFromJsonAsync<CreatedApiClientResourceDto>())!;
    }

    private async Task<ApiClientResourceDto> GetApiClientAsync(Guid apiClientId)
    {
        var response = await _httpClient.GetAsync($"/api-clients/{apiClientId}");

        return (await response.Content.ReadDataFromJsonAsync<ApiClientResourceDto>())!;
    }

    private async Task AuthenticateAsManagerAsync()
    {
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User, UserRole.Manager], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);
    }

    public async Task InitializeAsync()
    {
        await testFixture.ResetDatabaseAsync();

        var organizationEntities = new List<OrganizationEntity>
        {
            OrganizationConfigurations.DefaultOrganization,
            FakeOrganizationEntity.CreateValid(_fixture) with
            {
                OrganizationId = OrganizationId,
                StatusId = (int)OrganizationStatus.Enabled,
                IsDeleted = false
            },
            FakeOrganizationEntity.CreateValid(_fixture) with
            {
                OrganizationId = OtherOrganizationId,
                StatusId = (int)OrganizationStatus.Enabled,
                IsDeleted = false
            }
        };

        await DatabaseHelper.CreateOrganizations(testFixture.Services, organizationEntities);
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }
}
