using System.Net;
using System.Net.Http.Json;
using System.Net.Mime;
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
using Presentation.Models.Common;
using Presentation.Models.Invoices;
using Presentation.Models.OrganizationInvoices;
using UnitTests.TestHelpers.FakeObjects.Infrastructure.Entities;
using UnitTests.TestHelpers.FakeObjects.Presentation.Models.OrganizationInvoices;

namespace BehaviouralTests.Tests.Controllers;

public class OrganizationInvoiceControllerTests(TestFixture testFixture) : IClassFixture<TestFixture>, IAsyncLifetime
{
    private readonly Fixture _fixture = AutoFixtureHelper.Create();
    private readonly HttpClient _httpClient = testFixture.CreateClient();

    private const string Email = "username@mail.com";
    private const string Password = "P@$$w0rd";
    private static readonly Guid OrganizationId = new("4d6389e9-f5d1-4325-a004-915865e3adec");
    private static readonly Guid DisabledOrganizationId = new("03a9b48e-4af2-40bd-a180-cac58bd930f3");

    [Fact]
    public async Task GenerateInvoice_NotAuthenticated_ShouldReturnUnauthorized()
    {
        // Arrange
        var generateRequest = FakeGenerateOrganizationInvoiceRequestDto.CreateValid(_fixture);

        // Act
        var response = await _httpClient.PostAsJsonAsync($"/organizations/{OrganizationId}/invoices", generateRequest.Body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task GenerateInvoice_NotAdmin_ShouldReturnForbidden()
    {
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);
        var generateRequest = FakeGenerateOrganizationInvoiceRequestDto.CreateValid(_fixture);

        // Act
        var response = await _httpClient.PostAsJsonAsync($"/organizations/{OrganizationId}/invoices", generateRequest.Body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task GenerateInvoice_GivenInvalidRequest_ShouldReturnBadRequest()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User, UserRole.Admin], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);
        var generateRequest = FakeGenerateOrganizationInvoiceRequestDto.CreateValid(_fixture) with
        {
            Body = new GenerateOrganizationInvoiceRequestBodyDto
            {
                Month = 13,
                Year = 2020
            }
        };

        // Act
        var response = await _httpClient.PostAsJsonAsync($"/organizations/{OrganizationId}/invoices", generateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task GenerateInvoice_GivenValidRequest_ShouldReturnCreated()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User, UserRole.Admin], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);
        var generateRequest = FakeGenerateOrganizationInvoiceRequestDto.CreateValid(_fixture) with
        {
            Body = new GenerateOrganizationInvoiceRequestBodyDto
            {
                Month = 1,
                Year = 2020
            }
        };

