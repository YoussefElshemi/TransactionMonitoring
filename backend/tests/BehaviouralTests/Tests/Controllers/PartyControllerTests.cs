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
using Presentation.Constants;
using Presentation.Models.Common;
using Presentation.Models.Parties;
using Presentation.Models.PartyMappings;
using Presentation.Models.Rules;
using Presentation.Models.Transactions;
using UnitTests.TestHelpers.FakeObjects.Core.Models.ApiClients;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Users;
using UnitTests.TestHelpers.FakeObjects.Infrastructure.Entities;

namespace BehaviouralTests.Tests.Controllers;

public class PartyControllerTests(TestFixture testFixture) : IClassFixture<TestFixture>, IAsyncLifetime
{
    private readonly Fixture _fixture = AutoFixtureHelper.Create();
    private readonly HttpClient _httpClient = testFixture.CreateClient();
    private readonly HttpClient _ingestClient = testFixture.CreateClient();

    private const string Email = "username@mail.com";
    private const string Password = "P@$$w0rd";
    private static readonly Guid OrganizationId = new("4d6389e9-f5d1-4325-a004-915865e3adec");

    [Fact]
    public async Task ListParties_NotAuthenticated_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _httpClient.GetAsync("/parties");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task Evaluate_GivenAMapping_ShouldResolveEveryPartyTheMappingNames()
    {
        // Arrange
        await AuthenticateAsync(UserRole.Admin);
        await SetMappingAsync();
        await ArrangeIngestAsync();

        // Act
        var response = await _ingestClient.PostAsJsonAsync("/rule-sets/evaluate", Payload());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadDataFromJsonAsync<EvaluateAllRulesResourceDto>();

        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();

        var parties = await context.Parties.ToListAsync();
        parties.Select(x => x.Identifier).Should().BeEquivalentTo("ACC-1", "IBAN-1", "IBAN-2");

        var links = await context.TransactionParties.ToListAsync();
        links.Should().HaveCount(3);
        links.Should().OnlyContain(x => x.TransactionId == body!.Result.TransactionId);
        links.Count(x => x.PartyRoleId == (int)PartyRole.Remitter).Should().Be(1);
        links.Count(x => x.PartyRoleId == (int)PartyRole.Beneficiary).Should().Be(2);
    }

