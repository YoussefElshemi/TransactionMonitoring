using System.Net;
using System.Net.Http.Json;
using AutoFixture;
using BehaviouralTests.TestHelpers;
using Core.Constants;
using Core.Enums;
using Core.Extensions;
using Core.Models.Webhooks;
using FluentAssertions;
using Infrastructure;
using Infrastructure.Configurations;
using Infrastructure.Entities;
using Microsoft.Extensions.DependencyInjection;
using Presentation.Constants;
using Presentation.Models.Cases;
using Presentation.Models.Common;
using UnitTests.TestHelpers.FakeObjects.Infrastructure.Entities;
using UnitTests.TestHelpers.FakeObjects.Presentation.Models.Cases;

namespace BehaviouralTests.Tests.Controllers;

public class CaseControllerTests(TestFixture testFixture) : IClassFixture<TestFixture>, IAsyncLifetime
{
    private readonly Fixture _fixture = AutoFixtureHelper.Create();
    private readonly HttpClient _httpClient = testFixture.CreateClient();

    private const string Email = "username@mail.com";
    private const string Password = "P@$$w0rd";
    private static readonly Guid OrganizationId = new("4d6389e9-f5d1-4325-a004-915865e3adec");
    private static readonly Guid DisabledOrganizationId = new("03a9b48e-4af2-40bd-a180-cac58bd930f3");

