using System.Net;
using System.Net.Http.Json;
using AutoFixture;
using BehaviouralTests.TestHelpers;
using Core.Enums;
using FluentAssertions;
using Infrastructure.Configurations;
using Presentation.Models.Common;
using Presentation.Models.Invoices;
using Presentation.Models.OrganizationInvoices;
using Presentation.Models.Plans;
using Presentation.Models.Subscriptions;
using UnitTests.TestHelpers.FakeObjects.Infrastructure.Entities;

namespace BehaviouralTests.Tests.Controllers;

public class PlanAndSubscriptionControllerTests(TestFixture testFixture) : IClassFixture<TestFixture>, IAsyncLifetime
{
    private readonly Fixture _fixture = AutoFixtureHelper.Create();
    private readonly HttpClient _httpClient = testFixture.CreateClient();

    private const string AdminEmail = "platform@mail.com";
    private const string ManagerEmail = "desk@mail.com";
    private const string Password = "P@$$w0rd";
    private static readonly Guid OrganizationId = new("b6f0a2d1-4c8e-4a37-9f52-70d3c1a9b8e5");

    public async Task InitializeAsync()
    {
        await testFixture.ResetDatabaseAsync();

        await DatabaseHelper.CreateOrganizations(testFixture.Services,
        [
            OrganizationConfigurations.DefaultOrganization,
            FakeOrganizationEntity.CreateValid(_fixture) with
            {
                OrganizationId = OrganizationId,
                StatusId = (int)OrganizationStatus.Enabled,
                IsDeleted = false
            }
        ]);

        await AuthenticationHelper.CreateUserAsync(testFixture.Services, AdminEmail, Password, [UserRole.Admin], OrganizationId);
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, ManagerEmail, Password, [UserRole.Manager], OrganizationId);

        await AuthenticationHelper.SetTokenAsync(_httpClient, AdminEmail, Password);

