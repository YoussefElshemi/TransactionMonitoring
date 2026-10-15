using System.Net;
using System.Net.Http.Json;
using AutoFixture;
using BehaviouralTests.TestHelpers;
using Core.Constants;
using Core.Enums;
using FluentAssertions;
using Infrastructure;
using Infrastructure.Configurations;
using Infrastructure.Entities;
using Microsoft.Extensions.DependencyInjection;
using Presentation.Constants;
using Presentation.Models.CaseNotes;
using Presentation.Models.Common;
using UnitTests.TestHelpers.FakeObjects.Infrastructure.Entities;
using UnitTests.TestHelpers.FakeObjects.Presentation.Models.CaseNotes;

namespace BehaviouralTests.Tests.Controllers;

public class CaseNoteControllerTests(TestFixture testFixture) : IClassFixture<TestFixture>, IAsyncLifetime
{
    private readonly Fixture _fixture = AutoFixtureHelper.Create();
    private readonly HttpClient _httpClient = testFixture.CreateClient();

    private const string Email = "username@mail.com";
    private const string Password = "P@$$w0rd";
    private static readonly Guid OrganizationId = new("4d6389e9-f5d1-4325-a004-915865e3adec");
    private static readonly Guid DisabledOrganizationId = new("03a9b48e-4af2-40bd-a180-cac58bd930f3");

