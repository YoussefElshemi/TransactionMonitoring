using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using AutoFixture;
using BehaviouralTests.TestHelpers;
using Core.Enums;
using Core.ValueObjects;
using FluentAssertions;
using Infrastructure;
using Infrastructure.Configurations;
using Infrastructure.Entities;
using Infrastructure.Mappers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Presentation.Models.AuditLogs;
using Presentation.Models.Common;
using Presentation.Models.Parties;
using Presentation.Models.PartyMappings;
using Presentation.Models.Rules;
using UnitTests.TestHelpers.FakeObjects.Core.Models.ApiClients;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Users;
using UnitTests.TestHelpers.FakeObjects.Infrastructure.Entities;

namespace BehaviouralTests.Tests.Controllers;

public class PartyRiskControllerTests(TestFixture testFixture) : IClassFixture<TestFixture>, IAsyncLifetime
{
    private readonly Fixture _fixture = AutoFixtureHelper.Create();
    private readonly HttpClient _httpClient = testFixture.CreateClient();
    private readonly HttpClient _ingestClient = testFixture.CreateClient();

    private const string Email = "username@mail.com";
    private const string Password = "P@$$w0rd";
    private static readonly Guid OrganizationId = new("4d6389e9-f5d1-4325-a004-915865e3adec");

