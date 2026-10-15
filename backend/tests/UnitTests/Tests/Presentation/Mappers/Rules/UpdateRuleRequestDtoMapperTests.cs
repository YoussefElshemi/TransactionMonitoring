using Core.Enums;
using Core.Models.Common;
using Core.ValueObjects;
using FluentAssertions;
using Presentation.Mappers.Rules;
using Presentation.Models.Rules;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Presentation.Models.Rules;

namespace UnitTests.Tests.Presentation.Mappers.Rules;

public class UpdateRuleRequestDtoMapperTests : BaseTestClass
{
    [Fact]
    public void ToModel_UpdateRuleRequestDto()
    {
        // Arrange
        var updateRuleRequestDto = FakeUpdateRuleRequestDto.CreateValid(Fixture);

        // Act
        var updateRuleRequest = updateRuleRequestDto.ToModel();

        // Assert
        updateRuleRequest.RuleId.Should().Be(new RuleId(updateRuleRequestDto.RuleId));
        updateRuleRequest.FriendlyName.Should().Be(new FriendlyName(updateRuleRequestDto.Body.FriendlyName!));
        updateRuleRequest.IsEnabled.Should().Be(new IsEnabled(updateRuleRequestDto.Body.IsEnabled!.Value));
    }

    [Fact]
    public void ToModel_UpdateRuleRequestDto_NullFieldsMapToNull()
    {
        // Arrange
        var updateRuleRequestDto = FakeUpdateRuleRequestDto.CreateValid(Fixture) with
        {
            Body = new UpdateRuleRequestBodyDto
            {
                FriendlyName = null,
                IsEnabled = null
            }
        };

        // Act
        var updateRuleRequest = updateRuleRequestDto.ToModel();

        // Assert
        updateRuleRequest.FriendlyName.Should().BeNull();
        updateRuleRequest.IsEnabled.Should().BeNull();
    }

    [Fact]
    public void ToModel_AlertPolicySet_CarriesTheValue()
    {
        // Arrange
        var updateRuleRequestDto = FakeUpdateRuleRequestDto.CreateValid(Fixture) with
        {
            Body = new UpdateRuleRequestBodyDto
            {
                AlertSubjectRole = Patch<string?>.Set(nameof(PartyRole.Beneficiary)),
                DeduplicationWindow = Patch<int?>.Set(30)
            }
        };

        // Act
        var updateRuleRequest = updateRuleRequestDto.ToModel();

        // Assert
        updateRuleRequest.AlertSubjectRole.IsSet.Should().BeTrue();
        updateRuleRequest.AlertSubjectRole.Value.Should().Be(PartyRole.Beneficiary);
        updateRuleRequest.DeduplicationWindow.IsSet.Should().BeTrue();
        ((int)updateRuleRequest.DeduplicationWindow.Value!.Value).Should().Be(30);
    }

    [Fact]
    public void ToModel_AlertPolicySetToNull_IsSetAndCarriesNull()
    {
        // Arrange
        var updateRuleRequestDto = FakeUpdateRuleRequestDto.CreateValid(Fixture) with
        {
            Body = new UpdateRuleRequestBodyDto
            {
                AlertSubjectRole = Patch<string?>.Set(null),
                DeduplicationWindow = Patch<int?>.Set(null)
            }
        };

        // Act
        var updateRuleRequest = updateRuleRequestDto.ToModel();

        // Assert
        updateRuleRequest.AlertSubjectRole.IsSet.Should().BeTrue();
        updateRuleRequest.AlertSubjectRole.Value.Should().BeNull();
        updateRuleRequest.DeduplicationWindow.IsSet.Should().BeTrue();
        updateRuleRequest.DeduplicationWindow.Value.Should().BeNull();
    }

    [Fact]
    public void ToModel_AlertPolicyOmitted_IsNotSet()
    {
        // Arrange
        var updateRuleRequestDto = FakeUpdateRuleRequestDto.CreateValid(Fixture) with
        {
            Body = new UpdateRuleRequestBodyDto()
        };

        // Act
        var updateRuleRequest = updateRuleRequestDto.ToModel();

        // Assert
        updateRuleRequest.AlertSubjectRole.IsSet.Should().BeFalse();
        updateRuleRequest.DeduplicationWindow.IsSet.Should().BeFalse();
    }
}