        await DatabaseHelper.CreateUsages(testFixture.Services, Enumerable.Range(0, 50).Select(
            _ => FakeUsageEntity.CreateValid(_fixture) with
            {
                OrganizationId = OrganizationId,
                Organization = null!,
                CreatedAt = new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero)
            })
        );

        // Act
        var response = await _httpClient.PostAsJsonAsync($"/organizations/{OrganizationId}/invoices", generateRequest.Body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);

        var createdInvoice = await response.Content.ReadDataFromJsonAsync<InvoiceResourceDto>();
        createdInvoice.Should().NotBeNull();
        createdInvoice!.Invoice.Month.Should().Be(generateRequest.Body.Month);
        createdInvoice.Invoice.Year.Should().Be(generateRequest.Body.Year);

        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();
        var invoiceEntity = context.Invoices.FirstOrDefault(x => x.InvoiceId == createdInvoice.Invoice.InvoiceId);
        invoiceEntity.Should().NotBeNull();
    }

    [Fact]
    public async Task ListInvoice_NotAuthenticated_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _httpClient.GetAsync($"/organizations/{OrganizationId}/invoices");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task ListInvoice_NotAdmin_ShouldReturnForbidden()
    {
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);
        var generateRequest = FakeGenerateOrganizationInvoiceRequestDto.CreateValid(_fixture);

        // Act
        var response = await _httpClient.PostAsJsonAsync($"/organizations/{OrganizationId}/invoices", generateRequest.Body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task ListInvoice_GivenInvalidRequest_ShouldReturnBadRequest()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User, UserRole.Admin], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);
        await DatabaseHelper.CreateInvoices(testFixture.Services, Enumerable.Range(0, 50).Select(
            _ => FakeInvoiceEntity.CreateValid(_fixture) with
            {
                OrganizationId = OrganizationId,
                Organization = null!,
                IsDeleted = false
            })
        );

        // Act
        var response = await _httpClient.GetAsync($"/organizations/{OrganizationId}/invoices?PageSize=200");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task ListInvoice_GivenValidRequest_ShouldReturnOk()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User, UserRole.Admin], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);
        await DatabaseHelper.CreateInvoices(testFixture.Services, Enumerable.Range(0, 50).Select(
            _ => FakeInvoiceEntity.CreateValid(_fixture) with
            {
                OrganizationId = OrganizationId,
                Organization = null!,
                IsDeleted = false
            })
        );

        // Act
        var response = await _httpClient.GetAsync($"/organizations/{OrganizationId}/invoices");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);

        var invoices = await response.Content.ReadFromJsonAsync<PagedResponseDto<InvoiceResourceDto>>();
        invoices.Should().NotBeNull();
        invoices!.Metadata.Should().NotBeNull();
        invoices.Metadata.PageCount.Should().Be(5);
        invoices.Metadata.PageNumber.Should().Be(1);
        invoices.Metadata.PageSize.Should().Be(10);
        invoices.Metadata.TotalCount.Should().Be(50);
        invoices.Metadata.HasMore.Should().BeTrue();
    }

    [Fact]
    public async Task GetInvoice_NotAuthenticated_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _httpClient.GetAsync($"/organizations/{OrganizationId}/invoices/{_fixture.Create<Guid>()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task GetInvoice_NotAdmin_ShouldReturnForbidden()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);

        var invoiceEntity = FakeInvoiceEntity.CreateValid(_fixture) with
        {
            OrganizationId = OrganizationId,
            Organization = null!,
            IsDeleted = false
        };

        await DatabaseHelper.CreateInvoices(testFixture.Services, [invoiceEntity]);

        // Act
        var response = await _httpClient.GetAsync($"/organizations/{OrganizationId}/invoices/{invoiceEntity.InvoiceId}");
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task GetInvoice_GivenValidRequest_ShouldReturnOk()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User, UserRole.Admin], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);

        var invoiceEntity = FakeInvoiceEntity.CreateValid(_fixture) with
        {
            OrganizationId = OrganizationId,
            Organization = null!,
            IsDeleted = false
        };

        await DatabaseHelper.CreateInvoices(testFixture.Services, [invoiceEntity]);

        // Act
        var response = await _httpClient.GetAsync($"/organizations/{OrganizationId}/invoices/{invoiceEntity.InvoiceId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);

        var invoice = await response.Content.ReadDataFromJsonAsync<InvoiceResourceDto>();
        invoice.Should().NotBeNull();
        invoice!.Invoice.InvoiceId.Should().Be(invoiceEntity.InvoiceId);
        invoice.Invoice.Month.Should().Be(invoiceEntity.Month);
        invoice.Invoice.Year.Should().Be(invoiceEntity.Year);
    }

    [Fact]
    public async Task ExportInvoice_NotAuthenticated_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _httpClient.PostAsync($"/organizations/{OrganizationId}/invoices/{_fixture.Create<Guid>()}/export", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task ExportInvoice_NotAdmin_ShouldReturnForbidden()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);

        var invoiceEntity = FakeInvoiceEntity.CreateValid(_fixture) with
        {
            OrganizationId = OrganizationId,
            Organization = null!,
            IsDeleted = false
        };

        await DatabaseHelper.CreateInvoices(testFixture.Services, [invoiceEntity]);

        // Act
        var response = await _httpClient.PostAsync($"/organizations/{OrganizationId}/invoices/{invoiceEntity.InvoiceId}/export", null);
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task ExportInvoice_DoesNotBelongToOrganization_ShouldReturnForbidden()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User, UserRole.Manager], SystemConstants.DefaultOrganization);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);

        // Act
        var response = await _httpClient.PostAsync($"/organizations/{OrganizationId}/invoices/{_fixture.Create<Guid>()}/export", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task ExportInvoice_BelongsToDisabledOrganization_ShouldReturnForbidden()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User, UserRole.Admin], DisabledOrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);

        // Act
        var response = await _httpClient.PostAsync($"/organizations/{OrganizationId}/invoices/{_fixture.Create<Guid>()}/export", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task ExportInvoice_PlatformAdministratorWithoutOrganization_ShouldReturnOk()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User, UserRole.Admin], SystemConstants.DefaultOrganization);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);

        var invoiceEntity = FakeInvoiceEntity.CreateValid(_fixture) with
        {
            OrganizationId = OrganizationId,
            Organization = null!,
            Month = 1,
            Year = 2020,
            IsDeleted = false
        };

        await DatabaseHelper.CreateInvoices(testFixture.Services, [invoiceEntity]);

        await DatabaseHelper.CreateUsages(testFixture.Services, Enumerable.Range(0, 50).Select(
            _ => FakeUsageEntity.CreateValid(_fixture) with
            {
                CreatedAt = new DateTimeOffset(invoiceEntity.Year, invoiceEntity.Month, 1, 0, 0, 0, TimeSpan.Zero),
                OrganizationId = OrganizationId,
                Organization = null!
            }));

        // Act
        var response = await _httpClient.PostAsync($"/organizations/{OrganizationId}/invoices/{invoiceEntity.InvoiceId}/export", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be(MediaTypeNames.Application.Pdf);
    }

    [Fact]
    public async Task ExportInvoice_GivenValidRequest_ShouldReturnOk()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User, UserRole.Admin], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);

        var invoiceEntity = FakeInvoiceEntity.CreateValid(_fixture) with
        {
            OrganizationId = OrganizationId,
            Organization = null!,
            Month = 1,
            Year = 2020,
            IsDeleted = false
        };

        await DatabaseHelper.CreateInvoices(testFixture.Services, [invoiceEntity]);

        await DatabaseHelper.CreateUsages(testFixture.Services, Enumerable.Range(0, 50).Select(
            _ => FakeUsageEntity.CreateValid(_fixture) with
            {
                CreatedAt = new DateTimeOffset(invoiceEntity.Year, invoiceEntity.Month, 1, 0, 0, 0, TimeSpan.Zero),
                OrganizationId = OrganizationId,
                Organization = null!
            }));

        // Act
        var response = await _httpClient.PostAsync($"/organizations/{OrganizationId}/invoices/{invoiceEntity.InvoiceId}/export", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
        response.Content.Headers.ContentType!.MediaType.Should().Be(MediaTypeNames.Application.Pdf);
    }

    [Fact]
    public async Task DeleteInvoice_NotAuthenticated_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _httpClient.DeleteAsync($"/organizations/{OrganizationId}/invoices/{_fixture.Create<Guid>()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task DeleteInvoice_NotAdmin_ShouldReturnForbidden()
    {
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);
        var invoiceEntity = FakeInvoiceEntity.CreateValid(_fixture) with
        {
            OrganizationId = OrganizationId,
            Organization = null!,
            IsDeleted = false
        };

        await DatabaseHelper.CreateInvoices(testFixture.Services, [invoiceEntity]);

        // Act
        var response = await _httpClient.DeleteAsync($"/organizations/{OrganizationId}/invoices/{invoiceEntity.InvoiceId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task DeleteInvoice_GivenValidRequest_ShouldReturnNoContent()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User, UserRole.Admin], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);
        var invoiceEntity = FakeInvoiceEntity.CreateValid(_fixture) with
        {
            OrganizationId = OrganizationId,
            Organization = null!,
            IsDeleted = false
        };

        await DatabaseHelper.CreateInvoices(testFixture.Services, [invoiceEntity]);

        // Act
        var response = await _httpClient.DeleteAsync($"/organizations/{OrganizationId}/invoices/{invoiceEntity.InvoiceId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);

        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();
        var updatedInvoiceEntity = context.Invoices.FirstOrDefault(x => x.InvoiceId == invoiceEntity.InvoiceId);
        updatedInvoiceEntity.Should().NotBeNull();
        updatedInvoiceEntity!.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task RecoverInvoice_NotAuthenticated_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _httpClient.PostAsync($"/organizations/{OrganizationId}/invoices/{_fixture.Create<Guid>()}/recover", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task RecoverInvoice_NotAdmin_ShouldReturnForbidden()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);
        var invoiceEntity = FakeInvoiceEntity.CreateValid(_fixture) with
        {
            OrganizationId = OrganizationId,
            Organization = null!,
            IsDeleted = true
        };

        await DatabaseHelper.CreateInvoices(testFixture.Services, [invoiceEntity]);

        // Act
        var response = await _httpClient.PostAsync($"/organizations/{OrganizationId}/invoices/{invoiceEntity.InvoiceId}/recover", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task RecoverInvoice_GivenValidRequest_ShouldReturnNoContent()
    {
        // Arrange
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User, UserRole.Admin], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);
        var invoiceEntity = FakeInvoiceEntity.CreateValid(_fixture) with
        {
            OrganizationId = OrganizationId,
            Organization = null!,
            IsDeleted = true
        };

        await DatabaseHelper.CreateInvoices(testFixture.Services, [invoiceEntity]);

        // Act
        var response = await _httpClient.PostAsync($"/organizations/{OrganizationId}/invoices/{invoiceEntity.InvoiceId}/recover", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);

        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();
        var updatedInvoiceEntity = context.Invoices.FirstOrDefault(x => x.InvoiceId == invoiceEntity.InvoiceId);
        updatedInvoiceEntity.Should().NotBeNull();
        updatedInvoiceEntity!.IsDeleted.Should().BeFalse();
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

        await DatabaseHelper.CreatePlanWithSubscription(
            testFixture.Services, OrganizationId, 0.5m, new DateTimeOffset(2019, 1, 1, 0, 0, 0, TimeSpan.Zero));

        await DatabaseHelper.CreateBillingProfile(testFixture.Services, OrganizationId);
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }
}
