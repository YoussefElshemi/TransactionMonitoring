using Core.Interfaces.Services;
using Core.Jobs;
using Core.Models.Invoices;
using Core.Models.Organizations;
using Core.Models.Subscriptions;
using Core.ValueObjects;
using Moq;
using Quartz;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Invoices;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Organizations;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Subscriptions;

namespace UnitTests.Tests.Core.Jobs;

public class GenerateInvoiceJobTests : BaseTestClass
{
    private readonly Mock<IOrganizationService> _mockOrganizationService = new();
    private readonly Mock<IInvoiceService> _mockInvoiceService = new();
    private readonly Mock<ISubscriptionService> _mockSubscriptionService = new();
    private readonly Mock<IBillingProfileService> _mockBillingProfileService = new();

    private GenerateInvoiceJob CreateSut()
    {
        return new GenerateInvoiceJob(
            _mockOrganizationService.Object,
            _mockInvoiceService.Object,
            _mockSubscriptionService.Object,
            _mockBillingProfileService.Object,
            MockTimeProvider);
    }

    private List<Organization> GivenOrganizations()
    {
        var organizations = new List<Organization>
        {
            FakeOrganization.CreateValid(Fixture),
            FakeOrganization.CreateValid(Fixture)
        };

        _mockOrganizationService
            .Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(organizations);

        return organizations;
    }

    private void GivenInvoiceExists(bool exists)
    {
        _mockInvoiceService
            .Setup(x => x.ExistsByDateAsync(
                It.IsAny<OrganizationId>(),
                It.IsAny<Month>(),
                It.IsAny<Year>(),
                It.IsAny<IncludeDeleted>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(exists));
    }

    private void GivenSubscription(Subscription? subscription)
    {
        _mockSubscriptionService
            .Setup(x => x.GetInEffectAsync(It.IsAny<OrganizationId>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);
    }

    private void GivenBillingProfile(bool exists)
    {
        _mockBillingProfileService
            .Setup(x => x.ExistsAsync(It.IsAny<OrganizationId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(exists));
    }

    private void GivenGeneratedInvoice()
    {
        _mockInvoiceService
            .Setup(x => x.GenerateAsync(It.IsAny<GenerateInvoiceRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => FakeInvoice.CreateValid(Fixture));
    }

    [Fact]
    public async Task Execute_InvoiceDoesNotExist_GeneratesInvoices()
    {
        // Arrange
        var organizations = GivenOrganizations();
        GivenInvoiceExists(false);
        GivenSubscription(FakeSubscription.CreateValid(Fixture));
        GivenBillingProfile(true);
        GivenGeneratedInvoice();

        var job = CreateSut();
        var mockContext = new Mock<IJobExecutionContext>();

        // Act
        await job.Execute(mockContext.Object);

        // Assert
        _mockOrganizationService.Verify(x => x.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockInvoiceService.Verify(x => x.GenerateAsync(It.IsAny<GenerateInvoiceRequest>(), It.IsAny<CancellationToken>()), Times.Exactly(organizations.Count));

        _mockInvoiceService.Verify(x => x.IssueAsync(It.IsAny<InvoiceId>(), It.IsAny<CancellationToken>()), Times.Exactly(organizations.Count));
    }

    [Fact]
    public async Task Execute_InvoiceExists_DoesNotGenerateInvoice()
    {
        // Arrange
        GivenOrganizations();
        GivenInvoiceExists(true);
        GivenSubscription(FakeSubscription.CreateValid(Fixture));
        GivenBillingProfile(true);

        var job = CreateSut();
        var mockContext = new Mock<IJobExecutionContext>();

        // Act
        await job.Execute(mockContext.Object);

        // Assert
        _mockOrganizationService.Verify(x => x.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockInvoiceService.Verify(x => x.GenerateAsync(It.IsAny<GenerateInvoiceRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Execute_OrganizationOnNoPlan_SkipsIt()
    {
        // Arrange
        GivenOrganizations();
        GivenInvoiceExists(false);
        GivenSubscription(null);
        GivenBillingProfile(true);

        var job = CreateSut();
        var mockContext = new Mock<IJobExecutionContext>();

        // Act
        await job.Execute(mockContext.Object);

        // Assert
        _mockInvoiceService.Verify(x => x.GenerateAsync(It.IsAny<GenerateInvoiceRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Execute_OrganizationWithNoBillingProfile_SkipsIt()
    {
        // Arrange
        GivenOrganizations();
        GivenInvoiceExists(false);
        GivenSubscription(FakeSubscription.CreateValid(Fixture));
        GivenBillingProfile(false);

        var job = CreateSut();
        var mockContext = new Mock<IJobExecutionContext>();

        // Act
        await job.Execute(mockContext.Object);

        // Assert
        _mockInvoiceService.Verify(x => x.GenerateAsync(It.IsAny<GenerateInvoiceRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
