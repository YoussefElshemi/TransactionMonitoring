using FluentAssertions;
using Presentation.Mappers.Cases;
using Presentation.Mappers.RuleEvaluations;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Cases;

namespace UnitTests.Tests.Presentation.Mappers.Cases;

public class CaseDtoMapperTests : BaseTestClass
{
    [Fact]
    public void ToDto_Case()
    {
        // Arrange
        var @case = FakeCase.CreateValid(Fixture);

        // Act
        var caseDto = @case.ToDto();

        caseDto.CaseId.Should().Be(@case.CaseId);
        caseDto.FriendlyName.Should().Be(@case.FriendlyName);
        caseDto.Status.Should().Be(@case.Status.ToString());
        caseDto.Severity.Should().Be(@case.Severity.ToString());
        caseDto.TransactionId.Should().Be(@case.TransactionId!.Value);
        caseDto.RuleEvaluations.Should().BeEquivalentTo(@case.RuleEvaluations.Select(x => x.ToDto()).ToArray());
        caseDto.CreatedAt.Should().Be(@case.CreatedAt);
        caseDto.UpdatedAt.Should().Be(@case.UpdatedAt);
        caseDto.AssignedAt.Should().Be(@case.AssignedAt);
        caseDto.ResolvedAt.Should().Be(@case.ResolvedAt);
    }
}