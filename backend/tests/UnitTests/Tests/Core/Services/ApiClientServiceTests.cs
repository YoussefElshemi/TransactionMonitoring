using Core.Enums;
using Core.Helpers;
using Core.Interfaces;
using Core.Interfaces.Repositories;
using Core.Models.ApiClients;
using Core.Services;
using Core.ValueObjects;
using FluentAssertions;
using Moq;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Core.Models.ApiClients;

namespace UnitTests.Tests.Core.Services;

public class ApiClientServiceTests : BaseTestClass
{
    private readonly Mock<IApiClientRepository> _mockApiClientRepository = new();
    private readonly Mock<IUnitOfWork> _mockUnitOfWork = new();
    private readonly ApiClientService _sut;

    public ApiClientServiceTests()
    {
        _sut = new ApiClientService(_mockApiClientRepository.Object, _mockUnitOfWork.Object, MockTimeProvider, TestModeContext);
    }

    [Fact]
    public async Task CreateAsync_Always_ReturnsAPlaintextSecretThatIsNotPersisted()
    {
        // Arrange
        var request = FakeCreateApiClientRequest.CreateValid(Fixture);
        ApiClientSecret? persisted = null;

        _mockApiClientRepository
            .Setup(x => x.AddSecretAsync(It.IsAny<ApiClientSecret>(), It.IsAny<CancellationToken>()))
            .Callback<ApiClientSecret, CancellationToken>((secret, _) => persisted = secret);

        _mockApiClientRepository
            .Setup(x => x.GetAsync(It.IsAny<ApiClientId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FakeApiClient.CreateValid(Fixture));

        // Act
        var created = await _sut.CreateAsync(request, CancellationToken.None);

        // Assert
        ((string)created.ClientSecret).Should().StartWith("tmsec_");
        persisted.Should().NotBeNull();
        ((string)persisted!.SecretHash).Should().Be(ApiClientCredentialHelper.Hash(created.ClientSecret));
        ((string)persisted.SecretHash).Should().NotBe(created.ClientSecret);
    }

    [Fact]
    public async Task AuthenticateAsync_ClientIdIsUnknown_ReturnsInvalidClient()
    {
        // Arrange
        _mockApiClientRepository
            .Setup(x => x.FindActiveByClientIdAsync(It.IsAny<ClientId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ApiClient?)null);

        // Act
        var result = await _sut.AuthenticateAsync(FakeAuthenticateApiClientRequest.CreateValid(Fixture), CancellationToken.None);

        // Assert
        result.Outcome.Should().Be(ApiClientAuthenticationOutcome.InvalidClient);
        result.AuthenticatedApiClient.Should().BeNull();
    }

    [Fact]
    public async Task AuthenticateAsync_SecretIsWrong_ReturnsInvalidClient()
    {
        // Arrange
        SetUpClient(ApiClientCredentialHelper.GenerateClientSecret(Mode.Live));

        var request = FakeAuthenticateApiClientRequest.CreateValid(Fixture) with
        {
            ClientSecret = ApiClientCredentialHelper.GenerateClientSecret(Mode.Live)
        };

        // Act
        var result = await _sut.AuthenticateAsync(request, CancellationToken.None);

        // Assert
        result.Outcome.Should().Be(ApiClientAuthenticationOutcome.InvalidClient);
    }

    [Fact]
    public async Task AuthenticateAsync_SecretIsCorrect_ReturnsSuccessWithGrantedScopes()
    {
        // Arrange
        var clientSecret = ApiClientCredentialHelper.GenerateClientSecret(Mode.Live);
        var apiClient = SetUpClient(clientSecret, [ApiScope.RulesEvaluate, ApiScope.RuleSetsEvaluate]);

        var request = FakeAuthenticateApiClientRequest.CreateValid(Fixture) with
        {
            ClientId = apiClient.ClientId,
            ClientSecret = clientSecret
        };

        // Act
        var result = await _sut.AuthenticateAsync(request, CancellationToken.None);

        // Assert
        result.Outcome.Should().Be(ApiClientAuthenticationOutcome.Success);
        result.AuthenticatedApiClient!.Scopes.Should().BeEquivalentTo([ApiScope.RulesEvaluate, ApiScope.RuleSetsEvaluate]);
        result.AuthenticatedApiClient.OrganizationId.Should().Be(apiClient.OrganizationId);
    }

    [Fact]
    public async Task AuthenticateAsync_SecretIsCorrect_StampsTheSecretAsUsed()
    {
        // Arrange
        var clientSecret = ApiClientCredentialHelper.GenerateClientSecret(Mode.Live);
        var apiClient = SetUpClient(clientSecret);

        var request = FakeAuthenticateApiClientRequest.CreateValid(Fixture) with { ClientSecret = clientSecret };

        // Act
        await _sut.AuthenticateAsync(request, CancellationToken.None);

        // Assert
        _mockApiClientRepository.Verify(
            x => x.StampSecretUsedAsync(apiClient.Secrets[0].ApiClientSecretId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AuthenticateAsync_SecretIsRevoked_ReturnsInvalidClient()
    {
        // Arrange
        var clientSecret = ApiClientCredentialHelper.GenerateClientSecret(Mode.Live);
        SetUpClient(clientSecret, revokedAt: new RevokedAt(MockTimeProvider.GetUtcNow()));

        var request = FakeAuthenticateApiClientRequest.CreateValid(Fixture) with { ClientSecret = clientSecret };

        // Act
        var result = await _sut.AuthenticateAsync(request, CancellationToken.None);

        // Assert
        result.Outcome.Should().Be(ApiClientAuthenticationOutcome.InvalidClient);
    }

    [Fact]
    public async Task AuthenticateAsync_SecretHasExpired_ReturnsInvalidClient()
    {
        // Arrange
        var clientSecret = ApiClientCredentialHelper.GenerateClientSecret(Mode.Live);
        SetUpClient(clientSecret, expiresAt: new ExpiresAt(MockTimeProvider.GetUtcNow().AddMinutes(-1)));

        var request = FakeAuthenticateApiClientRequest.CreateValid(Fixture) with { ClientSecret = clientSecret };

        // Act
        var result = await _sut.AuthenticateAsync(request, CancellationToken.None);

        // Assert
        result.Outcome.Should().Be(ApiClientAuthenticationOutcome.InvalidClient);
    }

    [Fact]
    public async Task AuthenticateAsync_RequestsAScopeThatWasNotGranted_ReturnsInvalidScope()
    {
        // Arrange
        var clientSecret = ApiClientCredentialHelper.GenerateClientSecret(Mode.Live);
        SetUpClient(clientSecret, [ApiScope.RulesEvaluate]);

        var request = FakeAuthenticateApiClientRequest.CreateValid(Fixture) with
        {
            ClientSecret = clientSecret,
            Scopes = [ApiScope.RuleSetsEvaluate]
        };

        // Act
        var result = await _sut.AuthenticateAsync(request, CancellationToken.None);

        // Assert
        result.Outcome.Should().Be(ApiClientAuthenticationOutcome.InvalidScope);
        result.AuthenticatedApiClient.Should().BeNull();
    }

    [Fact]
    public async Task AuthenticateAsync_RequestsASubsetOfGrantedScopes_NarrowsTheToken()
    {
        // Arrange
        var clientSecret = ApiClientCredentialHelper.GenerateClientSecret(Mode.Live);
        SetUpClient(clientSecret, [ApiScope.RulesEvaluate, ApiScope.RuleSetsEvaluate]);

        var request = FakeAuthenticateApiClientRequest.CreateValid(Fixture) with
        {
            ClientSecret = clientSecret,
            Scopes = [ApiScope.RulesEvaluate]
        };

        // Act
        var result = await _sut.AuthenticateAsync(request, CancellationToken.None);

        // Assert
        result.Outcome.Should().Be(ApiClientAuthenticationOutcome.Success);
        result.AuthenticatedApiClient!.Scopes.Should().BeEquivalentTo([ApiScope.RulesEvaluate]);
    }

    private ApiClient SetUpClient(
        ClientSecret clientSecret,
        List<ApiScope>? scopes = null,
        RevokedAt? revokedAt = null,
        ExpiresAt? expiresAt = null)
    {
        var apiClient = FakeApiClient.CreateValid(Fixture) with
        {
            Scopes = scopes ?? [ApiScope.RulesEvaluate],
            Secrets =
            [
                FakeApiClientSecret.CreateValid(Fixture) with
                {
                    SecretHash = ApiClientCredentialHelper.Hash(clientSecret),
                    RevokedAt = revokedAt,
                    ExpiresAt = expiresAt
                }
            ]
        };

        _mockApiClientRepository
            .Setup(x => x.FindActiveByClientIdAsync(It.IsAny<ClientId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(apiClient);

        return apiClient;
    }
}
