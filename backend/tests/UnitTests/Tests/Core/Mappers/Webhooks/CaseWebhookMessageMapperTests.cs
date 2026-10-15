using Core.Mappers.Webhooks;
using FluentAssertions;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Cases;

namespace UnitTests.Tests.Core.Mappers.Webhooks;

public class CaseWebhookMessageMapperTests : BaseTestClass
{
    [Fact]
    public void ToWebhook_Case()
    {
        // Arrange
        var @case = FakeCase.CreateValid(Fixture);

        // Act
        var caseWebhookMessage = @case.ToWebhook();

        // Assert
        caseWebhookMessage.CaseId.Should().Be(@case.CaseId);
        caseWebhookMessage.FriendlyName.Should().Be(@case.FriendlyName);
        caseWebhookMessage.Status.Should().Be(@case.Status.ToString());
        caseWebhookMessage.Severity.Should().Be(@case.Severity.ToString());
        caseWebhookMessage.OrganizationId.Should().Be(@case.OrganizationId);
        caseWebhookMessage.AssignedToUser.Should().BeEquivalentTo(@case.AssignedToUser!.ToWebhook());
        caseWebhookMessage.CreatedAt.Should().Be(@case.CreatedAt);
        caseWebhookMessage.UpdatedAt.Should().Be(@case.UpdatedAt);
        caseWebhookMessage.AssignedAt.Should().Be(@case.AssignedAt);
        caseWebhookMessage.ResolvedAt.Should().Be(@case.ResolvedAt);
    }
}