    [Fact]
    public async Task SetPartyRisk_GivenAScoreAndANote_ShouldRecordBoth()
    {
        // Arrange
        await AuthenticateAsync(UserRole.Manager);
        await SetMappingAsync();
        await ArrangeIngestAsync();
        await _ingestClient.PostAsJsonAsync("/rule-sets/evaluate", Payload());

        var partyId = await ResolvePartyIdAsync("ACC-1");

        // Act
        var response = await _httpClient.PutAsJsonAsync($"/parties/{partyId}/risk", new SetPartyRiskRequestBodyDto
        {
            RiskScore = 85,
            Note = "Named in three SARs this quarter."
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var resource = await response.Content.ReadDataFromJsonAsync<PartyResourceDto>();
        resource!.Party.RiskScore.Should().Be(85);
        resource.Party.RiskNote.Should().Be("Named in three SARs this quarter.");
        resource.Party.RiskSetByUserId.Should().NotBeNull();
        resource.Party.RiskSetAt.Should().NotBeNull();
    }

    [Fact]
    public async Task SetPartyRisk_WithoutANote_ShouldReturnBadRequest()
    {
        // Arrange
        await AuthenticateAsync(UserRole.Manager);
        await SetMappingAsync();
        await ArrangeIngestAsync();
        await _ingestClient.PostAsJsonAsync("/rule-sets/evaluate", Payload());

        var partyId = await ResolvePartyIdAsync("ACC-1");

        // Act
        var response = await _httpClient.PutAsJsonAsync($"/parties/{partyId}/risk", new SetPartyRiskRequestBodyDto
        {
            RiskScore = 85,
            Note = "   "
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SetPartyRisk_GivenANullScore_ShouldClearItButKeepTheNote()
    {
        // Arrange
        await AuthenticateAsync(UserRole.Manager);
        await SetMappingAsync();
        await ArrangeIngestAsync();
        await _ingestClient.PostAsJsonAsync("/rule-sets/evaluate", Payload());

        var partyId = await ResolvePartyIdAsync("ACC-1");
        await _httpClient.PutAsJsonAsync($"/parties/{partyId}/risk", new SetPartyRiskRequestBodyDto { RiskScore = 85, Note = "Under review." });

        // Act
        var response = await _httpClient.PutAsJsonAsync($"/parties/{partyId}/risk", new SetPartyRiskRequestBodyDto
        {
            RiskScore = null,
            Note = "Review closed, nothing found."
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var resource = await response.Content.ReadDataFromJsonAsync<PartyResourceDto>();
        resource!.Party.RiskScore.Should().BeNull();
        resource.Party.RiskNote.Should().Be("Review closed, nothing found.");
    }

    [Fact]
    public async Task SetPartyRisk_AsUser_ShouldReturnForbidden()
    {
        // Arrange
        await AuthenticateAsync(UserRole.User);
        var partyId = Guid.NewGuid();

        // Act
        var response = await _httpClient.PutAsJsonAsync($"/parties/{partyId}/risk", new SetPartyRiskRequestBodyDto { RiskScore = 10, Note = "n" });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetPartyHistory_AfterTwoAssessments_ShouldCarryBothScoresAndNotes()
    {
        // Arrange
        await AuthenticateAsync(UserRole.Manager);
        await SetMappingAsync();
        await ArrangeIngestAsync();
        await _ingestClient.PostAsJsonAsync("/rule-sets/evaluate", Payload());

        var partyId = await ResolvePartyIdAsync("ACC-1");
        await _httpClient.PutAsJsonAsync($"/parties/{partyId}/risk", new SetPartyRiskRequestBodyDto { RiskScore = 85, Note = "Under review." });
        await _httpClient.PutAsJsonAsync($"/parties/{partyId}/risk", new SetPartyRiskRequestBodyDto { RiskScore = 20, Note = "Explained by payroll." });

        // Act
        var response = await _httpClient.GetAsync($"/parties/{partyId}/history");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var paged = await response.Content.ReadFromJsonAsync<PagedResponseDto<AuditLogResourceDto>>();
        paged!.Data.Should().NotBeEmpty();

        var changed = paged.Data.SelectMany(x => x.AuditLog.Changes).ToList();
        changed.Should().Contain(x => x.Field == "RiskNote" && x.NewValue!.ToString() == "Explained by payroll.");
        changed.Should().Contain(x => x.Field == "RiskScore" && x.NewValue!.ToString() == "20");
    }

    [Fact]
    public async Task Evaluate_GivenARuleOnPartyRisk_ShouldFireOnceThePartyIsAssessed()
    {
        // Arrange
        await AuthenticateAsync(UserRole.Manager);
        await SetMappingAsync();
        await ArrangeIngestAsync();

        await _ingestClient.PostAsJsonAsync("/rule-sets/evaluate", Payload());

        var partyId = await ResolvePartyIdAsync("ACC-1");
        await CreatePartyRiskRuleAsync();

        var beforeAssessment = await _ingestClient.PostAsJsonAsync("/rule-sets/evaluate", Payload());

        await _httpClient.PutAsJsonAsync($"/parties/{partyId}/risk", new SetPartyRiskRequestBodyDto { RiskScore = 90, Note = "High risk." });

        // Act
        var afterAssessment = await _ingestClient.PostAsJsonAsync("/rule-sets/evaluate", Payload());

        // Assert
        beforeAssessment.StatusCode.Should().Be(HttpStatusCode.OK);
        afterAssessment.StatusCode.Should().Be(HttpStatusCode.OK);

        var before = await beforeAssessment.Content.ReadDataFromJsonAsync<EvaluateAllRulesResourceDto>();
        var after = await afterAssessment.Content.ReadDataFromJsonAsync<EvaluateAllRulesResourceDto>();

        before!.Result.Results.Should().OnlyContain(x => !x.Matched);
        after!.Result.Results.Should().Contain(x => x.Matched);
    }

    [Fact]
    public async Task Evaluate_GivenARuleOnDaysSinceFirstSeen_ShouldFireOnTheIntroducingPayment()
    {
        // Arrange
        await AuthenticateAsync(UserRole.Manager);
        await SetMappingAsync();
        await ArrangeIngestAsync();
        await CreateNewPartyRuleAsync();

        // Act
        var response = await _ingestClient.PostAsJsonAsync("/rule-sets/evaluate", Payload());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadDataFromJsonAsync<EvaluateAllRulesResourceDto>();
        body!.Result.Results.Should().Contain(x => x.Matched);
    }

    [Fact]
    public async Task CreateRule_GivenARoleTheMappingDoesNotDeclare_ShouldReturnBadRequest()
    {
        // Arrange
        await AuthenticateAsync(UserRole.Manager);
        await SetMappingAsync();
        var ruleSetId = await ArrangeRuleSetAsync();

        // Act
        var response = await _httpClient.PostAsJsonAsync($"/rule-sets/{ruleSetId}/rules", new CreateRuleRequestBodyDto
        {
            FriendlyName = "Risky beneficiary",
            IsEnabled = true,
            Action = nameof(RuleAction.Flag),
            Severity = nameof(RuleSeverity.Medium),
            RuleBody = PartyRuleBody(nameof(PartyRole.Beneficiary), null)
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateRule_GivenAManyValuedRoleWithoutAQuantifier_ShouldReturnBadRequest()
    {
        // Arrange
        await AuthenticateAsync(UserRole.Manager);
        await SetMappingAsync();
        var ruleSetId = await ArrangeRuleSetAsync();

        // Act
        var response = await _httpClient.PostAsJsonAsync($"/rule-sets/{ruleSetId}/rules", new CreateRuleRequestBodyDto
        {
            FriendlyName = "Risky beneficiary",
            IsEnabled = true,
            Action = nameof(RuleAction.Flag),
            Severity = nameof(RuleSeverity.Medium),
            RuleBody = PartyRuleBody(nameof(PartyRole.Beneficiary), null)
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateRule_GivenAManyValuedRoleWithAQuantifier_ShouldReturnCreated()
    {
        // Arrange
        await AuthenticateAsync(UserRole.Manager);
        await SetMappingAsync();
        var ruleSetId = await ArrangeRuleSetAsync();

        // Act
        var response = await _httpClient.PostAsJsonAsync($"/rule-sets/{ruleSetId}/rules", new CreateRuleRequestBodyDto
        {
            FriendlyName = "Risky beneficiary",
            IsEnabled = true,
            Action = nameof(RuleAction.Flag),
            Severity = nameof(RuleSeverity.Medium),
            RuleBody = PartyRuleBody(nameof(PartyRole.Beneficiary), nameof(RuleQuantifier.Any))
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task DeletePartyMapping_WhileARuleReadsAParty_ShouldReturnConflict()
    {
        // Arrange
        await AuthenticateAsync(UserRole.Manager);
        await SetMappingAsync();
        await ArrangeIngestAsync();
        await CreatePartyRiskRuleAsync();

        // Act
        var response = await _httpClient.DeleteAsync("/organizations/my/party-mapping");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var stillInForce = await _httpClient.GetAsync("/organizations/my/party-mapping");
        stillInForce.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task DeletePartyMapping_WhenNoRuleReadsAParty_ShouldWithdraw()
    {
        // Arrange
        await AuthenticateAsync(UserRole.Manager);
        await SetMappingAsync();

        // Act
        var response = await _httpClient.DeleteAsync("/organizations/my/party-mapping");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task SetPartyMapping_DroppingARoleARuleReads_ShouldReturnConflict()
    {
        // Arrange
        await AuthenticateAsync(UserRole.Manager);
        await SetMappingAsync();
        await ArrangeIngestAsync();
        await CreatePartyRiskRuleAsync();

        var withoutRemitter = new SetPartyMappingRequestBodyDto
        {
            Mappings =
            [
                new PartyRoleMappingDto
                {
                    Role = nameof(PartyRole.Beneficiary),
                    Path = "parties[*]",
                    IdentifierPath = "iban"
                }
            ]
        };

        // Act
        var response = await _httpClient.PutAsJsonAsync("/organizations/my/party-mapping", withoutRemitter);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task ListPartyMappingUsages_ShouldNameTheRulesAndTheRolesTheyRead()
    {
        // Arrange
        await AuthenticateAsync(UserRole.Manager);
        await SetMappingAsync();
        await ArrangeIngestAsync();
        await CreatePartyRiskRuleAsync();

        // Act
        var response = await _httpClient.GetAsync("/organizations/my/party-mapping/usages");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var usages = await response.Content.ReadDataFromJsonAsync<PartyMappingUsageResponseDto[]>();
        usages.Should().ContainSingle();
        usages![0].FriendlyName.Should().Be("Risky remitter");
        usages[0].Roles.Should().Equal(nameof(PartyRole.Remitter));
    }

    private static RuleBodyDto PartyRuleBody(string role, string? quantifier, string attribute = nameof(PartyAttribute.RiskScore), int value = 70)
    {
        return new RuleBodyDto
        {
            Condition = nameof(RuleCondition.GreaterThan),
            Party = new PartyConditionDto { Role = role, Attribute = attribute },
            Quantifier = quantifier,
            Value = value
        };
    }

    private async Task CreatePartyRiskRuleAsync()
    {
        var ruleSetId = await ExistingRuleSetIdAsync();

        var response = await _httpClient.PostAsJsonAsync($"/rule-sets/{ruleSetId}/rules", new CreateRuleRequestBodyDto
        {
            FriendlyName = "Risky remitter",
            IsEnabled = true,
            Action = nameof(RuleAction.Flag),
            Severity = nameof(RuleSeverity.Medium),
            RuleBody = PartyRuleBody(nameof(PartyRole.Remitter), null)
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    private async Task CreateNewPartyRuleAsync()
    {
        var ruleSetId = await ExistingRuleSetIdAsync();

        var response = await _httpClient.PostAsJsonAsync($"/rule-sets/{ruleSetId}/rules", new CreateRuleRequestBodyDto
        {
            FriendlyName = "First-time remitter",
            IsEnabled = true,
            Action = nameof(RuleAction.Flag),
            Severity = nameof(RuleSeverity.Medium),
            RuleBody = new RuleBodyDto
            {
                Condition = nameof(RuleCondition.LessThan),
                Party = new PartyConditionDto
                {
                    Role = nameof(PartyRole.Remitter),
                    Attribute = nameof(PartyAttribute.DaysSinceFirstSeen)
                },
                Value = 1
            }
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    private async Task<Guid> ExistingRuleSetIdAsync()
    {
        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();

        var ruleSet = await context.RuleSets.FirstAsync(x => x.OrganizationId == OrganizationId);

        return ruleSet.RuleSetId;
    }

    private async Task<Guid> ArrangeRuleSetAsync()
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

        return ruleSetEntity.RuleSetId;
    }

    private async Task<Guid> ResolvePartyIdAsync(string identifier)
    {
        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();

        var party = await context.Parties.SingleAsync(x => x.Identifier == identifier);

        return party.PartyId;
    }

    private static JsonObject Payload()
    {
        return JsonNode.Parse(
            """
            {
              "amount": 100,
              "sender": { "accountId": "ACC-1", "name": "Ada", "country": "GB" },
              "parties": [ { "iban": "IBAN-1", "name": "Grace" } ]
            }
            """)!.AsObject();
    }

    private async Task SetMappingAsync()
    {
        var body = new SetPartyMappingRequestBodyDto
        {
            Mappings =
            [
                new PartyRoleMappingDto
                {
                    Role = nameof(PartyRole.Remitter),
                    Path = "sender",
                    IdentifierPath = "accountId",
                    DisplayNamePath = "name",
                    AttributePaths = [new PartyAttributeMappingDto { Name = "country", Path = "country" }]
                },
                new PartyRoleMappingDto
                {
                    Role = nameof(PartyRole.Beneficiary),
                    Path = "parties[*]",
                    IdentifierPath = "iban",
                    DisplayNamePath = "name"
                }
            ]
        };

        var response = await _httpClient.PutAsJsonAsync("/organizations/my/party-mapping", body);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private async Task ArrangeIngestAsync()
    {
        var applicationUserEntity = (FakeApplicationUser.CreateValid(_fixture) with
        {
            OrganizationId = new OrganizationId(OrganizationId),
            IsDeleted = new IsDeleted(false)
        }).ToEntity();

        await DatabaseHelper.CreateApplicationUsers(testFixture.Services, [applicationUserEntity]);

        await DatabaseHelper.CreateRuleSets(testFixture.Services, [
            FakeRuleSetEntity.CreateValid(_fixture) with
            {
                OrganizationId = OrganizationId,
                Organization = null!,
                IsEnabled = true,
                IsDeleted = false,
                Rules = []
            }
        ]);

        var createApiClientRequest = FakeCreateApiClientRequest.CreateValid(_fixture) with
        {
            OrganizationId = new OrganizationId(OrganizationId),
            UserId = new ApplicationUserId(applicationUserEntity.Id)
        };

        var createdApiClient = await AuthenticationHelper.CreateApiClientAsync(testFixture.Services, createApiClientRequest);
        await AuthenticationHelper.SetClientCredentialsTokenAsync(_ingestClient, createdApiClient.ApiClient.ClientId, createdApiClient.ClientSecret);
    }

    private async Task AuthenticateAsync(params UserRole[] roles)
    {
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User, .. roles], OrganizationId);
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
