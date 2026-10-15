using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AutoFixture;
using BehaviouralTests.TestHelpers;
using Core.Enums;
using FluentAssertions;
using Infrastructure.Configurations;
using Infrastructure.Entities;
using Presentation.Models.Common;
using Presentation.Models.Organizations;
using Presentation.Models.Rules;
using Presentation.Models.Users;
using UnitTests.TestHelpers.FakeObjects.Infrastructure.Entities;
using UnitTests.TestHelpers.FakeObjects.Presentation.Models.Rules;

namespace BehaviouralTests.Tests.Controllers;

public class ResponseEnrichmentTests(TestFixture testFixture) : IClassFixture<TestFixture>, IAsyncLifetime
{
    private readonly Fixture _fixture = AutoFixtureHelper.Create();
    private readonly HttpClient _httpClient = testFixture.CreateClient();

    private const string Email = "username@mail.com";
    private const string Password = "P@$$w0rd";
    private static readonly Guid OrganizationId = new("4d6389e9-f5d1-4325-a004-915865e3adec");
    private static readonly Guid OtherOrganizationId = new("03a9b48e-4af2-40bd-a180-cac58bd930f3");

    [Fact]
    public async Task GetUserInfo_ShouldCarryTheCallersOrganization()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User], OrganizationId);

        await AuthenticationHelper.SetCookieAsync(_httpClient, Email, Password);

        // Act
        var response = await _httpClient.GetAsync("/auth/manage/info");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var account = await response.Content.ReadDataFromJsonAsync<UserResourceDto>();
        account.Should().NotBeNull();
        account!.User.OrganizationId.Should().Be(OrganizationId);
        account.Organization.Should().NotBeNull();
        account.Organization!.OrganizationId.Should().Be(OrganizationId);
        account.Organization.Status.Should().Be(nameof(OrganizationStatus.Enabled));
    }

    [Fact]
    public async Task GetMyOrganization_AsPlainUser_ShouldReturnTheCallersOrganization()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);

        // Act
        var response = await _httpClient.GetAsync("/organizations/my");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var organization = await response.Content.ReadDataFromJsonAsync<OrganizationResponseDto>();
        organization.Should().NotBeNull();
        organization!.OrganizationId.Should().Be(OrganizationId);
    }

    [Fact]
    public async Task GetOrganization_AsPlainUser_ShouldReadTheirOwnAndNoOther()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);

        // Act
        var own = await _httpClient.GetAsync($"/organizations/{OrganizationId}");
        var other = await _httpClient.GetAsync($"/organizations/{Guid.NewGuid()}");

        // Assert
        own.StatusCode.Should().Be(HttpStatusCode.OK);
        other.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetRule_ShouldCarryItsRuleSetOrganizationAndAuthor()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User, UserRole.Manager], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);

        var ruleSetEntity = await CreateRuleSetAsync();
        var createdRule = await CreateRuleAsync(ruleSetEntity.RuleSetId);

        // Act
        var response = await _httpClient.GetAsync($"/rules/{createdRule.Rule.RuleId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var resource = await response.Content.ReadDataFromJsonAsync<RuleResourceDto>();
        resource.Should().NotBeNull();

        resource!.Rule.RuleSetId.Should().Be(ruleSetEntity.RuleSetId);
        resource.RuleSet.Should().NotBeNull();
        resource.RuleSet!.RuleSetId.Should().Be(ruleSetEntity.RuleSetId);

        resource.Organization.Should().NotBeNull();
        resource.Organization!.OrganizationId.Should().Be(OrganizationId);

        resource.CreatedByUser.Should().NotBeNull();
        resource.CreatedByUser!.UserId.Should().Be(resource.Rule.CreatedByUserId);
        resource.CreatedByUser.Email.Should().Be(Email);
    }

    [Fact]
    public async Task ListRules_ShouldEnrichEveryRowWithoutQueryingPerRow()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User, UserRole.Manager], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);

        var ruleSetEntity = await CreateRuleSetAsync();
        await CreateRuleAsync(ruleSetEntity.RuleSetId);

        testFixture.CommandCounter.Start();
        await _httpClient.GetAsync($"/rule-sets/{ruleSetEntity.RuleSetId}/rules");
        testFixture.CommandCounter.Stop();
        var singleRowCommands = testFixture.CommandCounter.Count;

        for (var i = 0; i < 9; i++)
        {
            await CreateRuleAsync(ruleSetEntity.RuleSetId);
        }

        // Act
        testFixture.CommandCounter.Start();
        var response = await _httpClient.GetAsync($"/rule-sets/{ruleSetEntity.RuleSetId}/rules");
        testFixture.CommandCounter.Stop();
        var tenRowCommands = testFixture.CommandCounter.Count;

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var page = await response.Content.ReadFromJsonAsync<PagedResponseDto<RuleResourceDto>>();
        page.Should().NotBeNull();
        page!.Data.Should().HaveCount(10);
        page.Data.Should().OnlyContain(x => x.RuleSet != null && x.Organization != null && x.CreatedByUser != null);

        // Guards the guard: if the counter stopped seeing requests both figures would be zero.
        singleRowCommands.Should().BePositive();

        tenRowCommands.Should().Be(singleRowCommands);
    }

    [Fact]
    public async Task GetRule_WhenTheAuthorBelongsToAnotherOrganization_ShouldOmitTheKeyRatherThanDisclose()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User, UserRole.Manager], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);

        var ruleSetEntity = await CreateRuleSetAsync();
        var createdRule = await CreateRuleAsync(ruleSetEntity.RuleSetId);

        var author = await AuthenticationHelper.CreateUserAsync(
            testFixture.Services, "author@mail.com", Password, [UserRole.User], OrganizationId);
        await DatabaseHelper.SetRuleAuthor(testFixture.Services, createdRule.Rule.RuleId, author.Id);
        await DatabaseHelper.MoveUserToOrganization(testFixture.Services, author.Id, OtherOrganizationId);

        // Act
        var response = await _httpClient.GetAsync($"/rules/{createdRule.Rule.RuleId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(body);
        var data = document.RootElement.GetProperty("data");

        data.TryGetProperty("createdByUser", out _).Should().BeFalse();

        data.GetProperty("rule").GetProperty("createdByUserId").GetGuid().Should().Be(author.Id);

        data.TryGetProperty("organization", out _).Should().BeTrue();
        data.TryGetProperty("ruleSet", out _).Should().BeTrue();
    }

    private async Task<RuleSetEntity> CreateRuleSetAsync()
    {
        var ruleSetEntity = FakeRuleSetEntity.CreateValid(_fixture) with
        {
            OrganizationId = OrganizationId,
            Organization = null!,
            IsEnabled = true,
            IsDeleted = false,
            Rules = []
        };

        await DatabaseHelper.CreateRuleSets(testFixture.Services, [ruleSetEntity]);

        return ruleSetEntity;
    }

    private async Task<RuleResourceDto> CreateRuleAsync(Guid ruleSetId)
    {
        var createRequest = FakeCreateRuleRequestDto.CreateValid(_fixture) with { RuleSetId = ruleSetId };

        var response = await _httpClient.PostAsJsonAsync($"/rule-sets/{ruleSetId}/rules", createRequest.Body);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        return (await response.Content.ReadDataFromJsonAsync<RuleResourceDto>())!;
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
