using System.Security.Cryptography;
using Core.Configs;
using Core.Helpers;
using Core.ValueObjects;
using FluentAssertions;
using UnitTests.TestHelpers;

namespace UnitTests.Tests.Core.Helpers;

public class DataKeyWrapperTests : BaseTestClass
{
    private static readonly OrganizationId OrganizationId = new(Guid.NewGuid());
    private static readonly byte[] MasterKey = RandomNumberGenerator.GetBytes(DataKey.ByteCount);

    [Fact]
    public void Create_MintsAKeyOfTheCiphersWidth()
    {
        // Act
        byte[] dataKey = DataKeyWrapper.Create();

        // Assert
        dataKey.Should().HaveCount(DataKey.ByteCount);
    }

    [Fact]
    public void Create_MintsADifferentKeyEachTime()
    {
        // Act
        byte[] first = DataKeyWrapper.Create();
        byte[] second = DataKeyWrapper.Create();

        // Assert
        first.Should().NotBeEquivalentTo(second);
    }

    [Fact]
    public void Wrap_RoundTripsThroughUnwrap()
    {
        // Arrange
        var dataKey = DataKeyWrapper.Create();

        // Act
        var wrapped = DataKeyWrapper.Wrap(MasterKey, OrganizationId, dataKey);
        var unwrapped = DataKeyWrapper.Unwrap(MasterKey, OrganizationId, wrapped);

        // Assert
        ((byte[])unwrapped).Should().BeEquivalentTo((byte[])dataKey);
    }

    [Fact]
    public void Wrap_FitsTheColumnItIsStoredIn()
    {
        // Act
        var wrapped = DataKeyWrapper.Wrap(MasterKey, OrganizationId, DataKeyWrapper.Create());

        // Assert
        wrapped.Length.Should().BeLessThanOrEqualTo(WrappedDataKey.MaxLength);
    }

    [Fact]
    public void Unwrap_RefusesAKeyWrappedUnderADifferentMasterKey()
    {
        // Arrange
        var wrapped = DataKeyWrapper.Wrap(MasterKey, OrganizationId, DataKeyWrapper.Create());

        // Act
        var act = () => DataKeyWrapper.Unwrap(RandomNumberGenerator.GetBytes(DataKey.ByteCount), OrganizationId, wrapped);

        // Assert
        act.Should().Throw<CryptographicException>();
    }

    [Fact]
    public void ReadMasterKey_AcceptsTheDevelopmentDefault()
    {
        // Act
        var masterKey = DataKeyWrapper.ReadMasterKey(PayloadProtectionConfig.DevelopmentMasterKey);

        // Assert
        masterKey.Should().HaveCount(DataKey.ByteCount);
        DataKeyWrapper.IsDevelopmentMasterKey(PayloadProtectionConfig.DevelopmentMasterKey).Should().BeTrue();
    }

    [Theory]
    [InlineData("not base64 at all")]
    [InlineData("dG9vLXNob3J0")]
    public void ReadMasterKey_RefusesAKeyThatIsNotThirtyTwoBytesOfBase64(string configured)
    {
        // Act
        var act = () => DataKeyWrapper.ReadMasterKey(configured);

        // Assert
        act.Should().Throw<InvalidOperationException>();
    }
}
