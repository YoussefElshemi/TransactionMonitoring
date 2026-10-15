using System.Security.Cryptography;
using System.Text;
using Core.Extensions;
using Core.Helpers;
using Core.ValueObjects;
using FluentAssertions;
using UnitTests.TestHelpers;

namespace UnitTests.Tests.Core.Helpers;

public class PayloadCipherTests : BaseTestClass
{
    private static readonly OrganizationId OrganizationId = new(Guid.NewGuid());

    [Fact]
    public void Protect_RoundTripsThroughUnprotect()
    {
        // Arrange
        var dataKey = DataKeyWrapper.Create();
        const string plaintext = """{"amount":24500,"sender":{"name":"Bob Smith"}}""";

        // Act
        var protectedPayload = PayloadCipher.Protect(dataKey, OrganizationId, plaintext);
        var unprotected = PayloadCipher.Unprotect(dataKey, OrganizationId, protectedPayload);

        // Assert
        unprotected.Should().Be(plaintext);
    }

    [Fact]
    public void Protect_DoesNotLeaveThePlaintextRecoverableFromTheEnvelope()
    {
        // Arrange
        var dataKey = DataKeyWrapper.Create();
        const string plaintext = """{"sender":{"name":"Bob Smith"}}""";

        // Act
        var protectedPayload = PayloadCipher.Protect(dataKey, OrganizationId, plaintext);

        // Assert
        protectedPayload.Should().NotContain("Bob Smith");
        Encoding.UTF8.GetString(Convert.FromBase64String(protectedPayload[PayloadCipher.Version.Length..]))
            .Should().NotContain("Bob Smith");
    }

    [Fact]
    public void Protect_ProducesADifferentEnvelopeEachTime()
    {
        // Arrange
        var dataKey = DataKeyWrapper.Create();
        const string plaintext = """{"amount":100}""";

        // Act
        var first = PayloadCipher.Protect(dataKey, OrganizationId, plaintext);
        var second = PayloadCipher.Protect(dataKey, OrganizationId, plaintext);

        // Assert
        first.Should().NotBe(second);
        PayloadCipher.Unprotect(dataKey, OrganizationId, first).Should().Be(plaintext);
        PayloadCipher.Unprotect(dataKey, OrganizationId, second).Should().Be(plaintext);
    }

    [Fact]
    public void Unprotect_RefusesAnEnvelopeBelongingToAnotherOrganisation()
    {
        // Arrange
        var dataKey = DataKeyWrapper.Create();
        var protectedPayload = PayloadCipher.Protect(dataKey, OrganizationId, """{"amount":100}""");

        // Act
        var act = () => PayloadCipher.Unprotect(dataKey, new OrganizationId(Guid.NewGuid()), protectedPayload);

        // Assert
        act.Should().Throw<CryptographicException>();
    }

    [Fact]
    public void Unprotect_RefusesAnEnvelopeUnderTheWrongKey()
    {
        // Arrange
        var protectedPayload = PayloadCipher.Protect(DataKeyWrapper.Create(), OrganizationId, """{"amount":100}""");

        // Act
        var act = () => PayloadCipher.Unprotect(DataKeyWrapper.Create(), OrganizationId, protectedPayload);

        // Assert
        act.Should().Throw<CryptographicException>();
    }

    [Fact]
    public void Unprotect_RefusesATamperedEnvelope()
    {
        // Arrange
        var dataKey = DataKeyWrapper.Create();
        var protectedPayload = PayloadCipher.Protect(dataKey, OrganizationId, """{"amount":100}""");

        var envelope = Convert.FromBase64String(protectedPayload[PayloadCipher.Version.Length..]);
        envelope[^1] ^= 0xFF;
        var tampered = PayloadCipher.Version + Convert.ToBase64String(envelope);

        // Act
        var act = () => PayloadCipher.Unprotect(dataKey, OrganizationId, tampered);

        // Assert
        act.Should().Throw<CryptographicException>();
    }

    [Fact]
    public void Unprotect_ReadsARowWrittenBeforeProtectionExisted()
    {
        // Arrange
        const string plaintext = """{"amount":100}""";
        var legacy = plaintext.ToBase64String();

        // Act
        var unprotected = PayloadCipher.Unprotect(DataKeyWrapper.Create(), OrganizationId, legacy);

        // Assert
        PayloadCipher.IsProtected(legacy).Should().BeFalse();
        unprotected.Should().Be(plaintext);
    }
}
