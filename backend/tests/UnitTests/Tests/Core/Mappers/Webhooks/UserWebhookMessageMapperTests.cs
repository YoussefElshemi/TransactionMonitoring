using Core.Mappers.Webhooks;
using FluentAssertions;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Users;

namespace UnitTests.Tests.Core.Mappers.Webhooks;

public class UserWebhookMessageMapperTests : BaseTestClass
{
    [Fact]
    public void ToWebhook_User()
    {
        // Arrange
        var user = FakeApplicationUser.CreateValid(Fixture);

        // Act
        var userWebhookMessage = user.ToWebhook();

        // Assert
        userWebhookMessage.UserId.Should().Be(user.UserId);
        userWebhookMessage.Email.Should().Be(user.Email);
        userWebhookMessage.UserName.Should().Be(user.UserName);
    }
}