    [Fact]
    public async Task CreateCaseNote_NotAuthenticated_ShouldReturnUnauthorized()
    {
        // Arrange
        var createRequest = FakeCreateCaseNoteRequestDto.CreateValid(_fixture);

        // Act
        var response = await _httpClient.PostAsJsonAsync($"/cases/{_fixture.Create<Guid>()}/notes", createRequest.Body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task CreateCaseNote_DoesNotBelongToOrganization_ShouldReturnForbidden()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User], SystemConstants.DefaultOrganization);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);
        var createRequest = FakeCreateCaseNoteRequestDto.CreateValid(_fixture);

        // Act
        var response = await _httpClient.PostAsJsonAsync($"/cases/{_fixture.Create<Guid>()}/notes", createRequest.Body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task CreateCaseNote_BelongsToDisabledOrganization_ShouldReturnForbidden()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User], DisabledOrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);
        var createRequest = FakeCreateCaseNoteRequestDto.CreateValid(_fixture);

        // Act
        var response = await _httpClient.PostAsJsonAsync($"/cases/{_fixture.Create<Guid>()}/notes", createRequest.Body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task CreateCaseNote_GivenInvalidRequest_ShouldReturnBadRequest()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);
        var caseEntity = FakeCaseEntity.CreateValid(_fixture) with
        {
            OrganizationId = OrganizationId,
            Organization = null!,
            IsDeleted = false
        };

        await DatabaseHelper.CreateCases(testFixture.Services, [caseEntity]);
        var createRequest = FakeCreateCaseNoteRequestDto.CreateInvalid(_fixture);

        // Act
        var response = await _httpClient.PostAsJsonAsync($"/cases/{caseEntity.CaseId}/notes", createRequest.Body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task CreateCaseNote_GivenValidRequest_ShouldReturnCreated()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);
        var caseEntity = FakeCaseEntity.CreateValid(_fixture) with
        {
            OrganizationId = OrganizationId,
            Organization = null!,
            IsDeleted = false
        };

        await DatabaseHelper.CreateCases(testFixture.Services, [caseEntity]);
        var createRequest = FakeCreateCaseNoteRequestDto.CreateValid(_fixture);

        // Act
        var response = await _httpClient.PostAsJsonAsync($"/cases/{caseEntity.CaseId}/notes", createRequest.Body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);

        var createdCaseNote = await response.Content.ReadDataFromJsonAsync<CaseNoteResourceDto>();
        createdCaseNote.Should().NotBeNull();
        createdCaseNote!.CaseNote.Message.Should().Be(createRequest.Body.Message);

        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();
        var caseNoteEntity = context.CaseNotes.FirstOrDefault(x => x.CaseNoteId == createdCaseNote.CaseNote.CaseNoteId);
        caseNoteEntity.Should().NotBeNull();
    }

    [Fact]
    public async Task ListCaseNote_NotAuthenticated_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _httpClient.GetAsync($"/cases/{_fixture.Create<Guid>()}/notes");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task ListCaseNote_DoesNotBelongToOrganization_ShouldReturnForbidden()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User], SystemConstants.DefaultOrganization);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);

        // Act
        var response = await _httpClient.GetAsync($"/cases/{_fixture.Create<Guid>()}/notes");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task ListCaseNote_BelongsToDisabledOrganization_ShouldReturnForbidden()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User], DisabledOrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);

        // Act
        var response = await _httpClient.GetAsync($"/cases/{_fixture.Create<Guid>()}/notes");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task ListCaseNote_GivenInvalidRequest_ShouldReturnBadRequest()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);
        var caseEntity = FakeCaseEntity.CreateValid(_fixture) with
        {
            OrganizationId = OrganizationId,
            Organization = null!,
            IsDeleted = false
        };

        await DatabaseHelper.CreateCases(testFixture.Services, [caseEntity]);
        await DatabaseHelper.CreateCaseNotes(testFixture.Services, Enumerable.Range(0, 50).Select(
            _ => FakeCaseNoteEntity.CreateValid(_fixture) with
            {
                OrganizationId = OrganizationId,
                Organization = null!,
                CaseId = caseEntity.CaseId,
                Case = null!,
                IsDeleted = false
            })
        );

        // Act
        var response = await _httpClient.GetAsync($"/cases/{caseEntity.CaseId}/notes?PageSize=200");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task ListCaseNote_GivenValidRequest_ShouldReturnOk()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);
        var caseEntity = FakeCaseEntity.CreateValid(_fixture) with
        {
            OrganizationId = OrganizationId,
            Organization = null!,
            IsDeleted = false
        };

        await DatabaseHelper.CreateCases(testFixture.Services, [caseEntity]);
        await DatabaseHelper.CreateCaseNotes(testFixture.Services, Enumerable.Range(0, 50).Select(
            _ => FakeCaseNoteEntity.CreateValid(_fixture) with
            {
                OrganizationId = OrganizationId,
                Organization = null!,
                CaseId = caseEntity.CaseId,
                Case = null!,
                IsDeleted = false
            })
        );

        // Act
        var response = await _httpClient.GetAsync($"/cases/{caseEntity.CaseId}/notes");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);

        var caseNotes = await response.Content.ReadFromJsonAsync<PagedResponseDto<CaseNoteResourceDto>>();
        caseNotes.Should().NotBeNull();
        caseNotes!.Metadata.Should().NotBeNull();
        caseNotes.Metadata.PageCount.Should().Be(5);
        caseNotes.Metadata.PageNumber.Should().Be(1);
        caseNotes.Metadata.PageSize.Should().Be(10);
        caseNotes.Metadata.TotalCount.Should().Be(50);
        caseNotes.Metadata.HasMore.Should().BeTrue();
    }

    [Fact]
    public async Task GetCaseNote_NotAuthenticated_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _httpClient.GetAsync($"/cases/{_fixture.Create<Guid>()}/notes/{_fixture.Create<Guid>()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task GetCaseNote_DoesNotBelongToOrganization_ShouldReturnForbidden()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User], SystemConstants.DefaultOrganization);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);

        // Act
        var response = await _httpClient.GetAsync($"/cases/{_fixture.Create<Guid>()}/notes/{_fixture.Create<Guid>()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task GetCaseNote_BelongsToDisabledOrganization_ShouldReturnForbidden()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User], DisabledOrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);

        // Act
        var response = await _httpClient.GetAsync($"/cases/{_fixture.Create<Guid>()}/notes/{_fixture.Create<Guid>()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task GetCaseNote_GivenValidRequest_ShouldReturnOk()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);

        var caseNoteEntity = FakeCaseNoteEntity.CreateValid(_fixture) with
        {
            Case = FakeCaseEntity.CreateValid(_fixture) with
            {
                OrganizationId = OrganizationId,
                Organization = null!,
                IsDeleted = false
            },
            OrganizationId = OrganizationId,
            Organization = null!,
            IsDeleted = false
        };

        await DatabaseHelper.CreateCaseNotes(testFixture.Services, [caseNoteEntity]);

        // Act
        var response = await _httpClient.GetAsync($"/cases/{caseNoteEntity.CaseId}/notes/{caseNoteEntity.CaseNoteId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);

        var caseNote = await response.Content.ReadDataFromJsonAsync<CaseNoteResourceDto>();
        caseNote.Should().NotBeNull();
        caseNote!.CaseNote.CaseNoteId.Should().Be(caseNoteEntity.CaseNoteId);
        caseNote.CaseNote.Message.Should().Be(caseNoteEntity.Message);
    }

    [Fact]
    public async Task UpdateCaseNote_NotAuthenticated_ShouldReturnUnauthorized()
    {
        // Arrange
        var updateRequest = FakeUpdateCaseNoteRequestDto.CreateValid(_fixture);

        // Act
        var response = await _httpClient.PatchAsJsonAsync($"/cases/{_fixture.Create<Guid>()}/notes/{updateRequest.CaseNoteId}", updateRequest.Body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task UpdateCaseNote_DoesNotBelongToOrganization_ShouldReturnForbidden()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User], SystemConstants.DefaultOrganization);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);
        var updateRequest = FakeUpdateCaseNoteRequestDto.CreateValid(_fixture);

        // Act
        var response = await _httpClient.PatchAsJsonAsync($"/cases/{_fixture.Create<Guid>()}/notes/{updateRequest.CaseNoteId}", updateRequest.Body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task UpdateCaseNote_BelongsToDisabledOrganization_ShouldReturnForbidden()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User], DisabledOrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);
        var updateRequest = FakeUpdateCaseNoteRequestDto.CreateValid(_fixture);

        // Act
        var response = await _httpClient.PatchAsJsonAsync($"/cases/{_fixture.Create<Guid>()}/notes/{updateRequest.CaseNoteId}", updateRequest.Body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task UpdateCaseNote_GivenInvalidRequest_ShouldReturnBadRequest()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);
        var caseNoteEntity = FakeCaseNoteEntity.CreateValid(_fixture) with
        {
            Case = FakeCaseEntity.CreateValid(_fixture) with
            {
                OrganizationId = OrganizationId,
                Organization = null!,
                IsDeleted = false
            },
            OrganizationId = OrganizationId,
            Organization = null!,
            IsDeleted = false
        };

        await DatabaseHelper.CreateCaseNotes(testFixture.Services, [caseNoteEntity]);

        var updateRequest = FakeUpdateCaseNoteRequestDto.CreateInvalid(_fixture) with
        {
            CaseNoteId = caseNoteEntity.CaseNoteId,
            OrganizationId = OrganizationId
        };

        // Act
        var response = await _httpClient.PatchAsJsonAsync($"/cases/{caseNoteEntity.CaseId}/notes/{updateRequest.CaseNoteId}", updateRequest.Body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task UpdateCaseNote_GivenValidRequest_ShouldReturnNoContent()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);
        var caseNoteEntity = FakeCaseNoteEntity.CreateValid(_fixture) with
        {
            Case = FakeCaseEntity.CreateValid(_fixture) with
            {
                OrganizationId = OrganizationId,
                Organization = null!,
                IsDeleted = false
            },
            OrganizationId = OrganizationId,
            Organization = null!,
            IsDeleted = false
        };

        await DatabaseHelper.CreateCaseNotes(testFixture.Services, [caseNoteEntity]);

        var updateRequest = FakeUpdateCaseNoteRequestDto.CreateValid(_fixture) with
        {
            CaseNoteId = caseNoteEntity.CaseNoteId,
            OrganizationId = OrganizationId
        };

        // Act
        var response = await _httpClient.PatchAsJsonAsync($"/cases/{caseNoteEntity.CaseId}/notes/{updateRequest.CaseNoteId}", updateRequest.Body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);

        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();
        var updatedCaseNoteEntity = context.CaseNotes.FirstOrDefault(x => x.CaseNoteId == updateRequest.CaseNoteId);
        updatedCaseNoteEntity.Should().NotBeNull();
        updatedCaseNoteEntity!.Message.Should().Be(updateRequest.Body.Message);
    }

    [Fact]
    public async Task DeleteCaseNote_NotAuthenticated_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _httpClient.DeleteAsync($"/cases/{_fixture.Create<Guid>()}/notes/{_fixture.Create<Guid>()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task DeleteCaseNote_DoesNotBelongToOrganization_ShouldReturnForbidden()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User], SystemConstants.DefaultOrganization);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);

        // Act
        var response = await _httpClient.DeleteAsync($"/cases/{_fixture.Create<Guid>()}/notes/{_fixture.Create<Guid>()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task DeleteCaseNote_BelongsToDisabledOrganization_ShouldReturnForbidden()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User], DisabledOrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);

        // Act
        var response = await _httpClient.DeleteAsync($"/cases/{_fixture.Create<Guid>()}/notes/{_fixture.Create<Guid>()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task DeleteCaseNote_GivenValidRequest_ShouldReturnNoContent()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);
        var caseNoteEntity = FakeCaseNoteEntity.CreateValid(_fixture) with
        {
            Case = FakeCaseEntity.CreateValid(_fixture) with
            {
                OrganizationId = OrganizationId,
                Organization = null!,
                IsDeleted = false
            },
            OrganizationId = OrganizationId,
            Organization = null!,
            IsDeleted = false
        };

        await DatabaseHelper.CreateCaseNotes(testFixture.Services, [caseNoteEntity]);

        // Act
        var response = await _httpClient.DeleteAsync($"/cases/{caseNoteEntity.CaseId}/notes/{caseNoteEntity.CaseNoteId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);

        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();
        var updatedCaseNoteEntity = context.CaseNotes.FirstOrDefault(x => x.CaseNoteId == caseNoteEntity.CaseNoteId);
        updatedCaseNoteEntity.Should().NotBeNull();
        updatedCaseNoteEntity!.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task RecoverCaseNote_NotAuthenticated_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _httpClient.PostAsync($"/cases/{_fixture.Create<Guid>()}/notes/{_fixture.Create<Guid>()}/recover", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task RecoverCaseNote_DoesNotBelongToOrganization_ShouldReturnForbidden()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User], SystemConstants.DefaultOrganization);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);

        // Act
        var response = await _httpClient.PostAsync($"/cases/{_fixture.Create<Guid>()}/notes/{_fixture.Create<Guid>()}/recover", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task RecoverCaseNote_BelongsToDisabledOrganization_ShouldReturnForbidden()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User], DisabledOrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);

        // Act
        var response = await _httpClient.PostAsync($"/cases/{_fixture.Create<Guid>()}/notes/{_fixture.Create<Guid>()}/recover", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task RecoverCaseNote_GivenValidRequest_ShouldReturnNoContent()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);
        var caseNoteEntity = FakeCaseNoteEntity.CreateValid(_fixture) with
        {
            Case = FakeCaseEntity.CreateValid(_fixture) with
            {
                OrganizationId = OrganizationId,
                Organization = null!,
                IsDeleted = false
            },
            OrganizationId = OrganizationId,
            Organization = null!,
            IsDeleted = true
        };

        await DatabaseHelper.CreateCaseNotes(testFixture.Services, [caseNoteEntity]);

        // Act
        var response = await _httpClient.PostAsync($"/cases/{caseNoteEntity.CaseId}/notes/{caseNoteEntity.CaseNoteId}/recover", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);

        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();
        var updatedCaseNoteEntity = context.CaseNotes.FirstOrDefault(x => x.CaseNoteId == caseNoteEntity.CaseNoteId);
        updatedCaseNoteEntity.Should().NotBeNull();
        updatedCaseNoteEntity!.IsDeleted.Should().BeFalse();
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
            },
            FakeOrganizationEntity.CreateValid(_fixture) with
            {
                OrganizationId = DisabledOrganizationId,
                StatusId = (int)OrganizationStatus.Disabled,
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
