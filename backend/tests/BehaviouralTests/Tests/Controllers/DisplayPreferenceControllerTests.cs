using System.Net;
using System.Net.Http.Json;
using AutoFixture;
using BehaviouralTests.TestHelpers;
using Core.Enums;
using FluentAssertions;
using Infrastructure.Configurations;
using Infrastructure.Entities;
using Presentation.Constants;
using Presentation.Models.Common;
using Presentation.Models.Localization;
using UnitTests.TestHelpers.FakeObjects.Infrastructure.Entities;

namespace BehaviouralTests.Tests.Controllers;

public class DisplayPreferenceControllerTests(TestFixture testFixture) : IClassFixture<TestFixture>, IAsyncLifetime
{
    private readonly Fixture _fixture = AutoFixtureHelper.Create();
    private readonly HttpClient _httpClient = testFixture.CreateClient();

    private const string Email = "analyst@mail.com";
    private const string Password = "P@$$w0rd";
    private static readonly Guid OrganizationId = new("6f5b7f2c-2f34-4a2f-9a1f-1c1f4c0b2a11");

    [Fact]
    public async Task GetPreferences_NotAuthenticated_ShouldReturnUnauthorized()
    {
        // Arrange
        // Act
        var response = await _httpClient.GetAsync("/users/me/preferences");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task GetPreferences_GivenNobodyHasStatedAnything_ResolvesToTheInstallationDefault()
    {
        // Arrange
        await AuthenticateAsync();

        // Act
        var response = await _httpClient.GetAsync("/users/me/preferences");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ResponseDataDto<DisplayPreferenceResponseDto>>();

        body!.Data.Locale.Should().BeNull();
        body.Data.TimeZone.Should().BeNull();
        body.Data.ResolvedLocale.Should().Be("en-GB");
        body.Data.ResolvedLanguage.Should().Be("en");
    }

    [Fact]
    public async Task UpdatePreferences_GivenALanguageTagTheHostDoesNotKnow_ShouldReturnBadRequest()
    {
        // Arrange
        await AuthenticateAsync();

        // Act
        var response = await _httpClient.PutAsJsonAsync(
            "/users/me/preferences", new { Locale = "not-a-language" });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task UpdatePreferences_GivenAZoneTheHostDoesNotKnow_ShouldReturnBadRequest()
    {
        // Arrange
        await AuthenticateAsync();

        // Act
        var response = await _httpClient.PutAsJsonAsync(
            "/users/me/preferences", new { TimeZone = "Mars/Olympus_Mons" });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdatePreferences_GivenALanguageAndAZone_StatesThemAndResolvesToThem()
    {
        // Arrange
        await AuthenticateAsync();

        // Act
        var response = await _httpClient.PutAsJsonAsync(
            "/users/me/preferences", new { Locale = "fr-BE", TimeZone = "Europe/Brussels" });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ResponseDataDto<DisplayPreferenceResponseDto>>();

        body!.Data.Locale.Should().Be("fr-BE");
        body.Data.TimeZone.Should().Be("Europe/Brussels");
        body.Data.ResolvedLocale.Should().Be("fr-BE");

        body.Data.ResolvedLanguage.Should().Be("fr");
    }

    [Fact]
    public async Task UpdatePreferences_GivenOnlyOneField_LeavesTheOtherAlone()
    {
        // Arrange
        await AuthenticateAsync();
        await _httpClient.PutAsJsonAsync("/users/me/preferences", new { Locale = "de-DE", TimeZone = "Europe/Berlin" });

        // Act
        var response = await _httpClient.PutAsJsonAsync("/users/me/preferences", new { TimeZone = "Asia/Tokyo" });

        // Assert
        var body = await response.Content.ReadFromJsonAsync<ResponseDataDto<DisplayPreferenceResponseDto>>();

        body!.Data.Locale.Should().Be("de-DE");
        body.Data.TimeZone.Should().Be("Asia/Tokyo");
    }

    [Fact]
    public async Task UpdatePreferences_GivenAnExplicitNull_ClearsTheStatedPreference()
    {
        // Arrange
        await AuthenticateAsync();
        await _httpClient.PutAsJsonAsync("/users/me/preferences", new { Locale = "de-DE" });

        // Act
        var response = await _httpClient.PutAsJsonAsync(
            "/users/me/preferences", new Dictionary<string, string?> { ["locale"] = null });

        // Assert
        var body = await response.Content.ReadFromJsonAsync<ResponseDataDto<DisplayPreferenceResponseDto>>();

        body!.Data.Locale.Should().BeNull();
        body.Data.ResolvedLocale.Should().Be("en-GB");
    }

    [Fact]
    public async Task GetCatalogue_ShouldPublishTheLanguagesTheProductHasTranslations()
    {
        // Arrange
        await AuthenticateAsync();

        // Act
        var response = await _httpClient.GetAsync("/localization/catalogue");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ResponseDataDto<LocalizationCatalogueResponseDto>>();

        body!.Data.Languages.Should().Contain(["en", "fr", "de", "es", "nl"]);
        body.Data.Locales.Should().Contain("fr-FR");
        body.Data.TimeZones.Should().Contain("Europe/London");
        body.Data.DefaultLocale.Should().Be("en-GB");
    }

    private async Task AuthenticateAsync()
    {
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User], OrganizationId);
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
            }
        };

        await DatabaseHelper.CreateOrganizations(testFixture.Services, organizationEntities);
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }
}
