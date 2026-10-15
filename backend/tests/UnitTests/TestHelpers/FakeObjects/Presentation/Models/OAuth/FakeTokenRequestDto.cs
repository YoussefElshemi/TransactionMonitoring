using AutoFixture;
using Presentation.Constants;
using Presentation.Models.OAuth;

namespace UnitTests.TestHelpers.FakeObjects.Presentation.Models.OAuth;

public static class FakeTokenRequestDto
{
    public static TokenRequestDto CreateValid(IFixture fixture)
    {
        return new TokenRequestDto
        {
            GrantType = OAuthConstants.ClientCredentialsGrantType,
            ClientId = fixture.Create<string>(),
            ClientSecret = fixture.Create<string>()
        };
    }
}
