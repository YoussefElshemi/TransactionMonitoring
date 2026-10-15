using Core.Interfaces.Services;
using Core.Models.Rules;
using Core.ValueObjects;
using FluentValidation.TestHelper;
using Moq;
using Presentation.Validators.Rules;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Presentation.Models.Rules;

namespace UnitTests.Tests.Presentation.Validators.Rules;

public class TransferRuleRequestValidatorTests : BaseTestClass
{
    private readonly Mock<IRuleService> _ruleServiceMock;
    private readonly Mock<IRuleSetService> _ruleSetServiceMock;
    private readonly TransferRuleRequestValidator _sut;

    public TransferRuleRequestValidatorTests()
    {
        _ruleSetServiceMock = new Mock<IRuleSetService>();
        _ruleServiceMock = new Mock<IRuleService>();

        _sut = new TransferRuleRequestValidator(_ruleServiceMock.Object, _ruleSetServiceMock.Object);
    }

    [Fact]
    public async Task Validate_ValidRequest_SucceedsValidation()
    {
        // Arrange
        var request = FakeTransferRuleRequestDto.CreateValid(Fixture);

        _ruleServiceMock
            .Setup(x => x.ExistsAsync(It.IsAny<OrganizationId>(), It.IsAny<RuleId>(), It.IsAny<IncludeDeleted>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(true));

        _ruleSetServiceMock
            .Setup(x => x.ExistsAsync(It.IsAny<OrganizationId>(), It.IsAny<RuleSetId>(), It.IsAny<IncludeDeleted>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(true));

        _ruleServiceMock
            .Setup(x => x.GetCountAsync(It.IsAny<ListRuleRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TotalCount(5));

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Validate_RuleIdEmpty_FailsValidation()
    {
        // Arrange
        var request = FakeTransferRuleRequestDto.CreateValid(Fixture) with
        {
            RuleId = Guid.Empty
        };

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.RuleId)
            .WithErrorCode("NotEmptyValidator");
    }

    [Fact]
    public async Task Validate_TargetRuleSetIdEmpty_FailsValidation()
    {
        // Arrange
        var request = FakeTransferRuleRequestDto.CreateValid(Fixture);
        request = request with
        {
            Body = request.Body with
            {
                TargetRuleSetId = Guid.Empty
            }
        };

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Body.TargetRuleSetId)
            .WithErrorCode("NotEmptyValidator");
    }

    [Fact]
    public async Task Validate_TargetRuleSetMaxRulesExceeded_FailsValidation()
    {
        // Arrange
        var request = FakeTransferRuleRequestDto.CreateValid(Fixture);

        _ruleServiceMock
            .Setup(x => x.ExistsAsync(It.IsAny<OrganizationId>(), It.IsAny<RuleId>(), It.IsAny<IncludeDeleted>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(true));

        _ruleSetServiceMock
            .Setup(x => x.ExistsAsync(It.IsAny<OrganizationId>(), It.IsAny<RuleSetId>(), It.IsAny<IncludeDeleted>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(true));

        _ruleServiceMock
            .Setup(x => x.GetCountAsync(It.IsAny<ListRuleRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TotalCount(CreateRuleRequestValidator.MaxRulesInRuleSet + 1));

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Body.TargetRuleSetId)
            .WithErrorMessage($"RuleSet count cannot be greater than {CreateRuleRequestValidator.MaxRulesInRuleSet}");
    }
}
