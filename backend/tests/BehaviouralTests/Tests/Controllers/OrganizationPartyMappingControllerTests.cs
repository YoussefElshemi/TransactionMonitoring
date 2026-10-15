using System.Net;
using System.Net.Http.Json;
using AutoFixture;
using BehaviouralTests.TestHelpers;
using Core.Enums;
using FluentAssertions;
using Infrastructure;
using Infrastructure.Configurations;
using Infrastructure.Entities;
using Microsoft.Extensions.DependencyInjection;
using Presentation.Constants;
using Presentation.Models.AuditLogs;
using Presentation.Models.Common;
using Presentation.Models.PartyMappings;
using Presentation.Models.TransactionSchemas;
using UnitTests.TestHelpers.FakeObjects.Infrastructure.Entities;

namespace BehaviouralTests.Tests.Controllers;

public class OrganizationPartyMappingControllerTests(TestFixture testFixture) : IClassFixture<TestFixture>, IAsyncLifetime
{
    private readonly Fixture _fixture = AutoFixtureHelper.Create();
    private readonly HttpClient _httpClient = testFixture.CreateClient();

    private const string Email = "username@mail.com";
    private const string Password = "P@$$w0rd";
    private static readonly Guid OrganizationId = new("4d6389e9-f5d1-4325-a004-915865e3adec");

