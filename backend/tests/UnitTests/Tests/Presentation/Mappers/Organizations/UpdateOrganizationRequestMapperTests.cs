using Core.Enums;
using Core.ValueObjects;
using FluentAssertions;
using Presentation.Mappers.Organizations;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Presentation.Models.Organizations;

namespace UnitTests.Tests.Presentation.Mappers.Organizations;

public class UpdateOrganizationRequestMapperTests : BaseTestClass
{
    [Fact]
    public void ToModel_Organization()
    {
        // Arrange
        var updateOrganizationRequestDto = FakeUpdateOrganizationRequestDto.CreateValid(Fixture);

        // Act
        var updateOrganizationRequest = updateOrganizationRequestDto.ToModel();

        // Assert
        updateOrganizationRequest.OrganizationId.Should().Be(new OrganizationId(updateOrganizationRequestDto.OrganizationId));
        updateOrganizationRequest.Status.Should().Be(Enum.Parse<OrganizationStatus>(updateOrganizationRequestDto.Body.Status!));
    }
}