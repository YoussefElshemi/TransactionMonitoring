using Core.Interfaces;
using Core.Interfaces.Repositories;
using Core.Models.CaseLinks;
using Core.Services;
using Core.ValueObjects;
using FluentAssertions;
using FluentValidation;
using Moq;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

namespace UnitTests.Tests.Core.Services;

public class CaseLinkServiceTests : BaseTestClass
{
    private readonly Mock<ICaseLinkRepository> _mockCaseLinkRepository = new();
    private readonly Mock<IUnitOfWork> _mockUnitOfWork = new();
    private readonly CaseLinkService _sut;

    public CaseLinkServiceTests()
    {
        _sut = new CaseLinkService(
            _mockCaseLinkRepository.Object,
            _mockUnitOfWork.Object,
            MockTimeProvider,
            TestModeContext);
    }

    [Fact]
    public async Task CreateAsync_LinkingACaseToItself_IsRefused()
    {
        // Arrange
        var caseId = FakeCaseId.CreateValid(Fixture);
        var request = Request(caseId, caseId);

        // Act
        var act = async () => await _sut.CreateAsync(request, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ValidationException>();
        _mockCaseLinkRepository.Verify(x => x.AddAsync(It.IsAny<CaseLink>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WhenTheCasesAreAlreadyLinked_IsRefused()
    {
        // Arrange
        var request = Request(FakeCaseId.CreateValid(Fixture), FakeCaseId.CreateValid(Fixture));
        _mockCaseLinkRepository
            .Setup(x => x.ExistsBetweenAsync(It.IsAny<OrganizationId>(), It.IsAny<CaseId>(), It.IsAny<CaseId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(true));

        // Act
        var act = async () => await _sut.CreateAsync(request, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ValidationException>();
        _mockCaseLinkRepository.Verify(x => x.AddAsync(It.IsAny<CaseLink>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_GivenTwoDistinctUnlinkedCases_WritesOneRow()
    {
        // Arrange
        var request = Request(FakeCaseId.CreateValid(Fixture), FakeCaseId.CreateValid(Fixture));
        _mockCaseLinkRepository
            .Setup(x => x.ExistsBetweenAsync(It.IsAny<OrganizationId>(), It.IsAny<CaseId>(), It.IsAny<CaseId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(false));

        // Act
        await _sut.CreateAsync(request, CancellationToken.None);

        // Assert
        _mockCaseLinkRepository.Verify(
            x => x.AddAsync(It.Is<CaseLink>(y => y.CaseId == request.CaseId && y.LinkedCaseId == request.LinkedCaseId), It.IsAny<CancellationToken>()),
            Times.Once);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    private CreateCaseLinkRequest Request(CaseId caseId, CaseId linkedCaseId)
    {
        return new CreateCaseLinkRequest
        {
            OrganizationId = FakeOrganizationId.CreateValid(Fixture),
            CaseId = caseId,
            LinkedCaseId = linkedCaseId,
            Reason = new LinkReason("same remitter"),
            CreatedByUserId = FakeApplicationUserId.CreateValid(Fixture)
        };
    }
}