    [Fact]
    public async Task SetPartyMapping_NotAuthenticated_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _httpClient.PutAsJsonAsync($"/organizations/{OrganizationId}/party-mapping", ValidBody());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task SetPartyMapping_NotAdmin_ShouldReturnForbidden()
    {
        // Arrange
        await AuthenticateAsync(UserRole.User);

        // Act
        var response = await _httpClient.PutAsJsonAsync($"/organizations/{OrganizationId}/party-mapping", ValidBody());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task SetPartyMapping_GivenValidRequest_ShouldReturnOkAndPersistVersionOne()
    {
        // Arrange
        await AuthenticateAsync(UserRole.Admin);

        // Act
        var response = await _httpClient.PutAsJsonAsync($"/organizations/{OrganizationId}/party-mapping", ValidBody());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var resource = await response.Content.ReadDataFromJsonAsync<PartyMappingResourceDto>();
        resource.Should().NotBeNull();
        resource!.PartyMapping.CurrentVersionNumber.Should().Be(1);
        resource.PartyMapping.Mappings.Should().HaveCount(2);

        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();
        context.PartyMappings.Should().ContainSingle(x => x.OrganizationId == OrganizationId);
        context.PartyMappingVersions.Should().ContainSingle(x => x.OrganizationId == OrganizationId);
    }

    [Fact]
    public async Task SetPartyMapping_GivenTheSameRoleTwice_ShouldReturnBadRequest()
    {
        // Arrange
        await AuthenticateAsync(UserRole.Admin);
        var body = new SetPartyMappingRequestBodyDto
        {
            Mappings =
            [
                new PartyRoleMappingDto { Role = nameof(PartyRole.Remitter), Path = "sender", IdentifierPath = "accountId" },
                new PartyRoleMappingDto { Role = nameof(PartyRole.Remitter), Path = "payer", IdentifierPath = "id" }
            ]
        };

        // Act
        var response = await _httpClient.PutAsJsonAsync($"/organizations/{OrganizationId}/party-mapping", body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SetPartyMapping_GivenAnUnknownRole_ShouldReturnBadRequest()
    {
        // Arrange
        await AuthenticateAsync(UserRole.Admin);
        var body = new SetPartyMappingRequestBodyDto
        {
            Mappings = [new PartyRoleMappingDto { Role = "Payer", IdentifierPath = "accountId" }]
        };

        // Act
        var response = await _httpClient.PutAsJsonAsync($"/organizations/{OrganizationId}/party-mapping", body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SetPartyMapping_GivenAWildcardOnTheIdentifier_ShouldReturnBadRequest()
    {
        // Arrange
        await AuthenticateAsync(UserRole.Admin);
        var body = new SetPartyMappingRequestBodyDto
        {
            Mappings = [new PartyRoleMappingDto { Role = nameof(PartyRole.Beneficiary), IdentifierPath = "parties[*].iban" }]
        };

        // Act
        var response = await _httpClient.PutAsJsonAsync($"/organizations/{OrganizationId}/party-mapping", body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SetPartyMapping_GivenAnUnreadablePath_ShouldReturnBadRequest()
    {
        // Arrange
        await AuthenticateAsync(UserRole.Admin);
        var body = new SetPartyMappingRequestBodyDto
        {
            Mappings = [new PartyRoleMappingDto { Role = nameof(PartyRole.Remitter), Path = "parties[*", IdentifierPath = "iban" }]
        };

        // Act
        var response = await _httpClient.PutAsJsonAsync($"/organizations/{OrganizationId}/party-mapping", body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SetPartyMapping_GivenAPathTheSchemaDoesNotDeclare_ShouldReturnBadRequest()
    {
        // Arrange
        await AuthenticateAsync(UserRole.Admin);
        await SetSchemaAsync();

        var body = new SetPartyMappingRequestBodyDto
        {
            Mappings = [new PartyRoleMappingDto { Role = nameof(PartyRole.Remitter), Path = "sender", IdentifierPath = "sortCode" }]
        };

        // Act
        var response = await _httpClient.PutAsJsonAsync($"/organizations/{OrganizationId}/party-mapping", body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SetPartyMapping_GivenPathsTheSchemaDeclares_ShouldReturnOk()
    {
        // Arrange
        await AuthenticateAsync(UserRole.Admin);
        await SetSchemaAsync();

        var body = new SetPartyMappingRequestBodyDto
        {
            Mappings = [new PartyRoleMappingDto { Role = nameof(PartyRole.Remitter), Path = "sender", IdentifierPath = "accountId", DisplayNamePath = "name" }]
        };

        // Act
        var response = await _httpClient.PutAsJsonAsync($"/organizations/{OrganizationId}/party-mapping", body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task SetPartyMapping_GivenAnExistingMapping_ShouldAddTheNextVersion()
    {
        // Arrange
        await AuthenticateAsync(UserRole.Admin);
        await _httpClient.PutAsJsonAsync($"/organizations/{OrganizationId}/party-mapping", ValidBody());

        // Act
        var response = await _httpClient.PutAsJsonAsync($"/organizations/{OrganizationId}/party-mapping", ValidBody());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var resource = await response.Content.ReadDataFromJsonAsync<PartyMappingResourceDto>();
        resource!.PartyMapping.CurrentVersionNumber.Should().Be(2);

        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();
        context.PartyMappings.Should().ContainSingle(x => x.OrganizationId == OrganizationId);
        context.PartyMappingVersions.Count(x => x.OrganizationId == OrganizationId).Should().Be(2);
    }

    [Fact]
    public async Task GetPartyMappingVersion_GivenAnEarlierVersion_ShouldReturnTheMappingsOfItsDay()
    {
        // Arrange
        await AuthenticateAsync(UserRole.Admin);
        await _httpClient.PutAsJsonAsync($"/organizations/{OrganizationId}/party-mapping", ValidBody());

        var narrowed = new SetPartyMappingRequestBodyDto
        {
            Mappings = [new PartyRoleMappingDto { Role = nameof(PartyRole.Remitter), Path = "sender", IdentifierPath = "accountId" }]
        };

        await _httpClient.PutAsJsonAsync($"/organizations/{OrganizationId}/party-mapping", narrowed);

        // Act
        var response = await _httpClient.GetAsync($"/organizations/{OrganizationId}/party-mapping/versions/1");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var resource = await response.Content.ReadDataFromJsonAsync<PartyMappingVersionResourceDto>();
        resource!.PartyMappingVersion.Mappings.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetPartyMapping_GivenNoMapping_ShouldReturnNotFound()
    {
        // Arrange
        await AuthenticateAsync(UserRole.Admin);

        // Act
        var response = await _httpClient.GetAsync($"/organizations/{OrganizationId}/party-mapping");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeletePartyMapping_GivenAMapping_ShouldWithdrawItAndStillAnswerIncludeDeleted()
    {
        // Arrange
        await AuthenticateAsync(UserRole.Admin);
        await _httpClient.PutAsJsonAsync($"/organizations/{OrganizationId}/party-mapping", ValidBody());

        // Act
        var deleteResponse = await _httpClient.DeleteAsync($"/organizations/{OrganizationId}/party-mapping");

        // Assert
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var inForce = await _httpClient.GetAsync($"/organizations/{OrganizationId}/party-mapping");
        inForce.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var withdrawn = await _httpClient.GetAsync($"/organizations/{OrganizationId}/party-mapping?includeDeleted=true");
        withdrawn.StatusCode.Should().Be(HttpStatusCode.OK);

        var resource = await withdrawn.Content.ReadDataFromJsonAsync<PartyMappingResourceDto>();
        resource!.PartyMapping.IsDeleted.Should().BeTrue();
        resource.PartyMapping.DeletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task SetPartyMapping_AfterAWithdrawal_ShouldCarryOnTheVersionNumbering()
    {
        // Arrange
        await AuthenticateAsync(UserRole.Admin);
        await _httpClient.PutAsJsonAsync($"/organizations/{OrganizationId}/party-mapping", ValidBody());
        await _httpClient.DeleteAsync($"/organizations/{OrganizationId}/party-mapping");

        // Act
        var response = await _httpClient.PutAsJsonAsync($"/organizations/{OrganizationId}/party-mapping", ValidBody());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var resource = await response.Content.ReadDataFromJsonAsync<PartyMappingResourceDto>();
        resource!.PartyMapping.CurrentVersionNumber.Should().Be(2);
        resource.PartyMapping.IsDeleted.Should().BeFalse();

        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();
        context.PartyMappings.Should().ContainSingle(x => x.OrganizationId == OrganizationId);
    }

    [Fact]
    public async Task RecoverPartyMapping_GivenAWithdrawnMapping_ShouldPutItBackInForce()
    {
        // Arrange
        await AuthenticateAsync(UserRole.Admin);
        await _httpClient.PutAsJsonAsync($"/organizations/{OrganizationId}/party-mapping", ValidBody());
        await _httpClient.DeleteAsync($"/organizations/{OrganizationId}/party-mapping");

        // Act
        var response = await _httpClient.PostAsync($"/organizations/{OrganizationId}/party-mapping/recover", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var inForce = await _httpClient.GetAsync($"/organizations/{OrganizationId}/party-mapping");
        inForce.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ListPartyMappingVersions_GivenTwoVersions_ShouldReturnBoth()
    {
        // Arrange
        await AuthenticateAsync(UserRole.Admin);
        await _httpClient.PutAsJsonAsync($"/organizations/{OrganizationId}/party-mapping", ValidBody());
        await _httpClient.PutAsJsonAsync($"/organizations/{OrganizationId}/party-mapping", ValidBody());

        // Act
        var response = await _httpClient.GetAsync($"/organizations/{OrganizationId}/party-mapping/versions");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var paged = await response.Content.ReadFromJsonAsync<PagedResponseDto<PartyMappingVersionResourceDto>>();
        paged!.Data.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetPartyMappingHistory_AfterAWithdrawal_ShouldStillAnswer()
    {
        // Arrange
        await AuthenticateAsync(UserRole.Admin);
        await _httpClient.PutAsJsonAsync($"/organizations/{OrganizationId}/party-mapping", ValidBody());
        await _httpClient.DeleteAsync($"/organizations/{OrganizationId}/party-mapping");

        // Act
        var response = await _httpClient.GetAsync($"/organizations/{OrganizationId}/party-mapping/history");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var paged = await response.Content.ReadFromJsonAsync<PagedResponseDto<AuditLogResourceDto>>();
        paged!.Data.Should().NotBeEmpty();
    }

    [Fact]
    public async Task SetMyPartyMapping_AsManager_ShouldReturnOk()
    {
        // Arrange
        await AuthenticateAsync(UserRole.Manager);

        // Act
        var response = await _httpClient.PutAsJsonAsync("/organizations/my/party-mapping", ValidBody());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var resource = await response.Content.ReadDataFromJsonAsync<PartyMappingResourceDto>();
        resource!.PartyMapping.OrganizationId.Should().Be(OrganizationId);
    }

    private async Task SetSchemaAsync()
    {
        var body = new SetTransactionSchemaRequestBodyDto
        {
            Fields =
            [
                new SchemaFieldDto
                {
                    Name = "sender",
                    Type = nameof(SchemaFieldType.Object),
                    IsRequired = true,
                    Fields =
                    [
                        new SchemaFieldDto { Name = "accountId", Type = nameof(SchemaFieldType.String), IsRequired = true },
                        new SchemaFieldDto { Name = "name", Type = nameof(SchemaFieldType.String), IsRequired = false }
                    ]
                }
            ]
        };

        var response = await _httpClient.PutAsJsonAsync($"/organizations/{OrganizationId}/transaction-schema", body);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private static SetPartyMappingRequestBodyDto ValidBody()
    {
        return new SetPartyMappingRequestBodyDto
        {
            Mappings =
            [
                new PartyRoleMappingDto
                {
                    Role = nameof(PartyRole.Remitter),
                    Path = "sender",
                    IdentifierPath = "accountId",
                    DisplayNamePath = "name"
                },
                new PartyRoleMappingDto
                {
                    Role = nameof(PartyRole.Beneficiary),
                    Path = "parties[*]",
                    IdentifierPath = "iban",
                    DisplayNamePath = "name"
                }
            ]
        };
    }

    private async Task AuthenticateAsync(params UserRole[] roles)
    {
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User, .. roles], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);
    }

    public async Task InitializeAsync()
    {
        await testFixture.ResetDatabaseAsync();

        var organizationEntities = new List<OrganizationEntity>
        {
            OrganizationConfigurations.DefaultOrganization,
            FakeOrganizationEntity.CreateValid(_fixture) with
            {
                OrganizationId = OrganizationId,
                StatusId = (int)OrganizationStatus.Enabled,
                IsDeleted = false
            }
        };

        await DatabaseHelper.CreateOrganizations(testFixture.Services, organizationEntities);
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }
}
