using Core.Enums;
using Core.ValueObjects;
using FluentAssertions;
using Presentation.Mappers.Cases;
using Presentation.Models.Cases;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Presentation.Models.Cases;

namespace UnitTests.Tests.Presentation.Mappers.Cases;

public class ResolveCaseRequestMapperTests : BaseTestClass
{
    [Fact]
    public void ToModel_ResolveCaseRequestDto()
    {
        // Arrange
        var resolveCaseRequestDto = FakeResolveCaseRequestDto.CreateValid(Fixture);

        // Act
        var resolveCaseRequest = resolveCaseRequestDto.ToModel();

        // Assert
        resolveCaseRequest.CaseId.Should().Be(new CaseId(resolveCaseRequestDto.CaseId));
        resolveCaseRequest.Status.Should().Be(CaseStatus.Approved);
        resolveCaseRequest.ResolvedByUserId.Should().Be(new ApplicationUserId(resolveCaseRequestDto.Body.UserId));
    }

    [Theory]
    [InlineData("Approved", CaseStatus.Approved)]
    [InlineData("Rejected", CaseStatus.Rejected)]
    [InlineData("approved", CaseStatus.Approved)]
    [InlineData("REJECTED", CaseStatus.Rejected)]
    public void ToModel_ResolveCaseRequestDto_ParsesTheStatusCaseInsensitively(string status, CaseStatus expected)
    {
        // Arrange
        var resolveCaseRequestDto = FakeResolveCaseRequestDto.CreateValid(Fixture) with
        {
            Body = new ResolveCaseRequestBodyDto
            {
                Status = status,
                Disposition = CaseDisposition.FalsePositive.ToString()
            }
        };

        // Act
        var resolveCaseRequest = resolveCaseRequestDto.ToModel();

        // Assert
        resolveCaseRequest.Status.Should().Be(expected);
    }
}