    [Fact]
    public async Task Evaluate_GivenNoMapping_ShouldResolveNobody()
    {
        // Arrange
        await ArrangeIngestAsync();

        // Act
        var response = await _ingestClient.PostAsJsonAsync("/rule-sets/evaluate", Payload());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();
        (await context.Parties.CountAsync()).Should().Be(0);
        (await context.TransactionParties.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Evaluate_RepeatedWithTheSameTransactionReference_ShouldNotDuplicateTheLinks()
    {
        // Arrange
        await AuthenticateAsync(UserRole.Admin);
        await SetMappingAsync();
        await ArrangeIngestAsync();
        _ingestClient.DefaultRequestHeaders.Add(HeaderNames.TransactionReference, "pay_9f3c21");

        // Act
        var first = await _ingestClient.PostAsJsonAsync("/rule-sets/evaluate", Payload());
        var second = await _ingestClient.PostAsJsonAsync("/rule-sets/evaluate", Payload());

        // Assert
        first.StatusCode.Should().Be(HttpStatusCode.OK);
        second.StatusCode.Should().Be(HttpStatusCode.OK);

        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();
        (await context.Transactions.CountAsync()).Should().Be(1);
        (await context.Parties.CountAsync()).Should().Be(3);
        (await context.TransactionParties.CountAsync()).Should().Be(3);
    }

    [Fact]
    public async Task Evaluate_GivenTheSameIdentifierOnTwoPayments_ShouldBeOnePartyWithTwoTransactions()
    {
        // Arrange
        await AuthenticateAsync(UserRole.Admin);
        await SetMappingAsync();
        await ArrangeIngestAsync();

        // Act
        await _ingestClient.PostAsJsonAsync("/rule-sets/evaluate", Payload());
        await _ingestClient.PostAsJsonAsync("/rule-sets/evaluate", Payload());

        // Assert
        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();

        (await context.Transactions.CountAsync()).Should().Be(2);

        var remitter = await context.Parties.SingleAsync(x => x.Identifier == "ACC-1");
        var links = await context.TransactionParties.Where(x => x.PartyId == remitter.PartyId).ToListAsync();
        links.Should().HaveCount(2);
        links.Select(x => x.TransactionId).Distinct().Should().HaveCount(2);

        var response = await _httpClient.GetAsync($"/parties/{remitter.PartyId}/transactions");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var paged = await response.Content.ReadFromJsonAsync<PagedResponseDto<TransactionSummaryResponseDto>>();
        paged!.Data.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetParty_GivenAResolvedParty_ShouldReturnItsIdentifierAndRoles()
    {
        // Arrange
        await AuthenticateAsync(UserRole.Admin);
        await SetMappingAsync();
        await ArrangeIngestAsync();
        await _ingestClient.PostAsJsonAsync("/rule-sets/evaluate", Payload());

        var partyId = await ResolvePartyIdAsync("ACC-1");

        // Act
        var response = await _httpClient.GetAsync($"/parties/{partyId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var resource = await response.Content.ReadDataFromJsonAsync<PartyResourceDto>();
        resource!.Party.Identifier.Should().Be("ACC-1");
        resource.Party.DisplayName.Should().Be("Ada");
        resource.Party.Roles.Should().Equal(nameof(PartyRole.Remitter));
    }

    [Fact]
    public async Task ListParties_ShouldReturnTheRolesEachPartyHasBeenSeenIn()
    {
        // Arrange
        await AuthenticateAsync(UserRole.Admin);
        await SetMappingAsync();
        await ArrangeIngestAsync();
        await _ingestClient.PostAsJsonAsync("/rule-sets/evaluate", Payload());

        // Act
        var response = await _httpClient.GetAsync("/parties?pageNumber=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var paged = await response.Content.ReadFromJsonAsync<PagedResponseDto<PartyResourceDto>>();
        paged!.Data.Should().HaveCount(3);
        paged.Data.Should().OnlyContain(x => x.Party.Roles.Length > 0);

        paged.Data.Single(x => x.Party.Identifier == "ACC-1").Party.Roles.Should().Equal(nameof(PartyRole.Remitter));
        paged.Data.Single(x => x.Party.Identifier == "IBAN-1").Party.Roles.Should().Equal(nameof(PartyRole.Beneficiary));
    }

    [Fact]
    public async Task ListParties_GivenAPartyInTwoRoles_ShouldReturnBoth()
    {
        // Arrange
        await AuthenticateAsync(UserRole.Admin);
        await SetMappingAsync();
        await ArrangeIngestAsync();

        var selfPayment = JsonNode.Parse(
            """
            {
              "amount": 100,
              "sender": { "accountId": "ACC-SELF", "name": "Ada" },
              "parties": [ { "iban": "ACC-SELF", "name": "Ada" } ]
            }
            """)!.AsObject();

        var evaluate = await _ingestClient.PostAsJsonAsync("/rule-sets/evaluate", selfPayment);
        evaluate.StatusCode.Should().Be(HttpStatusCode.OK, await evaluate.Content.ReadAsStringAsync());

        // Act
        var response = await _httpClient.GetAsync("/parties?identifier=ACC-SELF");

        // Assert
        var paged = await response.Content.ReadFromJsonAsync<PagedResponseDto<PartyResourceDto>>();
        paged!.Data.Should().ContainSingle();
        paged.Data[0].Party.Roles.Should().Equal(nameof(PartyRole.Remitter), nameof(PartyRole.Beneficiary));
    }

    [Fact]
    public async Task ListParties_GivenARoleFilter_ShouldReturnOnlyPartiesSeenInThatRole()
    {
        // Arrange
        await AuthenticateAsync(UserRole.Admin);
        await SetMappingAsync();
        await ArrangeIngestAsync();
        await _ingestClient.PostAsJsonAsync("/rule-sets/evaluate", Payload());

        // Act
        var response = await _httpClient.GetAsync($"/parties?role={nameof(PartyRole.Beneficiary)}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var paged = await response.Content.ReadFromJsonAsync<PagedResponseDto<PartyResourceDto>>();
        paged!.Data.Should().HaveCount(2);
        paged.Data.Select(x => x.Party.Identifier).Should().BeEquivalentTo("IBAN-1", "IBAN-2");
    }

    [Fact]
    public async Task ListTransactionParties_GivenAResolvedTransaction_ShouldReturnEveryRole()
    {
        // Arrange
        await AuthenticateAsync(UserRole.Admin);
        await SetMappingAsync();
        await ArrangeIngestAsync();

        var evaluateResponse = await _ingestClient.PostAsJsonAsync("/rule-sets/evaluate", Payload());
        var body = await evaluateResponse.Content.ReadDataFromJsonAsync<EvaluateAllRulesResourceDto>();

        // Act
        var response = await _httpClient.GetAsync($"/transactions/{body!.Result.TransactionId}/parties");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var parties = await response.Content.ReadDataFromJsonAsync<TransactionPartyResponseDto[]>();
        parties.Should().HaveCount(3);
        parties!.Count(x => x.Role == nameof(PartyRole.Beneficiary)).Should().Be(2);
        parties.Should().Contain(x => x.Role == nameof(PartyRole.Remitter) && x.Identifier == "ACC-1");
    }

    [Fact]
    public async Task Evaluate_AfterTheMappingIsWithdrawn_ShouldStopResolvingButKeepWhatWasResolved()
    {
        // Arrange
        await AuthenticateAsync(UserRole.Admin);
        await SetMappingAsync();
        await ArrangeIngestAsync();
        await _ingestClient.PostAsJsonAsync("/rule-sets/evaluate", Payload());

        await _httpClient.DeleteAsync($"/organizations/{OrganizationId}/party-mapping");

        // Act
        var response = await _ingestClient.PostAsJsonAsync("/rule-sets/evaluate", Payload());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();
        (await context.Transactions.CountAsync()).Should().Be(2);
        (await context.Parties.CountAsync()).Should().Be(3);
        (await context.TransactionParties.CountAsync()).Should().Be(3);
    }

    [Fact]
    public async Task GetParty_GivenAPartyOfAnotherOrganization_ShouldReturnNotFound()
    {
        // Arrange
        await AuthenticateAsync(UserRole.Admin);

        // Act
        var response = await _httpClient.GetAsync($"/parties/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
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
              "sender": { "accountId": "ACC-1", "name": "Ada" },
              "parties": [ { "iban": "IBAN-1", "name": "Grace" }, { "iban": "IBAN-2" } ]
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
                    DisplayNamePath = "name"
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

        var response = await _httpClient.PutAsJsonAsync($"/organizations/{OrganizationId}/party-mapping", body);
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
                Rules =
                [
                    FakeRuleEntity.CreateValid(_fixture) with
                    {
                        OrganizationId = OrganizationId,
                        Organization = null!,
                        IsEnabled = true,
                        IsDeleted = false
                    }
                ]
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