    [Fact]
    public async Task ListCase_NotAuthenticated_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _httpClient.GetAsync("/cases");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task ListCase_DoesNotBelongToOrganization_ShouldReturnForbidden()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User], SystemConstants.DefaultOrganization);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);

        // Act
        var response = await _httpClient.GetAsync("/cases");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task ListCase_BelongsToDisabledOrganization_ShouldReturnForbidden()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User], DisabledOrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);

        // Act
        var response = await _httpClient.GetAsync("/cases");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task ListCase_GivenInvalidRequest_ShouldReturnBadRequest()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);
        await DatabaseHelper.CreateCases(testFixture.Services, Enumerable.Range(0, 50).Select(
            _ => FakeCaseEntity.CreateValid(_fixture) with
            {
                OrganizationId = OrganizationId,
                Organization = null!,
                IsDeleted = false
            })
        );

        // Act
        var response = await _httpClient.GetAsync("/cases?PageSize=200");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task ListCase_GivenValidRequest_ShouldReturnOk()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);
        await DatabaseHelper.CreateCases(testFixture.Services, Enumerable.Range(0, 50).Select(
            _ => FakeCaseEntity.CreateValid(_fixture) with
            {
                OrganizationId = OrganizationId,
                Organization = null!,
                IsDeleted = false
            })
        );

        // Act
        var response = await _httpClient.GetAsync("/cases");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);

        var cases = await response.Content.ReadFromJsonAsync<PagedResponseDto<CaseResourceDto>>();
        cases.Should().NotBeNull();
        cases!.Metadata.Should().NotBeNull();
        cases.Metadata.PageCount.Should().Be(5);
        cases.Metadata.PageNumber.Should().Be(1);
        cases.Metadata.PageSize.Should().Be(10);
        cases.Metadata.TotalCount.Should().Be(50);
        cases.Metadata.HasMore.Should().BeTrue();
    }

    [Fact]
    public async Task ListMyCase_NotAuthenticated_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _httpClient.GetAsync("/cases/my");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task ListMyCase_DoesNotBelongToOrganization_ShouldReturnForbidden()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User], SystemConstants.DefaultOrganization);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);

        // Act
        var response = await _httpClient.GetAsync("/cases/my");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task ListMyCase_BelongsToDisabledOrganization_ShouldReturnForbidden()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User], DisabledOrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);

        // Act
        var response = await _httpClient.GetAsync("/cases/my");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task ListMyCase_GivenInvalidRequest_ShouldReturnBadRequest()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);
        await DatabaseHelper.CreateCases(testFixture.Services, Enumerable.Range(0, 50).Select(
            _ => FakeCaseEntity.CreateValid(_fixture) with
            {
                OrganizationId = OrganizationId,
                Organization = null!,
                IsDeleted = false
            })
        );

        // Act
        var response = await _httpClient.GetAsync("/cases/my?PageSize=200");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task ListMyCase_GivenValidRequest_ShouldReturnOk()
    {
        // Arrange
        var user = await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);
        await DatabaseHelper.CreateCases(testFixture.Services, Enumerable.Range(0, 50).Select(
            _ => FakeCaseEntity.CreateValid(_fixture) with
            {
                AssignedToUserId = user.Id,
                AssignedToUser = null,
                OrganizationId = OrganizationId,
                Organization = null!,
                IsDeleted = false
            })
        );

        // Act
        var response = await _httpClient.GetAsync("/cases/my");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);

        var cases = await response.Content.ReadFromJsonAsync<PagedResponseDto<CaseResourceDto>>();
        cases.Should().NotBeNull();
        cases!.Metadata.Should().NotBeNull();
        cases.Metadata.PageCount.Should().Be(5);
        cases.Metadata.PageNumber.Should().Be(1);
        cases.Metadata.PageSize.Should().Be(10);
        cases.Metadata.TotalCount.Should().Be(50);
        cases.Metadata.HasMore.Should().BeTrue();
    }

    [Fact]
    public async Task GetCase_NotAuthenticated_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _httpClient.GetAsync($"/cases/{_fixture.Create<Guid>()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task GetCase_DoesNotBelongToOrganization_ShouldReturnForbidden()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User], SystemConstants.DefaultOrganization);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);

        // Act
        var response = await _httpClient.GetAsync($"/cases/{_fixture.Create<Guid>()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task GetCase_BelongsToDisabledOrganization_ShouldReturnForbidden()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User], DisabledOrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);

        // Act
        var response = await _httpClient.GetAsync($"/cases/{_fixture.Create<Guid>()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task GetCase_GivenValidRequest_ShouldReturnOk()
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

        // Act
        var response = await _httpClient.GetAsync($"/cases/{caseEntity.CaseId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);

        var @case = await response.Content.ReadDataFromJsonAsync<CaseResourceDto>();
        @case.Should().NotBeNull();
        @case!.Case.CaseId.Should().Be(caseEntity.CaseId);
        @case.Case.FriendlyName.Should().Be(caseEntity.FriendlyName);
    }

    [Fact]
    public async Task AssignCase_NotAuthenticated_ShouldReturnUnauthorized()
    {
        // Arrange
        var assignRequest = FakeAssignCaseRequestDto.CreateValid(_fixture);

        // Act
        var response = await _httpClient.PostAsJsonAsync($"/cases/{assignRequest.CaseId}/assign", assignRequest.Body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task AssignCase_DoesNotBelongToOrganization_ShouldReturnForbidden()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User], SystemConstants.DefaultOrganization);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);
        var assignRequest = FakeAssignCaseRequestDto.CreateValid(_fixture);

        // Act
        var response = await _httpClient.PostAsJsonAsync($"/cases/{assignRequest.CaseId}/assign", assignRequest.Body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task AssignCase_BelongsToDisabledOrganization_ShouldReturnForbidden()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User], DisabledOrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);
        var assignRequest = FakeAssignCaseRequestDto.CreateValid(_fixture);

        // Act
        var response = await _httpClient.PostAsJsonAsync($"/cases/{assignRequest.CaseId}/assign", assignRequest.Body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task AssignCase_GivenInvalidRequest_ShouldReturnBadRequest()
    {
        // Arrange
        var user = await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);
        var caseEntity = FakeCaseEntity.CreateValid(_fixture) with
        {
            OrganizationId = OrganizationId,
            StatusId = (int)CaseStatus.Approved,
            Organization = null!,
            IsDeleted = false
        };

        await DatabaseHelper.CreateCases(testFixture.Services, [caseEntity]);

        var assignRequest = FakeAssignCaseRequestDto.CreateValid(_fixture) with
        {
            CaseId = caseEntity.CaseId,
            OrganizationId = OrganizationId,
            Body = new AssignCaseRequestBodyDto
            {
                UserId = user.Id
            }
        };

        // Act
        var response = await _httpClient.PostAsJsonAsync($"/cases/{assignRequest.CaseId}/assign", assignRequest.Body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task AssignCase_GivenValidRequest_ShouldReturnNoContent()
    {
        // Arrange
        var user = await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);
        var caseEntity = FakeCaseEntity.CreateValid(_fixture) with
        {
            AssignedToUserId = null,
            AssignedToUser = null!,
            OrganizationId = OrganizationId,
            Organization = null!,
            StatusId = (int)CaseStatus.Pending,
            IsDeleted = false
        };

        await DatabaseHelper.CreateCases(testFixture.Services, [caseEntity]);

        var assignRequest = FakeAssignCaseRequestDto.CreateValid(_fixture) with
        {
            CaseId = caseEntity.CaseId,
            OrganizationId = OrganizationId,
            Body = new AssignCaseRequestBodyDto
            {
                UserId = user.Id
            }
        };

        // Act
        var response = await _httpClient.PostAsJsonAsync($"/cases/{assignRequest.CaseId}/assign", assignRequest.Body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);

        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();
        var updatedCaseEntity = context.Cases.FirstOrDefault(x => x.CaseId == assignRequest.CaseId);
        updatedCaseEntity.Should().NotBeNull();
        updatedCaseEntity!.AssignedToUserId.Should().Be(assignRequest.Body.UserId);
    }

    [Fact]
    public async Task AssignCase_NoUserIdSupplied_ShouldAssignToCaller()
    {
        // Arrange
        var user = await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);
        var caseEntity = FakeCaseEntity.CreateValid(_fixture) with
        {
            AssignedToUserId = null,
            AssignedToUser = null!,
            OrganizationId = OrganizationId,
            Organization = null!,
            StatusId = (int)CaseStatus.Pending,
            IsDeleted = false
        };

        await DatabaseHelper.CreateCases(testFixture.Services, [caseEntity]);

        // Act
        var response = await _httpClient.PostAsJsonAsync($"/cases/{caseEntity.CaseId}/assign", new AssignCaseRequestBodyDto());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();
        var updatedCaseEntity = context.Cases.FirstOrDefault(x => x.CaseId == caseEntity.CaseId);
        updatedCaseEntity.Should().NotBeNull();
        updatedCaseEntity!.AssignedToUserId.Should().Be(user.Id);
    }

    [Fact]
    public async Task AssignCase_ManagerAssignsToAnotherUser_ShouldAssignToThatUser()
    {
        // Arrange
        var analyst = await AuthenticationHelper.CreateUserAsync(testFixture.Services, "analyst@mail.com", Password, [UserRole.User], OrganizationId);
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.Manager], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);
        var caseEntity = FakeCaseEntity.CreateValid(_fixture) with
        {
            AssignedToUserId = null,
            AssignedToUser = null!,
            OrganizationId = OrganizationId,
            Organization = null!,
            StatusId = (int)CaseStatus.Pending,
            IsDeleted = false
        };

        await DatabaseHelper.CreateCases(testFixture.Services, [caseEntity]);

        // Act
        var response = await _httpClient.PostAsJsonAsync($"/cases/{caseEntity.CaseId}/assign", new AssignCaseRequestBodyDto
        {
            UserId = analyst.Id
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();
        var updatedCaseEntity = context.Cases.FirstOrDefault(x => x.CaseId == caseEntity.CaseId);
        updatedCaseEntity.Should().NotBeNull();
        updatedCaseEntity!.AssignedToUserId.Should().Be(analyst.Id);
    }

    [Fact]
    public async Task AssignCase_UserAssignsToAnotherUser_ShouldReturnForbidden()
    {
        // Arrange
        var analyst = await AuthenticationHelper.CreateUserAsync(testFixture.Services, "analyst@mail.com", Password, [UserRole.User], OrganizationId);
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);
        var caseEntity = FakeCaseEntity.CreateValid(_fixture) with
        {
            AssignedToUserId = null,
            AssignedToUser = null!,
            OrganizationId = OrganizationId,
            Organization = null!,
            StatusId = (int)CaseStatus.Pending,
            IsDeleted = false
        };

        await DatabaseHelper.CreateCases(testFixture.Services, [caseEntity]);

        // Act
        var response = await _httpClient.PostAsJsonAsync($"/cases/{caseEntity.CaseId}/assign", new AssignCaseRequestBodyDto
        {
            UserId = analyst.Id
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();
        var updatedCaseEntity = context.Cases.FirstOrDefault(x => x.CaseId == caseEntity.CaseId);
        updatedCaseEntity.Should().NotBeNull();
        updatedCaseEntity!.AssignedToUserId.Should().BeNull();
    }

    [Fact]
    public async Task UnassignCase_NotAuthenticated_ShouldReturnUnauthorized()
    {
        // Arrange
        var unassignRequest = FakeUnassignCaseRequestDto.CreateValid(_fixture);

        // Act
        var response = await _httpClient.PostAsync($"/cases/{unassignRequest.CaseId}/unassign", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task UnassignCase_DoesNotBelongToOrganization_ShouldReturnForbidden()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User], SystemConstants.DefaultOrganization);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);
        var unassignRequest = FakeUnassignCaseRequestDto.CreateValid(_fixture);

        // Act
        var response = await _httpClient.PostAsync($"/cases/{unassignRequest.CaseId}/unassign", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task UnassignCase_BelongsToDisabledOrganization_ShouldReturnForbidden()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User], DisabledOrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);
        var unassignRequest = FakeUnassignCaseRequestDto.CreateValid(_fixture);

        // Act
        var response = await _httpClient.PostAsync($"/cases/{unassignRequest.CaseId}/unassign", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task UnassignCase_GivenInvalidRequest_ShouldReturnBadRequest()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);
        var caseEntity = FakeCaseEntity.CreateValid(_fixture) with
        {
            OrganizationId = OrganizationId,
            StatusId = (int)CaseStatus.Pending,
            AssignedToUserId = null,
            AssignedToUser = null,
            AssignedAt = null,
            Organization = null!,
            IsDeleted = false
        };

        await DatabaseHelper.CreateCases(testFixture.Services, [caseEntity]);

        var unassignRequest = FakeUnassignCaseRequestDto.CreateValid(_fixture) with
        {
            CaseId = caseEntity.CaseId,
            OrganizationId = OrganizationId
        };

        // Act
        var response = await _httpClient.PostAsync($"/cases/{unassignRequest.CaseId}/unassign", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task UnassignCase_GivenValidRequest_ShouldReturnNoContent()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);
        var caseEntity = FakeCaseEntity.CreateValid(_fixture) with
        {
            OrganizationId = OrganizationId,
            Organization = null!,
            StatusId = (int)CaseStatus.Assigned,
            IsDeleted = false
        };

        await DatabaseHelper.CreateCases(testFixture.Services, [caseEntity]);

        var unassignRequest = FakeUnassignCaseRequestDto.CreateValid(_fixture) with
        {
            CaseId = caseEntity.CaseId,
            OrganizationId = OrganizationId
        };

        // Act
        var response = await _httpClient.PostAsync($"/cases/{unassignRequest.CaseId}/unassign", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);

        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();
        var updatedCaseEntity = context.Cases.FirstOrDefault(x => x.CaseId == unassignRequest.CaseId);
        updatedCaseEntity.Should().NotBeNull();
        updatedCaseEntity!.AssignedToUserId.Should().BeNull();
    }

    [Fact]
    public async Task ResolveCase_NotAuthenticated_ShouldReturnUnauthorized()
    {
        // Arrange
        var resolveRequest = FakeResolveCaseRequestDto.CreateValid(_fixture);

        // Act
        var response = await _httpClient.PostAsJsonAsync($"/cases/{resolveRequest.CaseId}/resolve", resolveRequest.Body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task ResolveCase_DoesNotBelongToOrganization_ShouldReturnForbidden()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User], SystemConstants.DefaultOrganization);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);
        var resolveRequest = FakeResolveCaseRequestDto.CreateValid(_fixture);

        // Act
        var response = await _httpClient.PostAsJsonAsync($"/cases/{resolveRequest.CaseId}/resolve", resolveRequest.Body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task ResolveCase_BelongsToDisabledOrganization_ShouldReturnForbidden()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User], DisabledOrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);
        var resolveRequest = FakeResolveCaseRequestDto.CreateValid(_fixture);

        // Act
        var response = await _httpClient.PostAsJsonAsync($"/cases/{resolveRequest.CaseId}/resolve", resolveRequest.Body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task ResolveCase_GivenInvalidRequest_ShouldReturnBadRequest()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);
        var caseEntity = FakeCaseEntity.CreateValid(_fixture) with
        {
            OrganizationId = OrganizationId,
            StatusId = (int)CaseStatus.Pending,
            Organization = null!,
            IsDeleted = false
        };

        await DatabaseHelper.CreateCases(testFixture.Services, [caseEntity]);

        var resolveRequest = FakeResolveCaseRequestDto.CreateValid(_fixture) with
        {
            CaseId = caseEntity.CaseId,
            OrganizationId = OrganizationId
        };

        // Act
        var response = await _httpClient.PostAsJsonAsync($"/cases/{resolveRequest.CaseId}/resolve", resolveRequest.Body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task ResolveCase_GivenValidRequest_ShouldReturnNoContent()
    {
        // Arrange
        var user = await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);
        var caseEntity = FakeCaseEntity.CreateValid(_fixture) with
        {
            OrganizationId = OrganizationId,
            Organization = null!,
            AssignedToUserId = user.Id,
            AssignedToUser = null!,
            StatusId = (int)CaseStatus.Assigned,
            IsDeleted = false
        };

        await DatabaseHelper.CreateCases(testFixture.Services, [caseEntity]);

        var resolveRequest = FakeResolveCaseRequestDto.CreateValid(_fixture) with
        {
            CaseId = caseEntity.CaseId,
            OrganizationId = OrganizationId
        };

        // Act
        var response = await _httpClient.PostAsJsonAsync($"/cases/{resolveRequest.CaseId}/resolve", resolveRequest.Body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);

        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();
        var updatedCaseEntity = context.Cases.FirstOrDefault(x => x.CaseId == resolveRequest.CaseId);
        updatedCaseEntity.Should().NotBeNull();
        updatedCaseEntity!.StatusId.Should().Be((int)Enum.Parse<CaseStatus>(resolveRequest.Body.Status));
    }

    [Theory]
    [InlineData(nameof(CaseStatus.Approved), DecisionStatus.Released, DecisionReasonCode.AnalystApproved)]
    [InlineData(nameof(CaseStatus.Rejected), DecisionStatus.Blocked, DecisionReasonCode.AnalystRejected)]
    public async Task ResolveCase_GivenHeldDecision_ShouldResolveTheDecisionAndQueueItsWebhook(
        string caseStatus,
        DecisionStatus expectedStatus,
        DecisionReasonCode expectedReasonCode)
    {
        // Arrange
        var user = await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);

        var transactionEntity = FakeTransactionEntity.CreateValid(_fixture) with
        {
            OrganizationId = OrganizationId,
            Organization = null!,
            ExternalReference = "pay_resolve_me",
            IsDeleted = false
        };

        await DatabaseHelper.CreateTransactions(testFixture.Services, [transactionEntity]);

        var caseEntity = FakeCaseEntity.CreateValid(_fixture) with
        {
            OrganizationId = OrganizationId,
            Organization = null!,
            TransactionId = transactionEntity.TransactionId,
            AssignedToUserId = user.Id,
            AssignedToUser = null!,
            StatusId = (int)CaseStatus.Assigned,
            IsDeleted = false
        };

        await DatabaseHelper.CreateCases(testFixture.Services, [caseEntity]);

        var decisionEntity = FakeDecisionEntity.CreateValid(_fixture) with
        {
            OrganizationId = OrganizationId,
            Organization = null!,
            TransactionId = transactionEntity.TransactionId,
            Transaction = null!,
            CaseId = caseEntity.CaseId
        };

        await DatabaseHelper.CreateDecisions(testFixture.Services, [decisionEntity]);

        await DatabaseHelper.CreateWebhookConfigurations(testFixture.Services, [
            FakeWebhookConfigurationEntity.CreateValid(_fixture) with
            {
                OrganizationId = OrganizationId,
                Organization = null!,
                IsEnabled = true,
                IsDeleted = false
            }
        ]);

        var resolveRequest = FakeResolveCaseRequestDto.CreateValid(_fixture) with
        {
            CaseId = caseEntity.CaseId,
            OrganizationId = OrganizationId,
            Body = new ResolveCaseRequestBodyDto
            {
                Status = caseStatus,
                Disposition = nameof(CaseDisposition.TruePositive),
                Note = "Confirmed mule account"
            }
        };

        // Act
        var response = await _httpClient.PostAsJsonAsync($"/cases/{resolveRequest.CaseId}/resolve", resolveRequest.Body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();

        var updatedDecisionEntity = context.Decisions.FirstOrDefault(x => x.DecisionId == decisionEntity.DecisionId);
        updatedDecisionEntity.Should().NotBeNull();
        updatedDecisionEntity!.StatusId.Should().Be((int)expectedStatus);
        updatedDecisionEntity.ReasonCodeId.Should().Be((int)expectedReasonCode);
        updatedDecisionEntity.ResolvedByUserId.Should().Be(user.Id);
        updatedDecisionEntity.ResolvedAt.Should().NotBeNull();

        var decisionWebhook = context.OutboxMessages
            .AsEnumerable()
            .Select(x => x.Payload.FromBase64String())
            .Where(x => x.Contains(WebhookEvents.DecisionResolved))
            .Select(x => x.TryDeserialize<WebhookMessage<DecisionWebhookMessage>>())
            .SingleOrDefault();

        decisionWebhook.Should().NotBeNull();
        decisionWebhook!.Payload.Verdict.Should().Be(expectedStatus.ToString());
        decisionWebhook.Payload.ReasonCode.Should().Be(expectedReasonCode.ToString());
        decisionWebhook.Payload.ResolvedByUser.Should().NotBeNull();
        decisionWebhook.Payload.ResolvedByUser!.UserId.Should().Be(user.Id);
    }

    [Fact]
    public async Task DeleteCase_NotAuthenticated_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _httpClient.DeleteAsync($"/cases/{_fixture.Create<Guid>()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task DeleteCase_NotManager_ShouldReturnForbidden()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User], SystemConstants.DefaultOrganization);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);

        // Act
        var response = await _httpClient.DeleteAsync($"/cases/{_fixture.Create<Guid>()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task DeleteCase_DoesNotBelongToOrganization_ShouldReturnForbidden()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User], SystemConstants.DefaultOrganization);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);

        // Act
        var response = await _httpClient.DeleteAsync($"/cases/{_fixture.Create<Guid>()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task DeleteCase_BelongsToDisabledOrganization_ShouldReturnForbidden()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User], DisabledOrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);

        // Act
        var response = await _httpClient.DeleteAsync($"/cases/{_fixture.Create<Guid>()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task DeleteCase_GivenValidRequest_ShouldReturnNoContent()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User, UserRole.Manager], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);
        var caseEntity = FakeCaseEntity.CreateValid(_fixture) with
        {
            OrganizationId = OrganizationId,
            Organization = null!,
            IsDeleted = false
        };

        await DatabaseHelper.CreateCases(testFixture.Services, [caseEntity]);

        // Act
        var response = await _httpClient.DeleteAsync($"/cases/{caseEntity.CaseId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);

        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();
        var updatedCaseEntity = context.Cases.FirstOrDefault(x => x.CaseId == caseEntity.CaseId);
        updatedCaseEntity.Should().NotBeNull();
        updatedCaseEntity!.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task RecoverCase_NotAuthenticated_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _httpClient.PostAsync($"/cases/{_fixture.Create<Guid>()}/recover", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task RecoverCase_NotManager_ShouldReturnForbidden()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User], SystemConstants.DefaultOrganization);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);
        var caseEntity = FakeCaseEntity.CreateValid(_fixture) with
        {
            OrganizationId = OrganizationId,
            Organization = null!,
            IsDeleted = true
        };

        await DatabaseHelper.CreateCases(testFixture.Services, [caseEntity]);

        // Act
        var response = await _httpClient.PostAsync($"/cases/{_fixture.Create<Guid>()}/recover", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task RecoverCase_DoesNotBelongToOrganization_ShouldReturnForbidden()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User], SystemConstants.DefaultOrganization);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);

        // Act
        var response = await _httpClient.PostAsync($"/cases/{_fixture.Create<Guid>()}/recover", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task RecoverCase_BelongsToDisabledOrganization_ShouldReturnForbidden()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User], DisabledOrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);

        // Act
        var response = await _httpClient.PostAsync($"/cases/{_fixture.Create<Guid>()}/recover", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task RecoverCase_GivenValidRequest_ShouldReturnNoContent()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User, UserRole.Manager], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);
        var caseEntity = FakeCaseEntity.CreateValid(_fixture) with
        {
            OrganizationId = OrganizationId,
            Organization = null!,
            IsDeleted = true
        };

        await DatabaseHelper.CreateCases(testFixture.Services, [caseEntity]);

        // Act
        var response = await _httpClient.PostAsync($"/cases/{caseEntity.CaseId}/recover", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);

        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();
        var updatedCaseEntity = context.Cases.FirstOrDefault(x => x.CaseId == caseEntity.CaseId);
        updatedCaseEntity.Should().NotBeNull();
        updatedCaseEntity!.IsDeleted.Should().BeFalse();
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