        await DatabaseHelper.CreateBillingProfile(testFixture.Services, OrganizationId, customerCountryCode: "US");
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    [Fact]
    public async Task CreatePlan_GivenAValidRequest_ReturnsCreatedWithVersionOne()
    {
        // Act
        var response = await _httpClient.PostAsJsonAsync("/plans", PlanBody("Growth"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var created = await response.Content.ReadFromJsonAsync<ResponseDataDto<PlanResponseDto>>();
        created!.Data.Name.Should().Be("Growth");
        created.Data.CurrentVersionNumber.Should().Be(1);
        created.Data.CurrentVersion.Should().NotBeNull();
        created.Data.CurrentVersion!.CostPerRequest.Should().Be(0.25m);
    }

    [Fact]
    public async Task CreatePlan_GivenADuplicateName_ReturnsConflict()
    {
        // Arrange
        await _httpClient.PostAsJsonAsync("/plans", PlanBody("Growth"));

        // Act
        var response = await _httpClient.PostAsJsonAsync("/plans", PlanBody("Growth"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task CreatePlan_AsManager_ReturnsForbidden()
    {
        // Arrange
        await AuthenticationHelper.SetTokenAsync(_httpClient, ManagerEmail, Password);

        // Act
        var response = await _httpClient.PostAsJsonAsync("/plans", PlanBody("Growth"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task PublishPlanVersion_GivenAPlan_AddsTheNextVersionAndKeepsTheOld()
    {
        // Arrange
        var planId = await CreatePlanAsync("Growth");

        // Act
        var response = await _httpClient.PostAsJsonAsync(
            $"/plans/{planId}/versions", new PublishPlanVersionRequestBodyDto { CostPerRequest = 0.40m, Currency = Currency.Gbp });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var published = await response.Content.ReadFromJsonAsync<ResponseDataDto<PlanVersionResponseDto>>();
        published!.Data.VersionNumber.Should().Be(2);
        published.Data.CostPerRequest.Should().Be(0.40m);

        var versionsResponse = await _httpClient.GetAsync($"/plans/{planId}/versions");
        var versions = await versionsResponse.Content.ReadFromJsonAsync<PagedResponseDto<PlanVersionResponseDto>>();

        versions!.Data.Should().HaveCount(2);
        versions.Data.Should().Contain(x => x.VersionNumber == 1 && x.CostPerRequest == 0.25m);
    }

    [Fact]
    public async Task CreateSubscription_GivenAPlanVersion_OpensAnEndlessStretch()
    {
        // Arrange
        var planVersionId = await CurrentPlanVersionIdAsync(await CreatePlanAsync("Growth"));

        // Act
        var response = await _httpClient.PostAsJsonAsync(
            $"/organizations/{OrganizationId}/subscriptions",
            new CreateSubscriptionRequestBodyDto { PlanVersionId = planVersionId, EffectiveFrom = null });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var created = await response.Content.ReadFromJsonAsync<ResponseDataDto<SubscriptionResponseDto>>();
        created!.Data.PlanVersionId.Should().Be(planVersionId);
        created.Data.EffectiveTo.Should().BeNull();
        created.Data.PlanName.Should().Be("Growth");
    }

    [Fact]
    public async Task CreateSubscription_GivenAFutureDate_ClosesTheOpenStretchAtThatInstant()
    {
        // Arrange
        var planId = await CreatePlanAsync("Growth");
        var firstVersionId = await CurrentPlanVersionIdAsync(planId);

        await SubscribeAsync(firstVersionId, null);

        await _httpClient.PostAsJsonAsync($"/plans/{planId}/versions", new PublishPlanVersionRequestBodyDto { CostPerRequest = 0.40m, Currency = Currency.Gbp });
        var secondVersionId = await CurrentPlanVersionIdAsync(planId);

        var renewal = DateTimeOffset.UtcNow.AddMonths(1);

        // Act
        var response = await _httpClient.PostAsJsonAsync(
            $"/organizations/{OrganizationId}/subscriptions",
            new CreateSubscriptionRequestBodyDto { PlanVersionId = secondVersionId, EffectiveFrom = renewal });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var listResponse = await _httpClient.GetAsync($"/organizations/{OrganizationId}/subscriptions?IncludeSuperseded=true");
        var subscriptions = await listResponse.Content.ReadFromJsonAsync<PagedResponseDto<SubscriptionResponseDto>>();

        var closed = subscriptions!.Data.Single(x => x.PlanVersionId == firstVersionId);
        closed.EffectiveTo.Should().BeCloseTo(renewal, TimeSpan.FromSeconds(1));

        var scheduled = subscriptions.Data.Single(x => x.PlanVersionId == secondVersionId);
        scheduled.EffectiveFrom.Should().BeCloseTo(renewal, TimeSpan.FromSeconds(1));
        scheduled.EffectiveTo.Should().BeNull();
    }

    [Fact]
    public async Task DeleteSubscription_GivenAScheduledChange_ReopensThePredecessor()
    {
        // Arrange
        var planId = await CreatePlanAsync("Growth");
        var firstVersionId = await CurrentPlanVersionIdAsync(planId);

        await SubscribeAsync(firstVersionId, null);

        await _httpClient.PostAsJsonAsync($"/plans/{planId}/versions", new PublishPlanVersionRequestBodyDto { CostPerRequest = 0.40m, Currency = Currency.Gbp });
        var secondVersionId = await CurrentPlanVersionIdAsync(planId);

        var scheduledId = await SubscribeAsync(secondVersionId, DateTimeOffset.UtcNow.AddMonths(1));

        // Act
        var response = await _httpClient.DeleteAsync($"/organizations/{OrganizationId}/subscriptions/{scheduledId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var listResponse = await _httpClient.GetAsync($"/organizations/{OrganizationId}/subscriptions");
        var subscriptions = await listResponse.Content.ReadFromJsonAsync<PagedResponseDto<SubscriptionResponseDto>>();

        subscriptions!.Data.Should().ContainSingle();
        subscriptions.Data.Single().PlanVersionId.Should().Be(firstVersionId);
        subscriptions.Data.Single().EffectiveTo.Should().BeNull();
    }

    [Fact]
    public async Task DeleteSubscription_GivenAStretchAlreadyInForce_ReturnsConflict()
    {
        // Arrange
        var planVersionId = await CurrentPlanVersionIdAsync(await CreatePlanAsync("Growth"));
        var subscriptionId = await SubscribeAsync(planVersionId, null);

        // Act
        var response = await _httpClient.DeleteAsync($"/organizations/{OrganizationId}/subscriptions/{subscriptionId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task GenerateInvoice_GivenASubscription_SnapshotsThePlanVersion()
    {
        // Arrange
        var billedMonth = new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero);

        var planVersionId = await CurrentPlanVersionIdAsync(await CreatePlanAsync("Growth"));
        await SubscribeAsync(planVersionId, null);

        await BackdateSubscriptionsAsync(billedMonth.AddYears(-1));

        await DatabaseHelper.CreateUsages(testFixture.Services, Enumerable.Range(0, 8).Select(
            _ => FakeUsageEntity.CreateValid(_fixture) with
            {
                OrganizationId = OrganizationId,
                Organization = null!,
                Count = 1,
                CreatedAt = billedMonth
            }));

        // Act
        var response = await _httpClient.PostAsJsonAsync(
            $"/organizations/{OrganizationId}/invoices",
            new GenerateOrganizationInvoiceRequestBodyDto { Month = 1, Year = 2020 });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var invoice = await response.Content.ReadDataFromJsonAsync<InvoiceResourceDto>();
        invoice!.Invoice.PlanVersionId.Should().Be(planVersionId);
        invoice.Invoice.PlanVersionNumber.Should().Be(1);
        invoice.Invoice.CostPerRequest.Should().Be(0.25m);
        invoice.Invoice.Amount.Should().Be(8 * 0.25m);
    }

    [Fact]
    public async Task GenerateInvoice_GivenNoSubscription_ReturnsConflict()
    {
        // Act
        var response = await _httpClient.PostAsJsonAsync(
            $"/organizations/{OrganizationId}/invoices",
            new GenerateOrganizationInvoiceRequestBodyDto { Month = 1, Year = 2020 });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task DeletePlan_GivenAPlanSomebodyIsOn_ReturnsConflict()
    {
        // Arrange
        var planId = await CreatePlanAsync("Growth");
        await SubscribeAsync(await CurrentPlanVersionIdAsync(planId), null);

        // Act
        var response = await _httpClient.DeleteAsync($"/plans/{planId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var getResponse = await _httpClient.GetAsync($"/plans/{planId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task DeletePlan_GivenAPlanNobodyIsOn_ReturnsNoContent()
    {
        // Arrange
        var planId = await CreatePlanAsync("Growth");

        // Act
        var response = await _httpClient.DeleteAsync($"/plans/{planId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getResponse = await _httpClient.GetAsync($"/plans/{planId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdatePlan_DisablingIt_WithdrawsItWithoutDisturbingItsSubscribers()
    {
        // Arrange
        var planId = await CreatePlanAsync("Growth");
        var planVersionId = await CurrentPlanVersionIdAsync(planId);

        await SubscribeAsync(planVersionId, null);

        // Act
        var response = await _httpClient.PatchAsJsonAsync(
            $"/plans/{planId}", new UpdatePlanRequestBodyDto { Name = null, Description = null, IsEnabled = false });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var onOffer = await ListPlansAsync("?IncludeDisabled=false");
        onOffer.Should().NotContain(x => x.PlanId == planId);

        var everything = await ListPlansAsync("?IncludeDisabled=true");
        everything.Should().Contain(x => x.PlanId == planId && !x.IsEnabled);

        var subscriptionsResponse = await _httpClient.GetAsync($"/organizations/{OrganizationId}/subscriptions");
        var subscriptions = await subscriptionsResponse.Content.ReadFromJsonAsync<PagedResponseDto<SubscriptionResponseDto>>();

        subscriptions!.Data.Should().ContainSingle();
        subscriptions.Data.Single().PlanVersionId.Should().Be(planVersionId);
    }

    [Fact]
    public async Task UpdatePlan_RenamingIt_LeavesThePublishedPriceAlone()
    {
        // Arrange
        var planId = await CreatePlanAsync("Growth");
        var planVersionId = await CurrentPlanVersionIdAsync(planId);

        // Act
        var response = await _httpClient.PatchAsJsonAsync(
            $"/plans/{planId}", new UpdatePlanRequestBodyDto { Name = "Scale", Description = null, IsEnabled = null });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var getResponse = await _httpClient.GetAsync($"/plans/{planId}");
        var plan = await getResponse.Content.ReadFromJsonAsync<ResponseDataDto<PlanResponseDto>>();

        plan!.Data.Name.Should().Be("Scale");
        plan.Data.CurrentVersion!.PlanVersionId.Should().Be(planVersionId);
        plan.Data.CurrentVersion.VersionNumber.Should().Be(1);
        plan.Data.CurrentVersion.CostPerRequest.Should().Be(0.25m);
    }

    [Fact]
    public async Task ListPlans_GivenAnOwner_ShowsTheCatalogueAndTheirOwnPlanOnly()
    {
        // Arrange
        var cataloguePlanId = await CreatePlanAsync("Growth");
        var ownPlanId = await CreatePlanAsync("Negotiated", OrganizationId);
        var otherPlanId = await CreatePlanAsync("Someone Else", OrganizationConfigurations.DefaultOrganization.OrganizationId);

        // Act
        var plans = await ListPlansAsync($"?OwnerOrganizationId={OrganizationId}");

        // Assert
        plans.Should().Contain(x => x.PlanId == cataloguePlanId);
        plans.Should().Contain(x => x.PlanId == ownPlanId);
        plans.Should().NotContain(x => x.PlanId == otherPlanId);
    }

    [Fact]
    public async Task ListPlans_GivenNoOwner_ShowsEveryPlan()
    {
        // Arrange
        var cataloguePlanId = await CreatePlanAsync("Growth");
        var bespokePlanId = await CreatePlanAsync("Negotiated", OrganizationId);

        // Act
        var plans = await ListPlansAsync(string.Empty);

        // Assert
        plans.Should().Contain(x => x.PlanId == cataloguePlanId);
        plans.Should().Contain(x => x.PlanId == bespokePlanId);
    }

    [Fact]
    public async Task GetSubscription_GivenAnotherOrganizationOnTheRoute_ReturnsNotFound()
    {
        // Arrange
        var planVersionId = await CurrentPlanVersionIdAsync(await CreatePlanAsync("Growth"));
        var subscriptionId = await SubscribeAsync(planVersionId, null);

        var otherOrganizationId = OrganizationConfigurations.DefaultOrganization.OrganizationId;

        // Act
        var response = await _httpClient.GetAsync($"/organizations/{otherOrganizationId}/subscriptions/{subscriptionId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteSubscription_GivenAnotherOrganizationOnTheRoute_ReturnsNotFound()
    {
        // Arrange
        var planId = await CreatePlanAsync("Growth");
        await SubscribeAsync(await CurrentPlanVersionIdAsync(planId), null);

        await _httpClient.PostAsJsonAsync($"/plans/{planId}/versions", new PublishPlanVersionRequestBodyDto { CostPerRequest = 0.40m, Currency = Currency.Gbp });
        var scheduledId = await SubscribeAsync(await CurrentPlanVersionIdAsync(planId), DateTimeOffset.UtcNow.AddMonths(1));

        var otherOrganizationId = OrganizationConfigurations.DefaultOrganization.OrganizationId;

        // Act
        var response = await _httpClient.DeleteAsync($"/organizations/{otherOrganizationId}/subscriptions/{scheduledId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var stillThere = await _httpClient.GetAsync($"/organizations/{OrganizationId}/subscriptions/{scheduledId}");
        stillThere.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ListSubscriptions_ByDefault_ExcludesStretchesThatHaveClosed()
    {
        // Arrange
        var planId = await CreatePlanAsync("Growth");
        var firstVersionId = await CurrentPlanVersionIdAsync(planId);

        await SubscribeAsync(firstVersionId, null);

        await _httpClient.PostAsJsonAsync($"/plans/{planId}/versions", new PublishPlanVersionRequestBodyDto { CostPerRequest = 0.40m, Currency = Currency.Gbp });
        var secondVersionId = await CurrentPlanVersionIdAsync(planId);

        await SubscribeAsync(secondVersionId, DateTimeOffset.UtcNow.AddMonths(1));

        await DatabaseHelper.ShiftSubscriptions(testFixture.Services, OrganizationId, TimeSpan.FromDays(-365));

        // Act
        var openOnly = await ListSubscriptionsAsync("?IncludeSuperseded=false");
        var everything = await ListSubscriptionsAsync("?IncludeSuperseded=true");

        // Assert
        openOnly.Should().NotContain(x => x.PlanVersionId == firstVersionId);
        everything.Should().Contain(x => x.PlanVersionId == firstVersionId);
        everything.Should().Contain(x => x.PlanVersionId == secondVersionId);
    }

    [Theory]
    [InlineData("/plans?PageNumber=0")]
    [InlineData("/plans?PageSize=-5")]
    [InlineData("/plans?PageSize=100000")]
    public async Task ListPlans_GivenPagingOutOfBounds_ReturnsBadRequest(string route)
    {
        // Act
        var response = await _httpClient.GetAsync(route);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData("?PageNumber=0")]
    [InlineData("?PageSize=100000")]
    public async Task ListSubscriptions_GivenPagingOutOfBounds_ReturnsBadRequest(string query)
    {
        // Act
        var response = await _httpClient.GetAsync($"/organizations/{OrganizationId}/subscriptions{query}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData("?PageNumber=0")]
    [InlineData("?PageSize=100000")]
    public async Task ListPlanVersions_GivenPagingOutOfBounds_ReturnsBadRequest(string query)
    {
        // Arrange
        var planId = await CreatePlanAsync("Growth");

        // Act
        var response = await _httpClient.GetAsync($"/plans/{planId}/versions{query}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private static CreatePlanRequestBodyDto PlanBody(string name, Guid? organizationId = null)
    {
        return new CreatePlanRequestBodyDto
        {
            Name = name,
            Currency = Currency.Gbp,
            Description = "Migrated from the per-organization rate.",
            OrganizationId = organizationId,
            CostPerRequest = 0.25m
        };
    }

    private async Task<Guid> CreatePlanAsync(string name, Guid? organizationId = null)
    {
        var response = await _httpClient.PostAsJsonAsync("/plans", PlanBody(name, organizationId));
        var created = await response.Content.ReadFromJsonAsync<ResponseDataDto<PlanResponseDto>>();

        return created!.Data.PlanId;
    }

    private async Task<List<PlanResponseDto>> ListPlansAsync(string query)
    {
        var response = await _httpClient.GetAsync($"/plans{query}");
        var plans = await response.Content.ReadFromJsonAsync<PagedResponseDto<PlanResponseDto>>();

        return plans!.Data.ToList();
    }

    private async Task<List<SubscriptionResponseDto>> ListSubscriptionsAsync(string query)
    {
        var response = await _httpClient.GetAsync($"/organizations/{OrganizationId}/subscriptions{query}");
        var subscriptions = await response.Content.ReadFromJsonAsync<PagedResponseDto<SubscriptionResponseDto>>();

        return subscriptions!.Data.ToList();
    }

    private async Task<Guid> CurrentPlanVersionIdAsync(Guid planId)
    {
        var response = await _httpClient.GetAsync($"/plans/{planId}");
        var plan = await response.Content.ReadFromJsonAsync<ResponseDataDto<PlanResponseDto>>();

        return plan!.Data.CurrentVersion!.PlanVersionId;
    }

    private async Task<Guid> SubscribeAsync(Guid planVersionId, DateTimeOffset? effectiveFrom)
    {
        var response = await _httpClient.PostAsJsonAsync(
            $"/organizations/{OrganizationId}/subscriptions",
            new CreateSubscriptionRequestBodyDto { PlanVersionId = planVersionId, EffectiveFrom = effectiveFrom });

        var created = await response.Content.ReadFromJsonAsync<ResponseDataDto<SubscriptionResponseDto>>();

        return created!.Data.SubscriptionId;
    }

    private async Task BackdateSubscriptionsAsync(DateTimeOffset effectiveFrom)
    {
        await DatabaseHelper.BackdateSubscriptions(testFixture.Services, OrganizationId, effectiveFrom);
    }
}
