using Core.Enums;
using Core.Models.Common;
using Core.ValueObjects;
using FluentAssertions;
using Presentation.Mappers.Organizations;
using Presentation.Models.Organizations;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Presentation.Models.Organizations;

namespace UnitTests.Tests.Presentation.Mappers.Organizations;

public class UpdateOrganizationHoldPolicyRequestMapperTests : BaseTestClass
{
    [Fact]
    public void ToModel_HoldPolicy()
    {
        // Arrange
        var updateOrganizationHoldPolicyRequestDto = FakeUpdateOrganizationHoldPolicyRequestDto.CreateValid(Fixture);

        // Act
        var updateOrganizationRequest = updateOrganizationHoldPolicyRequestDto.ToModel();

        // Assert
        updateOrganizationRequest.OrganizationId.Should().Be(new OrganizationId(updateOrganizationHoldPolicyRequestDto.OrganizationId));
        updateOrganizationRequest.HoldWindow.IsSet.Should().BeTrue();
        updateOrganizationRequest.HoldWindow.Value.Should().Be(new HoldWindow(updateOrganizationHoldPolicyRequestDto.Body.HoldWindow.Value!.Value));
        updateOrganizationRequest.HoldExpiryVerdict.IsSet.Should().BeTrue();
        updateOrganizationRequest.HoldExpiryVerdict.Value.Should().Be(Enum.Parse<HoldExpiryVerdict>(updateOrganizationHoldPolicyRequestDto.Body.HoldExpiryVerdict.Value!, true));
    }

    [Fact]
    public void ToModel_LeavesTheFieldsOnlyAnAdminCanChangeUntouched()
    {
        // Arrange
        var updateOrganizationHoldPolicyRequestDto = FakeUpdateOrganizationHoldPolicyRequestDto.CreateValid(Fixture);

        // Act
        var updateOrganizationRequest = updateOrganizationHoldPolicyRequestDto.ToModel();

        // Assert
        updateOrganizationRequest.FriendlyName.Should().BeNull();
        updateOrganizationRequest.Status.Should().BeNull();
    }

    [Fact]
    public void ToModel_GivenAnOmittedHoldPolicy_LeavesBothPatchesUnset()
    {
        // Arrange
        var updateOrganizationHoldPolicyRequestDto = FakeUpdateOrganizationHoldPolicyRequestDto.CreateValid(Fixture);
        updateOrganizationHoldPolicyRequestDto = updateOrganizationHoldPolicyRequestDto with
        {
            Body = new UpdateOrganizationHoldPolicyRequestBodyDto()
        };

        // Act
        var updateOrganizationRequest = updateOrganizationHoldPolicyRequestDto.ToModel();

        // Assert
        updateOrganizationRequest.HoldWindow.IsSet.Should().BeFalse();
        updateOrganizationRequest.HoldExpiryVerdict.IsSet.Should().BeFalse();
    }

    [Fact]
    public void ToModel_GivenAnExplicitNullHoldPolicy_SetsBothPatchesToNull()
    {
        // Arrange
        var updateOrganizationHoldPolicyRequestDto = FakeUpdateOrganizationHoldPolicyRequestDto.CreateValid(Fixture);
        updateOrganizationHoldPolicyRequestDto = updateOrganizationHoldPolicyRequestDto with
        {
            Body = new UpdateOrganizationHoldPolicyRequestBodyDto
            {
                HoldWindow = Patch<int?>.Set(null),
                HoldExpiryVerdict = Patch<string?>.Set(null)
            }
        };

        // Act
        var updateOrganizationRequest = updateOrganizationHoldPolicyRequestDto.ToModel();

        // Assert
        updateOrganizationRequest.HoldWindow.IsSet.Should().BeTrue();
        updateOrganizationRequest.HoldWindow.Value.Should().BeNull();
        updateOrganizationRequest.HoldExpiryVerdict.IsSet.Should().BeTrue();
        updateOrganizationRequest.HoldExpiryVerdict.Value.Should().BeNull();
    }
}
