using Core.Enums;
using Core.Exceptions;
using Core.Interfaces;
using Core.Interfaces.Repositories;
using Core.Interfaces.Services;
using Core.Models.Billing;
using Core.Services;
using Core.ValueObjects;
using FluentAssertions;
using Moq;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Billing;
using UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

namespace UnitTests.Tests.Core.Services;

public class BillingProfileServiceTests : BaseTestClass
{
    private readonly Mock<IBillingProfileRepository> _mockBillingProfileRepository = new();
    private readonly Mock<IPaymentProvider> _mockPaymentProvider = new();
    private readonly Mock<IUnitOfWork> _mockUnitOfWork = new();
    private readonly BillingProfileService _sut;

    public BillingProfileServiceTests()
    {
        _sut = new BillingProfileService(
            _mockBillingProfileRepository.Object,
            _mockPaymentProvider.Object,
            _mockUnitOfWork.Object,
            MockTimeProvider);
    }

    private SaveBillingProfileRequest Request(string countryCode = "GB", string? taxIdentifier = null)
    {
        return new SaveBillingProfileRequest
        {
            OrganizationId = FakeOrganizationId.CreateValid(Fixture),
            BillingEntityId = FakeBillingEntityId.CreateValid(Fixture),
            Locale = null,
            LegalName = FakeLegalName.CreateValid(Fixture),
            ContactName = FakeContactName.CreateValid(Fixture),
            ContactEmail = FakeEmail.CreateValid(Fixture),
            Address = FakePostalAddress.CreateValid(Fixture),
            CountryCode = new CountryCode(countryCode),
            TaxIdentifier = taxIdentifier is null ? null : new TaxIdentifier(taxIdentifier),
            Currency = Currency.Gbp,
            PaymentTerms = new PaymentTerms(28),
            UpdatedByUserId = FakeApplicationUserId.CreateValid(fixture: Fixture)
        };
    }

    private BillingProfile GivenStoredProfile(BillingProfile? billingProfile)
    {
        var stored = billingProfile ?? FakeBillingProfile.CreateValid(Fixture);

        _mockBillingProfileRepository
            .Setup(x => x.GetAsync(It.IsAny<OrganizationId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(stored);

        return stored;
    }

    [Fact]
    public async Task SaveAsync_GivenNoExistingProfile_Adds()
    {
        // Arrange
        _mockBillingProfileRepository
            .SetupSequence(x => x.GetAsync(It.IsAny<OrganizationId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((BillingProfile?)null)
            .ReturnsAsync(FakeBillingProfile.CreateValid(Fixture));

        // Act
        await _sut.SaveAsync(Request(), CancellationToken.None);

        // Assert
        _mockBillingProfileRepository.Verify(x => x.AddAsync(It.IsAny<BillingProfile>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockBillingProfileRepository.Verify(x => x.UpdateAsync(It.IsAny<BillingProfile>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SaveAsync_GivenAnExistingProfile_Updates()
    {
        // Arrange
        GivenStoredProfile(null);

        // Act
        await _sut.SaveAsync(Request(), CancellationToken.None);

        // Assert
        _mockBillingProfileRepository.Verify(x => x.UpdateAsync(It.IsAny<BillingProfile>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockBillingProfileRepository.Verify(x => x.AddAsync(It.IsAny<BillingProfile>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SaveAsync_GivenARegistrationWrittenWithoutItsPrefix_StoresItNormalised()
    {
        // Arrange
        BillingProfile? written = null;

        _mockBillingProfileRepository
            .Setup(x => x.GetAsync(It.IsAny<OrganizationId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((BillingProfile?)null);

        _mockBillingProfileRepository
            .Setup(x => x.AddAsync(It.IsAny<BillingProfile>(), It.IsAny<CancellationToken>()))
            .Callback((BillingProfile profile, CancellationToken _) => written = profile);

        _mockBillingProfileRepository
            .SetupSequence(x => x.GetAsync(It.IsAny<OrganizationId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((BillingProfile?)null)
            .ReturnsAsync(FakeBillingProfile.CreateValid(Fixture));

        // Act
        await _sut.SaveAsync(Request(taxIdentifier: "123456789"), CancellationToken.None);

        // Assert
        ((string)written!.TaxIdentifier!.Value).Should().Be("GB123456789");
    }

    [Fact]
    public async Task SaveAsync_GivenAMalformedRegistration_Throws()
    {
        // Arrange
        // Act
        var act = async () => await _sut.SaveAsync(Request(taxIdentifier: "GB12"), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidTaxIdentifierException>();

        _mockBillingProfileRepository.Verify(x => x.AddAsync(It.IsAny<BillingProfile>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task VerifyTaxIdentifierAsync_GivenAWellFormedRegistration_StampsIt()
    {
        // Arrange
        GivenStoredProfile(FakeBillingProfile.CreateValid(Fixture) with
        {
            CountryCode = new CountryCode("GB"),
            TaxIdentifier = new TaxIdentifier("GB123456789"),
            TaxIdentifierVerifiedAt = null
        });

        // Act
        await _sut.VerifyTaxIdentifierAsync(FakeOrganizationId.CreateValid(Fixture), CancellationToken.None);

        // Assert
        _mockBillingProfileRepository.Verify(
            x => x.VerifyTaxIdentifierAsync(It.IsAny<OrganizationId>(), It.IsAny<VerifiedAt>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task VerifyTaxIdentifierAsync_GivenAMalformedRegistration_Throws()
    {
        // Arrange
        GivenStoredProfile(FakeBillingProfile.CreateValid(Fixture) with
        {
            CountryCode = new CountryCode("DE"),
            TaxIdentifier = new TaxIdentifier("GB123456789"),
            TaxIdentifierVerifiedAt = null
        });

        // Act
        var act = async () => await _sut.VerifyTaxIdentifierAsync(FakeOrganizationId.CreateValid(Fixture), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidTaxIdentifierException>();

        _mockBillingProfileRepository.Verify(
            x => x.VerifyTaxIdentifierAsync(It.IsAny<OrganizationId>(), It.IsAny<VerifiedAt>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetAsync_GivenNoProfile_Throws()
    {
        // Arrange
        _mockBillingProfileRepository
            .Setup(x => x.GetAsync(It.IsAny<OrganizationId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((BillingProfile?)null);

        // Act
        var act = async () => await _sut.GetAsync(FakeOrganizationId.CreateValid(Fixture), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NoBillingProfileException>();
    }
}
