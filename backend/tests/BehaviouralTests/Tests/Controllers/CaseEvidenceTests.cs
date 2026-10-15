using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using AutoFixture;
using BehaviouralTests.TestHelpers;
using Core.Enums;
using Core.Interfaces.Services;
using FluentAssertions;
using Infrastructure;
using Infrastructure.Configurations;
using Infrastructure.Entities;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Presentation.Models.Attachments;
using Presentation.Models.CaseLinks;
using Presentation.Models.Cases;
using Presentation.Models.Common;
using Presentation.Models.NotificationSubscriptions;
using UnitTests.TestHelpers.FakeObjects.Infrastructure.Entities;

namespace BehaviouralTests.Tests.Controllers;

public class CaseEvidenceTests(TestFixture testFixture) : IClassFixture<TestFixture>, IAsyncLifetime
{
    private readonly Fixture _fixture = AutoFixtureHelper.Create();

    // Stubbed: the host's antivirus deletes a real EICAR file before it can be scanned.
    private readonly HttpClient _httpClient = testFixture
        .WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
            services.AddScoped<IVirusScanner, StubVirusScanner>()))
        .CreateClient();

    private const string Email = "analyst@mail.com";
    private const string Password = "P@$$w0rd";
    private static readonly Guid OrganizationId = new("8c1f5a72-3e94-4d16-b0a7-9f2d6c48e513");

    [Fact]
    public async Task UploadAttachment_WithCleanFile_StoresItAndClearsItForDownload()
    {
        // Arrange
        await ArrangeAsync();
        var caseId = await SeedCaseAsync();

        // Act
        var response = await UploadAsync(caseId, "statement.txt", "text/plain", "nothing untoward in here");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var resource = await response.Content.ReadDataFromJsonAsync<AttachmentResourceDto>();
        resource!.Attachment.ScanStatus.Should().Be(nameof(AttachmentScanStatus.Clean));
        resource.Attachment.ThreatName.Should().BeNull();
        resource.Attachment.ScannedAt.Should().NotBeNull();
        resource.Attachment.ByteCount.Should().Be(24);
        resource.Attachment.FileName.Should().Be("statement.txt");

        var download = await _httpClient.GetAsync($"/cases/{caseId}/attachments/{resource.Attachment.AttachmentId}/content");

        download.StatusCode.Should().Be(HttpStatusCode.OK);
        (await download.Content.ReadAsStringAsync()).Should().Be("nothing untoward in here");
    }

    [Fact]
    public async Task UploadAttachment_WithInfectedFile_QuarantinesItAndRefusesTheDownload()
    {
        // Arrange
        await ArrangeAsync();
        var caseId = await SeedCaseAsync();

        // Act
        var response = await UploadAsync(caseId, "invoice.txt", "text/plain", StubVirusScanner.InfectedMarker);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var resource = await response.Content.ReadDataFromJsonAsync<AttachmentResourceDto>();
        resource!.Attachment.ScanStatus.Should().Be(nameof(AttachmentScanStatus.Infected));
        resource.Attachment.ThreatName.Should().Be("Stub-Test-Signature");

        var download = await _httpClient.GetAsync($"/cases/{caseId}/attachments/{resource.Attachment.AttachmentId}/content");

        download.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var listed = await ListAttachmentsAsync(caseId);
        listed.Should().ContainSingle();
        listed[0].Attachment.ScanStatus.Should().Be(nameof(AttachmentScanStatus.Infected));
    }

    [Fact]
    public async Task UploadAttachment_WithDisallowedContentType_IsRefused()
    {
        // Arrange
        await ArrangeAsync();
        var caseId = await SeedCaseAsync();

        // Act
        var response = await UploadAsync(caseId, "payload.exe", "application/x-msdownload", "MZ");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await ListAttachmentsAsync(caseId)).Should().BeEmpty();
    }

    [Fact]
    public async Task ListAttachments_FilteredByScanStatus_ReturnsOnlyThatStatus()
    {
        // Arrange
        await ArrangeAsync();
        var caseId = await SeedCaseAsync();
        await UploadAsync(caseId, "clean.txt", "text/plain", "fine");
        await UploadAsync(caseId, "bad.txt", "text/plain", StubVirusScanner.InfectedMarker);

        // Act
        var response = await _httpClient.GetAsync(
            $"/cases/{caseId}/attachments?ScanStatus={nameof(AttachmentScanStatus.Infected)}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var page = await response.Content.ReadFromJsonAsync<PagedResponseDto<AttachmentResourceDto>>();
        page!.Data.Should().ContainSingle();
        page.Data.First().Attachment.FileName.Should().Be("bad.txt");
    }

    [Fact]
    public async Task RescanAttachment_AlreadyScanned_IsRefused()
    {
        // Arrange
        await ArrangeAsync();
        var caseId = await SeedCaseAsync();
        var upload = await UploadAsync(caseId, "clean.txt", "text/plain", "fine");
        var resource = await upload.Content.ReadDataFromJsonAsync<AttachmentResourceDto>();

        // Act
        var response = await _httpClient.PostAsync(
            $"/cases/{caseId}/attachments/{resource!.Attachment.AttachmentId}/rescan", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task LinkCases_ReadFromTheOtherEnd_ShowsTheLink()
    {
        // Arrange
        await ArrangeAsync();
        var caseId = await SeedCaseAsync();
        var otherCaseId = await SeedCaseAsync();

        // Act
        var response = await LinkAsync(caseId, otherCaseId, "same remitter, same week");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var fromOriginal = await ListLinksAsync(caseId);
        fromOriginal.Should().ContainSingle();
        fromOriginal[0].CaseLink.LinkedCaseId.Should().Be(otherCaseId);
        fromOriginal[0].CaseLink.Reason.Should().Be("same remitter, same week");
        fromOriginal[0].LinkedCase!.CaseId.Should().Be(otherCaseId);

        var fromOther = await ListLinksAsync(otherCaseId);
        fromOther.Should().ContainSingle();
        fromOther[0].CaseLink.LinkedCaseId.Should().Be(caseId);
        fromOther[0].LinkedCase!.CaseId.Should().Be(caseId);
    }

    [Fact]
    public async Task LinkCases_AlreadyLinkedInReverse_IsRefused()
    {
        // Arrange
        await ArrangeAsync();
        var caseId = await SeedCaseAsync();
        var otherCaseId = await SeedCaseAsync();
        await LinkAsync(caseId, otherCaseId, null);

        // Act
        var response = await LinkAsync(otherCaseId, caseId, null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await ListLinksAsync(caseId)).Should().ContainSingle();
    }

    [Fact]
    public async Task LinkCases_ToItself_IsRefused()
    {
        // Arrange
        await ArrangeAsync();
        var caseId = await SeedCaseAsync();

        // Act
        var response = await LinkAsync(caseId, caseId, null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task UnlinkCases_ThenLinkAgain_IsAllowed()
    {
        // Arrange
        await ArrangeAsync();
        var caseId = await SeedCaseAsync();
        var otherCaseId = await SeedCaseAsync();
        var created = await LinkAsync(caseId, otherCaseId, null);
        var resource = await created.Content.ReadDataFromJsonAsync<CaseLinkResourceDto>();

        // Act
        var deleted = await _httpClient.DeleteAsync($"/cases/{caseId}/links/{resource!.CaseLink.CaseLinkId}");
        var relinked = await LinkAsync(caseId, otherCaseId, null);

        // Assert
        deleted.StatusCode.Should().Be(HttpStatusCode.NoContent);
        relinked.StatusCode.Should().Be(HttpStatusCode.Created);
        (await ListLinksAsync(caseId)).Should().ContainSingle();
    }

    [Fact]
    public async Task GetCaseLink_ReadFromTheOtherEnd_ShowsTheFarCase()
    {
        // Arrange
        await ArrangeAsync();
        var caseId = await SeedCaseAsync();
        var otherCaseId = await SeedCaseAsync();
        var created = await LinkAsync(caseId, otherCaseId, null);
        var caseLinkId = (await created.Content.ReadDataFromJsonAsync<CaseLinkResourceDto>())!.CaseLink.CaseLinkId;

        // Act
        var response = await _httpClient.GetAsync($"/cases/{otherCaseId}/links/{caseLinkId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var resource = await response.Content.ReadDataFromJsonAsync<CaseLinkResourceDto>();
        resource!.CaseLink.CaseId.Should().Be(otherCaseId);
        resource.CaseLink.LinkedCaseId.Should().Be(caseId);
        resource.LinkedCase!.CaseId.Should().Be(caseId);
    }

    [Fact]
    public async Task GetCaseLink_UnderACaseItDoesNotJoin_IsNotFound()
    {
        // Arrange
        await ArrangeAsync();
        var caseId = await SeedCaseAsync();
        var otherCaseId = await SeedCaseAsync();
        var unrelatedCaseId = await SeedCaseAsync();
        var created = await LinkAsync(caseId, otherCaseId, null);
        var caseLinkId = (await created.Content.ReadDataFromJsonAsync<CaseLinkResourceDto>())!.CaseLink.CaseLinkId;

        // Act
        var response = await _httpClient.GetAsync($"/cases/{unrelatedCaseId}/links/{caseLinkId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UnlinkCases_UnderACaseTheLinkDoesNotJoin_IsRefusedAndLeavesTheLink()
    {
        // Arrange
        await ArrangeAsync();
        var caseId = await SeedCaseAsync();
        var otherCaseId = await SeedCaseAsync();
        var unrelatedCaseId = await SeedCaseAsync();
        var created = await LinkAsync(caseId, otherCaseId, null);
        var caseLinkId = (await created.Content.ReadDataFromJsonAsync<CaseLinkResourceDto>())!.CaseLink.CaseLinkId;

        // Act
        var response = await _httpClient.DeleteAsync($"/cases/{unrelatedCaseId}/links/{caseLinkId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await ListLinksAsync(caseId)).Should().ContainSingle();
    }

    [Fact]
    public async Task ListCases_FilteredByParty_ReturnsThatPartysHistoryIncludingClosedCases()
    {
        // Arrange
        await ArrangeAsync();
        var partyId = await SeedPartyAsync();
        var otherPartyId = await SeedPartyAsync();

        var openCaseId = await SeedCaseAsync(partyId: partyId, status: CaseStatus.Pending);
        var closedCaseId = await SeedCaseAsync(partyId: partyId, status: CaseStatus.Approved);
        await SeedCaseAsync(partyId: otherPartyId, status: CaseStatus.Pending);

        // Act
        var response = await _httpClient.GetAsync($"/cases?PartyId={partyId}&PageSize=50");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var page = await response.Content.ReadFromJsonAsync<PagedResponseDto<CaseResourceDto>>();
        page!.Data.Select(x => x.Case.CaseId).Should().BeEquivalentTo([openCaseId, closedCaseId]);
    }

    [Fact]
    public async Task NotifyBreached_WithAnEmailSubscription_StagesOneNotificationAndStampsTheCase()
    {
        // Arrange
        await ArrangeAsync();
        await CreateSubscriptionAsync(NotificationChannel.Email, "ops@mail.com", notifyOnBreach: true);
        var breachedCaseId = await SeedCaseAsync(dueAt: DateTimeOffset.UtcNow.AddHours(-2));

        // Act
        var notified = await NotifyBreachedAsync();

        // Assert
        notified.Should().Be(1);

        var outboxMessages = await OutboxMessagesAsync(NotificationChannel.Email);
        outboxMessages.Should().ContainSingle();
        outboxMessages[0].Destination.Should().Be("ops@mail.com");

        (await BreachNotifiedAtAsync(breachedCaseId)).Should().NotBeNull();
    }

    [Fact]
    public async Task NotifyBreached_RunTwice_DoesNotNotifyTheSameCaseAgain()
    {
        // Arrange
        await ArrangeAsync();
        await CreateSubscriptionAsync(NotificationChannel.Email, "ops@mail.com", notifyOnBreach: true);
        await SeedCaseAsync(dueAt: DateTimeOffset.UtcNow.AddHours(-2));
        await NotifyBreachedAsync();

        // Act
        var notified = await NotifyBreachedAsync();

        // Assert
        notified.Should().Be(0);
        (await OutboxMessagesAsync(NotificationChannel.Email)).Should().ContainSingle();
    }

    [Fact]
    public async Task NotifyBreached_WithACaseInsideItsWindow_NotifiesNobody()
    {
        // Arrange
        await ArrangeAsync();
        await CreateSubscriptionAsync(NotificationChannel.Email, "ops@mail.com", notifyOnBreach: true);
        await SeedCaseAsync(dueAt: DateTimeOffset.UtcNow.AddHours(2));

        // Act
        var notified = await NotifyBreachedAsync();

        // Assert
        notified.Should().Be(0);
        (await OutboxMessagesAsync(NotificationChannel.Email)).Should().BeEmpty();
    }

    [Fact]
    public async Task NotifyBreached_WithABreachOnlySubscription_IgnoresTheSeverityFloor()
    {
        // Arrange
        await ArrangeAsync();
        await CreateSubscriptionAsync(NotificationChannel.Chat, "https://chat.example.com/hook", notifyOnBreach: true);
        await SeedCaseAsync(dueAt: DateTimeOffset.UtcNow.AddHours(-2), severity: RuleSeverity.Info);

        // Act
        var notified = await NotifyBreachedAsync();

        // Assert
        notified.Should().Be(1);
        (await OutboxMessagesAsync(NotificationChannel.Chat)).Should().ContainSingle();
    }

    [Fact]
    public async Task CreateSubscription_OnTheWebhookChannel_IsRefused()
    {
        // Arrange
        await ArrangeAsync();

        // Act
        var response = await CreateSubscriptionAsync(NotificationChannel.Webhook, "https://example.com/hook", notifyOnBreach: true);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateSubscription_AskingForNothing_IsRefused()
    {
        // Arrange
        await ArrangeAsync();

        // Act
        var response = await CreateSubscriptionAsync(NotificationChannel.Email, "ops@mail.com", notifyOnBreach: false);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateSubscription_ToADestinationTheChannelCannotUse_IsRefused()
    {
        // Arrange
        await ArrangeAsync();
        var created = await CreateSubscriptionAsync(NotificationChannel.Chat, "https://chat.example.com/hooks/abc", notifyOnBreach: true);
        var resource = await created.Content.ReadDataFromJsonAsync<NotificationSubscriptionResourceDto>();
        var notificationSubscriptionId = resource!.NotificationSubscription.NotificationSubscriptionId;

        // Act
        var response = await _httpClient.PatchAsJsonAsync(
            $"/notification-subscriptions/{notificationSubscriptionId}",
            new UpdateNotificationSubscriptionRequestBodyDto { Destination = "ops@mail.com" });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private async Task ArrangeAsync()
    {
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.Admin], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);
    }

    private async Task<HttpResponseMessage> UploadAsync(Guid caseId, string fileName, string contentType, string content)
    {
        using var form = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes(content));
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        form.Add(fileContent, "File", fileName);

        return await _httpClient.PostAsync($"/cases/{caseId}/attachments", form);
    }

    private async Task<List<AttachmentResourceDto>> ListAttachmentsAsync(Guid caseId)
    {
        var response = await _httpClient.GetAsync($"/cases/{caseId}/attachments?PageSize=50");
        var page = await response.Content.ReadFromJsonAsync<PagedResponseDto<AttachmentResourceDto>>();

        return page!.Data.ToList();
    }

    private async Task<HttpResponseMessage> LinkAsync(Guid caseId, Guid linkedCaseId, string? reason)
    {
        return await _httpClient.PostAsJsonAsync($"/cases/{caseId}/links", new CreateCaseLinkRequestBodyDto
        {
            LinkedCaseId = linkedCaseId,
            Reason = reason
        });
    }

    private async Task<List<CaseLinkResourceDto>> ListLinksAsync(Guid caseId)
    {
        var response = await _httpClient.GetAsync($"/cases/{caseId}/links?PageSize=50");
        var page = await response.Content.ReadFromJsonAsync<PagedResponseDto<CaseLinkResourceDto>>();

        return page!.Data.ToList();
    }

    private async Task<HttpResponseMessage> CreateSubscriptionAsync(
        NotificationChannel channel, string destination, bool notifyOnBreach, RuleSeverity? minimumSeverity = null)
    {
        return await _httpClient.PostAsJsonAsync("/notification-subscriptions", new CreateNotificationSubscriptionRequestBodyDto
        {
            Channel = channel,
            Destination = destination,
            MinimumSeverity = minimumSeverity,
            NotifyOnBreach = notifyOnBreach,
            IsEnabled = true
        });
    }

    private async Task<int> NotifyBreachedAsync()
    {
        using var scope = testFixture.Services.CreateScope();
        var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

        return await notificationService.NotifyBreachedAsync(CancellationToken.None);
    }

    private async Task<List<OutboxMessageEntity>> OutboxMessagesAsync(NotificationChannel channel)
    {
        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();

        return await context.OutboxMessages
            .Where(x => x.ChannelId == (int)channel)
            .ToListAsync();
    }

    private async Task<DateTimeOffset?> BreachNotifiedAtAsync(Guid caseId)
    {
        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();

        return await context.Cases
            .Where(x => x.CaseId == caseId)
            .Select(x => x.BreachNotifiedAt)
            .FirstAsync();
    }

    private async Task<Guid> SeedPartyAsync()
    {
        var partyEntity = FakePartyEntity.CreateValid(_fixture) with
        {
            OrganizationId = OrganizationId,
            Organization = null!
        };

        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();

        await context.Parties.AddAsync(partyEntity);
        await context.SaveChangesAsync();

        return partyEntity.PartyId;
    }

    private async Task<Guid> SeedCaseAsync(
        Guid? partyId = null,
        CaseStatus status = CaseStatus.Pending,
        RuleSeverity severity = RuleSeverity.High,
        DateTimeOffset? dueAt = null)
    {
        var now = DateTimeOffset.UtcNow;

        var caseEntity = FakeCaseEntity.CreateValid(_fixture) with
        {
            OrganizationId = OrganizationId,
            Organization = null!,
            TransactionId = null,
            PartyId = partyId,
            StatusId = (int)status,
            SeverityId = (int)severity,
            DispositionId = null,
            CaseReasonCodeId = null,
            QueueId = null,
            AssignedToUserId = null,
            AssignedToUser = null!,
            AssignedAt = null,
            CreatedAt = now,
            UpdatedAt = now,
            DueAt = dueAt ?? now.AddHours(24),
            EscalatedAt = null,
            EscalationRuleId = null,
            BreachNotifiedAt = null,
            ResolvedAt = status == CaseStatus.Approved ? now : null,
            ReopenedAt = null,
            IsDeleted = false
        };

        await DatabaseHelper.CreateCases(testFixture.Services, [caseEntity]);

        return caseEntity.CaseId;
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
