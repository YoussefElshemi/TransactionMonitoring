using Core.Enums;
using Core.Helpers;
using Core.ValueObjects;
using FluentAssertions;
using UnitTests.TestHelpers;

namespace UnitTests.Tests.Core.Helpers;

public class ApiClientCredentialHelperTests : BaseTestClass
{
    [Fact]
    public void GenerateClientId_Always_ReturnsPrefixedHex()
    {
        // Act
        string clientId = ApiClientCredentialHelper.GenerateClientId(Mode.Live);

        // Assert
        clientId.Should().StartWith("tmc_");
        clientId["tmc_".Length..].Should().MatchRegex("^[0-9a-f]{32}$");
    }

    [Fact]
    public void GenerateClientSecret_Always_ReturnsPrefixed256BitHex()
    {
        // Act
        string clientSecret = ApiClientCredentialHelper.GenerateClientSecret(Mode.Live);

        // Assert
        clientSecret.Should().StartWith("tmsec_");
        clientSecret["tmsec_".Length..].Should().MatchRegex("^[0-9a-f]{64}$");
    }

    [Fact]
    public void GenerateClientSecret_CalledRepeatedly_ReturnsDistinctValues()
    {
        // Act
        var secrets = Enumerable.Range(0, 100)
            .Select(_ => (string)ApiClientCredentialHelper.GenerateClientSecret(Mode.Live))
            .ToList();

        // Assert
        secrets.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void Verify_GivenTheSameSecret_ReturnsTrue()
    {
        // Arrange
        var clientSecret = ApiClientCredentialHelper.GenerateClientSecret(Mode.Live);
        var secretHash = ApiClientCredentialHelper.Hash(clientSecret);

        // Act
        var verified = ApiClientCredentialHelper.Verify(clientSecret, secretHash);

        // Assert
        verified.Should().BeTrue();
    }

    [Fact]
    public void Verify_GivenADifferentSecret_ReturnsFalse()
    {
        // Arrange
        var secretHash = ApiClientCredentialHelper.Hash(ApiClientCredentialHelper.GenerateClientSecret(Mode.Live));

        // Act
        var verified = ApiClientCredentialHelper.Verify(ApiClientCredentialHelper.GenerateClientSecret(Mode.Live), secretHash);

        // Assert
        verified.Should().BeFalse();
    }

    [Fact]
    public void Hash_Always_DoesNotContainThePlaintext()
    {
        // Arrange
        var clientSecret = ApiClientCredentialHelper.GenerateClientSecret(Mode.Live);

        // Act
        string secretHash = ApiClientCredentialHelper.Hash(clientSecret);

        // Assert
        secretHash.Should().NotContain(clientSecret);
        secretHash.Should().MatchRegex("^[0-9a-f]{64}$");
    }

    [Fact]
    public void ToSecretPrefix_Always_KeepsOnlyTheLeadingCharacters()
    {
        // Arrange
        var clientSecret = new ClientSecret("tmsec_0123456789abcdef");

        // Act
        string secretPrefix = ApiClientCredentialHelper.ToSecretPrefix(clientSecret);

        // Assert
        secretPrefix.Should().Be("tmsec_012345");
    }
}
