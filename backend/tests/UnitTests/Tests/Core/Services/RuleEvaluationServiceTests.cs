using Core.Interfaces;
using Core.Interfaces.Repositories;
using Core.Models.RuleEvaluations;
using Core.Services;
using Core.ValueObjects;
using FluentAssertions;
using Moq;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Core.Models.RuleEvaluations;

namespace UnitTests.Tests.Core.Services;

public class RuleEvaluationServiceTests : BaseTestClass
{
    private readonly Mock<IRuleEvaluationRepository> _mockRuleEvaluationRepository = new();
    private readonly Mock<IUnitOfWork> _mockUnitOfWork = new();
    private readonly RuleEvaluationService _sut;

    public RuleEvaluationServiceTests()
    {
        _sut = new RuleEvaluationService(
            _mockRuleEvaluationRepository.Object,
            _mockUnitOfWork.Object,
            MockTimeProvider,
            TestModeContext);
    }

    [Fact]
    public async Task CreateAsync_GivenCreateRuleEvaluationRequest_CreatesRuleEvaluation()
    {
        // Arrange
        var createRuleEvaluationRequest = FakeCreateRuleEvaluationRequest.CreateValid(Fixture);
        var ruleEvaluation = FakeRuleEvaluation.CreateValid(Fixture);

        _mockRuleEvaluationRepository
            .Setup(x => x.GetAsync(It.IsAny<RuleEvaluationId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ruleEvaluation);

        // Act
        var result = await _sut.CreateAsync(createRuleEvaluationRequest, CancellationToken.None);

        // Assert
        result.Should().BeEquivalentTo(ruleEvaluation);
        _mockRuleEvaluationRepository.Verify(x => x.AddAsync(It.IsAny<RuleEvaluation>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockRuleEvaluationRepository.Verify(x => x.GetAsync(It.IsAny<RuleEvaluationId>(), It.IsAny<CancellationToken>()), Times.Once);
    }

}
