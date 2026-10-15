using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using AutoFixture;
using BehaviouralTests.TestHelpers;
using Core.Enums;
using Core.Models.Rules;
using Core.ValueObjects;
using FluentAssertions;
using Infrastructure;
using Infrastructure.Configurations;
using Infrastructure.Entities;
using Infrastructure.Mappers;
using Microsoft.Extensions.DependencyInjection;
using Presentation.Constants;
using Presentation.Models.AuditLogs;
using Presentation.Models.Common;
using Presentation.Models.TransactionSchemas;
using UnitTests.TestHelpers.FakeObjects.Core.Models.ApiClients;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Users;
using UnitTests.TestHelpers.FakeObjects.Infrastructure.Entities;

namespace BehaviouralTests.Tests.Controllers;

public class OrganizationTransactionSchemaControllerTests(TestFixture testFixture) : IClassFixture<TestFixture>, IAsyncLifetime
{
    private readonly Fixture _fixture = AutoFixtureHelper.Create();
    private readonly HttpClient _httpClient = testFixture.CreateClient();

    private const string Email = "username@mail.com";
    private const string Password = "P@$$w0rd";
    private static readonly Guid OrganizationId = new("4d6389e9-f5d1-4325-a004-915865e3adec");

    [Fact]
    public async Task SetTransactionSchema_NotAuthenticated_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _httpClient.PutAsJsonAsync($"/organizations/{OrganizationId}/transaction-schema", ValidBody());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);
    }

    [Fact]
    public async Task SetTransactionSchema_NotAdmin_ShouldReturnForbidden()
    {
        // Arrange
        await AuthenticateAsync(UserRole.User);

        // Act
        var response = await _httpClient.PutAsJsonAsync($"/organizations/{OrganizationId}/transaction-schema", ValidBody());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task SetTransactionSchema_GivenValidRequest_ShouldReturnOkAndPersistVersionOne()
    {
        // Arrange
        await AuthenticateAsync(UserRole.Admin);

        // Act
        var response = await _httpClient.PutAsJsonAsync($"/organizations/{OrganizationId}/transaction-schema", ValidBody());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var resource = await response.Content.ReadDataFromJsonAsync<TransactionSchemaResourceDto>();
        resource.Should().NotBeNull();
        resource!.TransactionSchema.CurrentVersionNumber.Should().Be(1);
        resource.TransactionSchema.Fields.Should().HaveCount(2);

        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();
        context.TransactionSchemas.Should().ContainSingle(x => x.OrganizationId == OrganizationId);
        context.TransactionSchemaVersions.Should().ContainSingle(x => x.OrganizationId == OrganizationId);
    }

    [Fact]
    public async Task SetTransactionSchema_GivenAFieldWithoutAName_ShouldReturnBadRequest()
    {
        // Arrange
        await AuthenticateAsync(UserRole.Admin);
        var body = new SetTransactionSchemaRequestBodyDto
        {
            Fields = [new SchemaFieldDto { Name = "", Type = nameof(SchemaFieldType.Number), IsRequired = true }]
        };

        // Act
        var response = await _httpClient.PutAsJsonAsync($"/organizations/{OrganizationId}/transaction-schema", body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SetTransactionSchema_GivenAnUnknownFieldType_ShouldReturnBadRequest()
    {
        // Arrange
        await AuthenticateAsync(UserRole.Admin);
        var body = new SetTransactionSchemaRequestBodyDto
        {
            Fields = [new SchemaFieldDto { Name = "amount", Type = "Decimal", IsRequired = true }]
        };

        // Act
        var response = await _httpClient.PutAsJsonAsync($"/organizations/{OrganizationId}/transaction-schema", body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SetTransactionSchema_GivenDuplicateFieldNames_ShouldReturnBadRequest()
    {
        // Arrange
        await AuthenticateAsync(UserRole.Admin);
        var body = new SetTransactionSchemaRequestBodyDto
        {
            Fields =
            [
                new SchemaFieldDto { Name = "amount", Type = nameof(SchemaFieldType.Number), IsRequired = true },
                new SchemaFieldDto { Name = "amount", Type = nameof(SchemaFieldType.String), IsRequired = false }
            ]
        };

        // Act
        var response = await _httpClient.PutAsJsonAsync($"/organizations/{OrganizationId}/transaction-schema", body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SetTransactionSchema_GivenAllowedValuesOnANumberField_ShouldReturnBadRequest()
    {
        // Arrange
        await AuthenticateAsync(UserRole.Admin);
        var body = new SetTransactionSchemaRequestBodyDto
        {
            Fields =
            [
                new SchemaFieldDto
                {
                    Name = "amount",
                    Type = nameof(SchemaFieldType.Number),
                    IsRequired = true,
                    AllowedValues = ["1", "2"]
                }
            ]
        };

        // Act
        var response = await _httpClient.PutAsJsonAsync($"/organizations/{OrganizationId}/transaction-schema", body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SetTransactionSchema_CalledTwice_ShouldAddAVersionRatherThanASecondSchema()
    {
        // Arrange
        await AuthenticateAsync(UserRole.Admin);
        var first = await _httpClient.PutAsJsonAsync($"/organizations/{OrganizationId}/transaction-schema", ValidBody());
        first.StatusCode.Should().Be(HttpStatusCode.OK, await first.Content.ReadAsStringAsync());

        var revised = new SetTransactionSchemaRequestBodyDto
        {
            Fields = [new SchemaFieldDto { Name = "amount", Type = nameof(SchemaFieldType.Number), IsRequired = true }]
        };

        // Act
        var response = await _httpClient.PutAsJsonAsync($"/organizations/{OrganizationId}/transaction-schema", revised);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var resource = await response.Content.ReadDataFromJsonAsync<TransactionSchemaResourceDto>();
        resource!.TransactionSchema.CurrentVersionNumber.Should().Be(2);
        resource.TransactionSchema.Fields.Should().ContainSingle();

        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();
        context.TransactionSchemas.Should().ContainSingle(x => x.OrganizationId == OrganizationId);
        context.TransactionSchemaVersions.Count(x => x.OrganizationId == OrganizationId).Should().Be(2);
    }

    [Fact]
    public async Task GetTransactionSchema_WhenNoneIsDefined_ShouldReturnNotFound()
    {
        // Arrange
        await AuthenticateAsync(UserRole.Admin);

        // Act
        var response = await _httpClient.GetAsync($"/organizations/{OrganizationId}/transaction-schema");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetTransactionSchema_GivenValidRequest_ShouldReturnOk()
    {
        // Arrange
        await AuthenticateAsync(UserRole.Admin);
        await _httpClient.PutAsJsonAsync($"/organizations/{OrganizationId}/transaction-schema", ValidBody());

        // Act
        var response = await _httpClient.GetAsync($"/organizations/{OrganizationId}/transaction-schema");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var resource = await response.Content.ReadDataFromJsonAsync<TransactionSchemaResourceDto>();
        resource!.TransactionSchema.Fields.Should().HaveCount(2);
        resource.Organization.Should().NotBeNull();
    }

    [Fact]
    public async Task ListTransactionSchemaVersions_GivenValidRequest_ShouldReturnOk()
    {
        // Arrange
        await AuthenticateAsync(UserRole.Admin);
        await _httpClient.PutAsJsonAsync($"/organizations/{OrganizationId}/transaction-schema", ValidBody());

        // Act
        var response = await _httpClient.GetAsync($"/organizations/{OrganizationId}/transaction-schema/versions");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var page = await response.Content.ReadFromJsonAsync<PagedResponseDto<TransactionSchemaVersionResourceDto>>();
        page!.Data.Should().ContainSingle();
        page.Data[0].TransactionSchemaVersion.VersionNumber.Should().Be(1);
    }

    [Fact]
    public async Task GetTransactionSchemaVersion_GivenAnUnknownVersion_ShouldReturnNotFound()
    {
        // Arrange
        await AuthenticateAsync(UserRole.Admin);
        await _httpClient.PutAsJsonAsync($"/organizations/{OrganizationId}/transaction-schema", ValidBody());

        // Act
        var response = await _httpClient.GetAsync($"/organizations/{OrganizationId}/transaction-schema/versions/9");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetTransactionSchemaVersion_GivenValidRequest_ShouldReturnTheFieldsOfItsDay()
    {
        // Arrange
        await AuthenticateAsync(UserRole.Admin);
        await _httpClient.PutAsJsonAsync($"/organizations/{OrganizationId}/transaction-schema", ValidBody());

        var revised = new SetTransactionSchemaRequestBodyDto
        {
            Fields = [new SchemaFieldDto { Name = "amount", Type = nameof(SchemaFieldType.Number), IsRequired = true }]
        };
        await _httpClient.PutAsJsonAsync($"/organizations/{OrganizationId}/transaction-schema", revised);

        // Act
        var response = await _httpClient.GetAsync($"/organizations/{OrganizationId}/transaction-schema/versions/1");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var resource = await response.Content.ReadDataFromJsonAsync<TransactionSchemaVersionResourceDto>();
        resource!.TransactionSchemaVersion.Fields.Should().HaveCount(2);
    }

    [Fact]
    public async Task SetTransactionSchema_ShouldBeRecordedInTheAuditHistory()
    {
        // Arrange
        await AuthenticateAsync(UserRole.Admin);
        await _httpClient.PutAsJsonAsync($"/organizations/{OrganizationId}/transaction-schema", ValidBody());

        var revised = new SetTransactionSchemaRequestBodyDto
        {
            Fields = [new SchemaFieldDto { Name = "amount", Type = nameof(SchemaFieldType.Number), IsRequired = true }]
        };
        await _httpClient.PutAsJsonAsync($"/organizations/{OrganizationId}/transaction-schema", revised);

        // Act
        var response = await _httpClient.GetAsync($"/organizations/{OrganizationId}/transaction-schema/history");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();
        context.AuditLogs
            .Count(x => x.EntityTypeId == (int)AuditableEntityType.TransactionSchema)
            .Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetMyTransactionSchema_GivenValidRequest_ShouldReturnOk()
    {
        // Arrange
        await AuthenticateAsync(UserRole.Admin);
        await _httpClient.PutAsJsonAsync("/organizations/my/transaction-schema", ValidBody());

        // Act
        var response = await _httpClient.GetAsync("/organizations/my/transaction-schema");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var resource = await response.Content.ReadDataFromJsonAsync<TransactionSchemaResourceDto>();
        resource!.TransactionSchema.OrganizationId.Should().Be(OrganizationId);
    }

    [Fact]
    public async Task CreateRule_GivenAFieldNotOnTheSchema_ShouldReturnBadRequest()
    {
        // Arrange
        await AuthenticateAsync(UserRole.Admin, UserRole.Manager);
        await _httpClient.PutAsJsonAsync($"/organizations/{OrganizationId}/transaction-schema", ValidBody());

        var ruleSetEntity = await SeedRuleSetAsync();

        var body = CreateRuleBody("amont", nameof(RuleCondition.GreaterThan), 10);

        // Act
        var response = await _httpClient.PostAsJsonAsync($"/rule-sets/{ruleSetEntity.RuleSetId}/rules", body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var problem = await response.Content.ReadFromJsonAsync<JsonObject>();
        problem!["violations"]!.AsArray().Should().ContainSingle();
        problem["violations"]![0]!["field"]!.GetValue<string>().Should().Be("amont");
    }

    [Fact]
    public async Task CreateRule_GivenAFieldOnTheSchema_ShouldReturnCreated()
    {
        // Arrange
        await AuthenticateAsync(UserRole.Admin, UserRole.Manager);
        await _httpClient.PutAsJsonAsync($"/organizations/{OrganizationId}/transaction-schema", ValidBody());

        var ruleSetEntity = await SeedRuleSetAsync();

        var body = CreateRuleBody("amount", nameof(RuleCondition.GreaterThan), 10);

        // Act
        var response = await _httpClient.PostAsJsonAsync($"/rule-sets/{ruleSetEntity.RuleSetId}/rules", body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task CreateRule_WhenNoSchemaIsDefined_ShouldReturnCreated()
    {
        // Arrange
        await AuthenticateAsync(UserRole.Admin, UserRole.Manager);

        var ruleSetEntity = await SeedRuleSetAsync();

        var body = CreateRuleBody("anythingAtAll", nameof(RuleCondition.GreaterThan), 10);

        // Act
        var response = await _httpClient.PostAsJsonAsync($"/rule-sets/{ruleSetEntity.RuleSetId}/rules", body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task EvaluateRule_GivenAPayloadMissingARequiredField_ShouldReturnBadRequest()
    {
        // Arrange
        await AuthenticateAsync(UserRole.Admin);
        await _httpClient.PutAsJsonAsync($"/organizations/{OrganizationId}/transaction-schema", ValidBody());

        var ruleEntity = await SeedFiringRuleAsync();
        await AuthenticateAsApiClientAsync();

        // Act
        var response = await _httpClient.PostAsJsonAsync(
            $"/rules/{ruleEntity.RuleId}/evaluate",
            new JsonObject { ["currency"] = "GBP" });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var problem = await response.Content.ReadFromJsonAsync<JsonObject>();
        problem!["violations"]![0]!["field"]!.GetValue<string>().Should().Be("amount");
    }

    [Fact]
    public async Task EvaluateRule_GivenAPayloadOfTheWrongType_ShouldReturnBadRequest()
    {
        // Arrange
        await AuthenticateAsync(UserRole.Admin);
        await _httpClient.PutAsJsonAsync($"/organizations/{OrganizationId}/transaction-schema", ValidBody());

        var ruleEntity = await SeedFiringRuleAsync();
        await AuthenticateAsApiClientAsync();

        // Act
        var response = await _httpClient.PostAsJsonAsync(
            $"/rules/{ruleEntity.RuleId}/evaluate",
            new JsonObject { ["amount"] = "many", ["currency"] = "GBP" });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task EvaluateRule_GivenACurrencyOutsideTheEnumeration_ShouldReturnBadRequest()
    {
        // Arrange
        await AuthenticateAsync(UserRole.Admin);
        await _httpClient.PutAsJsonAsync($"/organizations/{OrganizationId}/transaction-schema", ValidBody());

        var ruleEntity = await SeedFiringRuleAsync();
        await AuthenticateAsApiClientAsync();

        // Act
        var response = await _httpClient.PostAsJsonAsync(
            $"/rules/{ruleEntity.RuleId}/evaluate",
            new JsonObject { ["amount"] = 500, ["currency"] = "USD" });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task EvaluateRule_GivenAPayloadMatchingTheSchema_ShouldReturnOk()
    {
        // Arrange
        await AuthenticateAsync(UserRole.Admin);
        await _httpClient.PutAsJsonAsync($"/organizations/{OrganizationId}/transaction-schema", ValidBody());

        var ruleEntity = await SeedFiringRuleAsync();
        await AuthenticateAsApiClientAsync();

        // Act
        var response = await _httpClient.PostAsJsonAsync(
            $"/rules/{ruleEntity.RuleId}/evaluate",
            new JsonObject { ["amount"] = 500, ["currency"] = "GBP" });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task EvaluateRule_GivenAnInvalidPayload_ShouldNotRecordAnything()
    {
        // Arrange
        await AuthenticateAsync(UserRole.Admin);
        await _httpClient.PutAsJsonAsync($"/organizations/{OrganizationId}/transaction-schema", ValidBody());

        var ruleEntity = await SeedFiringRuleAsync();
        await AuthenticateAsApiClientAsync();

        // Act
        await _httpClient.PostAsJsonAsync(
            $"/rules/{ruleEntity.RuleId}/evaluate",
            new JsonObject { ["currency"] = "GBP" });

        // Assert
        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();
        context.Transactions.Should().BeEmpty();
        context.RuleEvaluations.Should().BeEmpty();
        context.Usages.Should().BeEmpty();
    }

    [Fact]
    public async Task ValidateRule_GivenAFieldNotOnTheSchema_ShouldReturnBadRequest()
    {
        // Arrange
        await AuthenticateAsync(UserRole.Admin);
        await _httpClient.PutAsJsonAsync($"/organizations/{OrganizationId}/transaction-schema", ValidBody());

        // Act
        var response = await _httpClient.PostAsJsonAsync("/rules/validate", RuleBodyJson("amont", nameof(RuleCondition.GreaterThan), 10));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var problem = await response.Content.ReadFromJsonAsync<JsonObject>();
        problem!["violations"]![0]!["field"]!.GetValue<string>().Should().Be("amont");
    }

    [Fact]
    public async Task ValidateRule_GivenAFieldOnTheSchema_ShouldReturnOk()
    {
        // Arrange
        await AuthenticateAsync(UserRole.Admin);
        await _httpClient.PutAsJsonAsync($"/organizations/{OrganizationId}/transaction-schema", ValidBody());

        // Act
        var response = await _httpClient.PostAsJsonAsync("/rules/validate", RuleBodyJson("amount", nameof(RuleCondition.GreaterThan), 10));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task TestRule_GivenAFieldNotOnTheSchema_ShouldReturnBadRequest()
    {
        // Arrange
        await AuthenticateAsync(UserRole.Admin);
        await _httpClient.PutAsJsonAsync($"/organizations/{OrganizationId}/transaction-schema", ValidBody());

        var body = new JsonObject
        {
            ["ruleBody"] = RuleBodyJson("amont", nameof(RuleCondition.GreaterThan), 10),
            ["payload"] = new JsonObject { ["amount"] = 500 }
        };

        // Act
        var response = await _httpClient.PostAsJsonAsync("/rules/test", body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task TestRule_GivenAPayloadNotMatchingTheSchema_ShouldReturnBadRequest()
    {
        // Arrange
        await AuthenticateAsync(UserRole.Admin);
        await _httpClient.PutAsJsonAsync($"/organizations/{OrganizationId}/transaction-schema", ValidBody());

        var body = new JsonObject
        {
            ["ruleBody"] = RuleBodyJson("amount", nameof(RuleCondition.GreaterThan), 10),
            ["payload"] = new JsonObject { ["currency"] = "GBP" }
        };

        // Act
        var response = await _httpClient.PostAsJsonAsync("/rules/test", body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateRuleVersion_GivenAFieldNotOnTheSchema_ShouldReturnBadRequest()
    {
        // Arrange
        await AuthenticateAsync(UserRole.Admin, UserRole.Manager);
        await _httpClient.PutAsJsonAsync($"/organizations/{OrganizationId}/transaction-schema", ValidBody());

        var ruleEntity = await SeedFiringRuleAsync();

        var body = new JsonObject
        {
            ["ruleBody"] = RuleBodyJson("amont", nameof(RuleCondition.GreaterThan), 10)
        };

        // Act
        var response = await _httpClient.PostAsJsonAsync($"/rules/{ruleEntity.RuleId}/versions", body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateRuleVersion_GivenAFieldOnTheSchema_ShouldReturnCreated()
    {
        // Arrange
        await AuthenticateAsync(UserRole.Admin, UserRole.Manager);
        await _httpClient.PutAsJsonAsync($"/organizations/{OrganizationId}/transaction-schema", ValidBody());

        var ruleEntity = await SeedFiringRuleAsync();

        var body = new JsonObject
        {
            ["ruleBody"] = RuleBodyJson("amount", nameof(RuleCondition.LessThan), 10)
        };

        // Act
        var response = await _httpClient.PostAsJsonAsync($"/rules/{ruleEntity.RuleId}/versions", body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task EvaluateRuleSet_GivenAPayloadNotMatchingTheSchema_ShouldReturnBadRequest()
    {
        // Arrange
        await AuthenticateAsync(UserRole.Admin);
        await _httpClient.PutAsJsonAsync($"/organizations/{OrganizationId}/transaction-schema", ValidBody());

        var ruleEntity = await SeedFiringRuleAsync();
        await AuthenticateAsApiClientAsync();

        // Act
        var response = await _httpClient.PostAsJsonAsync(
            $"/rule-sets/{ruleEntity.RuleSetId}/evaluate",
            new JsonObject { ["currency"] = "GBP" });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task EvaluateAllRuleSets_GivenAPayloadNotMatchingTheSchema_ShouldReturnBadRequest()
    {
        // Arrange
        await AuthenticateAsync(UserRole.Admin);
        await _httpClient.PutAsJsonAsync($"/organizations/{OrganizationId}/transaction-schema", ValidBody());

        await SeedFiringRuleAsync();
        await AuthenticateAsApiClientAsync();

        // Act
        var response = await _httpClient.PostAsJsonAsync(
            "/rule-sets/evaluate",
            new JsonObject { ["currency"] = "GBP" });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetTransactionSchemaHistory_WhenNoneIsDefined_ShouldReturnNotFound()
    {
        // Arrange
        await AuthenticateAsync(UserRole.Admin);

        // Act
        var response = await _httpClient.GetAsync($"/organizations/{OrganizationId}/transaction-schema/history");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteTransactionSchema_GivenValidRequest_ShouldReturnNoContentAndThenNotFound()
    {
        // Arrange
        await AuthenticateAsync(UserRole.Admin);
        await _httpClient.PutAsJsonAsync($"/organizations/{OrganizationId}/transaction-schema", ValidBody());

        // Act
        var response = await _httpClient.DeleteAsync($"/organizations/{OrganizationId}/transaction-schema");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var get = await _httpClient.GetAsync($"/organizations/{OrganizationId}/transaction-schema");
        get.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteTransactionSchema_WhenNoneIsDefined_ShouldReturnNotFound()
    {
        // Arrange
        await AuthenticateAsync(UserRole.Admin);

        // Act
        var response = await _httpClient.DeleteAsync($"/organizations/{OrganizationId}/transaction-schema");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteTransactionSchema_CalledTwice_ShouldReturnNotFoundTheSecondTime()
    {
        // Arrange
        await AuthenticateAsync(UserRole.Admin);
        await _httpClient.PutAsJsonAsync($"/organizations/{OrganizationId}/transaction-schema", ValidBody());
        await _httpClient.DeleteAsync($"/organizations/{OrganizationId}/transaction-schema");

        // Act
        var response = await _httpClient.DeleteAsync($"/organizations/{OrganizationId}/transaction-schema");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteTransactionSchema_NotAdmin_ShouldReturnForbidden()
    {
        // Arrange
        await AuthenticateAsync(UserRole.User);

        // Act
        var response = await _httpClient.DeleteAsync($"/organizations/{OrganizationId}/transaction-schema");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task EvaluateRule_AfterTheSchemaIsDeleted_ShouldAcceptAPayloadThatBreachedIt()
    {
        // Arrange
        await AuthenticateAsync(UserRole.Admin);
        await _httpClient.PutAsJsonAsync($"/organizations/{OrganizationId}/transaction-schema", ValidBody());

        var ruleEntity = await SeedFiringRuleAsync();

        await _httpClient.DeleteAsync($"/organizations/{OrganizationId}/transaction-schema");
        await AuthenticateAsApiClientAsync();

        // Act
        var response = await _httpClient.PostAsJsonAsync(
            $"/rules/{ruleEntity.RuleId}/evaluate",
            new JsonObject { ["currency"] = "USD" });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CreateRule_AfterTheSchemaIsDeleted_ShouldAcceptAFieldThatWasNotOnIt()
    {
        // Arrange
        await AuthenticateAsync(UserRole.Admin, UserRole.Manager);
        await _httpClient.PutAsJsonAsync($"/organizations/{OrganizationId}/transaction-schema", ValidBody());
        await _httpClient.DeleteAsync($"/organizations/{OrganizationId}/transaction-schema");

        var ruleSetEntity = await SeedRuleSetAsync();

        // Act
        var response = await _httpClient.PostAsJsonAsync(
            $"/rule-sets/{ruleSetEntity.RuleSetId}/rules",
            CreateRuleBody("anythingAtAll", nameof(RuleCondition.GreaterThan), 10));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task ListTransactionSchemaVersions_AfterTheSchemaIsDeleted_ShouldStillReturnTheHistory()
    {
        // Arrange
        await AuthenticateAsync(UserRole.Admin);
        await _httpClient.PutAsJsonAsync($"/organizations/{OrganizationId}/transaction-schema", ValidBody());
        await _httpClient.DeleteAsync($"/organizations/{OrganizationId}/transaction-schema");

        // Act
        var versions = await _httpClient.GetAsync($"/organizations/{OrganizationId}/transaction-schema/versions");
        var version = await _httpClient.GetAsync($"/organizations/{OrganizationId}/transaction-schema/versions/1");
        var history = await _httpClient.GetAsync($"/organizations/{OrganizationId}/transaction-schema/history");

        // Assert
        versions.StatusCode.Should().Be(HttpStatusCode.OK);
        version.StatusCode.Should().Be(HttpStatusCode.OK);
        history.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task SetTransactionSchema_AfterADelete_ShouldResumeVersionNumberingOnTheSameSchema()
    {
        // Arrange
        await AuthenticateAsync(UserRole.Admin);
        var first = await _httpClient.PutAsJsonAsync($"/organizations/{OrganizationId}/transaction-schema", ValidBody());
        var original = await first.Content.ReadDataFromJsonAsync<TransactionSchemaResourceDto>();

        await _httpClient.DeleteAsync($"/organizations/{OrganizationId}/transaction-schema");

        var revised = new SetTransactionSchemaRequestBodyDto
        {
            Fields = [new SchemaFieldDto { Name = "amount", Type = nameof(SchemaFieldType.Number), IsRequired = true }]
        };

        // Act
        var response = await _httpClient.PutAsJsonAsync($"/organizations/{OrganizationId}/transaction-schema", revised);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var resource = await response.Content.ReadDataFromJsonAsync<TransactionSchemaResourceDto>();
        resource!.TransactionSchema.CurrentVersionNumber.Should().Be(2);
        resource.TransactionSchema.TransactionSchemaId.Should().Be(original!.TransactionSchema.TransactionSchemaId);

        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();
        context.TransactionSchemas.Count(x => x.OrganizationId == OrganizationId).Should().Be(1);
        context.TransactionSchemaVersions.Count(x => x.OrganizationId == OrganizationId).Should().Be(2);
    }

    [Fact]
    public async Task RecoverTransactionSchema_GivenADeletedSchema_ShouldRestoreItWithoutAddingAVersion()
    {
        // Arrange
        await AuthenticateAsync(UserRole.Admin);
        await _httpClient.PutAsJsonAsync($"/organizations/{OrganizationId}/transaction-schema", ValidBody());
        await _httpClient.DeleteAsync($"/organizations/{OrganizationId}/transaction-schema");

        // Act
        var response = await _httpClient.PostAsync($"/organizations/{OrganizationId}/transaction-schema/recover", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var get = await _httpClient.GetAsync($"/organizations/{OrganizationId}/transaction-schema");
        get.StatusCode.Should().Be(HttpStatusCode.OK);

        var resource = await get.Content.ReadDataFromJsonAsync<TransactionSchemaResourceDto>();
        resource!.TransactionSchema.CurrentVersionNumber.Should().Be(1);
        resource.TransactionSchema.Fields.Should().HaveCount(2);

        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();
        context.TransactionSchemaVersions.Count(x => x.OrganizationId == OrganizationId).Should().Be(1);
    }

    [Fact]
    public async Task RecoverTransactionSchema_WhenTheSchemaIsNotDeleted_ShouldReturnBadRequest()
    {
        // Arrange
        await AuthenticateAsync(UserRole.Admin);
        await _httpClient.PutAsJsonAsync($"/organizations/{OrganizationId}/transaction-schema", ValidBody());

        // Act
        var response = await _httpClient.PostAsync($"/organizations/{OrganizationId}/transaction-schema/recover", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task RecoverTransactionSchema_WhenNoneWasEverDefined_ShouldReturnNotFound()
    {
        // Arrange
        await AuthenticateAsync(UserRole.Admin);

        // Act
        var response = await _httpClient.PostAsync($"/organizations/{OrganizationId}/transaction-schema/recover", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteMyTransactionSchema_AsManager_ShouldReturnNoContent()
    {
        // Arrange
        await AuthenticateAsync(UserRole.Manager);
        await _httpClient.PutAsJsonAsync("/organizations/my/transaction-schema", ValidBody());

        // Act
        var response = await _httpClient.DeleteAsync("/organizations/my/transaction-schema");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var get = await _httpClient.GetAsync("/organizations/my/transaction-schema");
        get.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task RecoverMyTransactionSchema_AsManager_ShouldRestoreIt()
    {
        // Arrange
        await AuthenticateAsync(UserRole.Manager);
        await _httpClient.PutAsJsonAsync("/organizations/my/transaction-schema", ValidBody());
        await _httpClient.DeleteAsync("/organizations/my/transaction-schema");

        // Act
        var response = await _httpClient.PostAsync("/organizations/my/transaction-schema/recover", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var get = await _httpClient.GetAsync("/organizations/my/transaction-schema");
        get.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task DeleteTransactionSchema_ShouldBeRecordedInTheAuditHistory()
    {
        // Arrange
        await AuthenticateAsync(UserRole.Admin);
        await _httpClient.PutAsJsonAsync($"/organizations/{OrganizationId}/transaction-schema", ValidBody());

        // Act
        await _httpClient.DeleteAsync($"/organizations/{OrganizationId}/transaction-schema");

        // Assert
        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();
        context.AuditLogs
            .Count(x => x.EntityTypeId == (int)AuditableEntityType.TransactionSchema)
            .Should().BeGreaterThan(1);
    }

    [Fact]
    public async Task GetTransactionSchema_IncludingDeleted_ShouldReturnTheWithdrawnSchemaWithItsMarker()
    {
        // Arrange
        await AuthenticateAsync(UserRole.Admin);
        await _httpClient.PutAsJsonAsync($"/organizations/{OrganizationId}/transaction-schema", ValidBody());
        await _httpClient.DeleteAsync($"/organizations/{OrganizationId}/transaction-schema");

        // Act
        var response = await _httpClient.GetAsync($"/organizations/{OrganizationId}/transaction-schema?includeDeleted=true");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var resource = await response.Content.ReadDataFromJsonAsync<TransactionSchemaResourceDto>();
        resource!.TransactionSchema.IsDeleted.Should().BeTrue();
        resource.TransactionSchema.DeletedAt.Should().NotBeNull();
        resource.TransactionSchema.Fields.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetTransactionSchema_IncludingDeleted_WhenNoneWasEverDefined_ShouldStillReturnNotFound()
    {
        // Arrange
        await AuthenticateAsync(UserRole.Admin);

        // Act
        var response = await _httpClient.GetAsync($"/organizations/{OrganizationId}/transaction-schema?includeDeleted=true");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetTransactionSchema_WithoutIncludingDeleted_ShouldStillNotFindAWithdrawnSchema()
    {
        // Arrange
        await AuthenticateAsync(UserRole.Admin);
        await _httpClient.PutAsJsonAsync($"/organizations/{OrganizationId}/transaction-schema", ValidBody());
        await _httpClient.DeleteAsync($"/organizations/{OrganizationId}/transaction-schema");

        // Act
        var response = await _httpClient.GetAsync($"/organizations/{OrganizationId}/transaction-schema");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetTransactionSchema_GivenALiveSchema_ShouldReportItAsNotDeleted()
    {
        // Arrange
        await AuthenticateAsync(UserRole.Admin);
        await _httpClient.PutAsJsonAsync($"/organizations/{OrganizationId}/transaction-schema", ValidBody());

        // Act
        var response = await _httpClient.GetAsync($"/organizations/{OrganizationId}/transaction-schema");

        // Assert
        var resource = await response.Content.ReadDataFromJsonAsync<TransactionSchemaResourceDto>();
        resource!.TransactionSchema.IsDeleted.Should().BeFalse();
        resource.TransactionSchema.DeletedAt.Should().BeNull();
    }

    [Fact]
    public async Task GetMyTransactionSchema_IncludingDeleted_ShouldReturnTheWithdrawnSchema()
    {
        // Arrange
        await AuthenticateAsync(UserRole.Manager);
        await _httpClient.PutAsJsonAsync("/organizations/my/transaction-schema", ValidBody());
        await _httpClient.DeleteAsync("/organizations/my/transaction-schema");

        // Act
        var response = await _httpClient.GetAsync("/organizations/my/transaction-schema?includeDeleted=true");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var resource = await response.Content.ReadDataFromJsonAsync<TransactionSchemaResourceDto>();
        resource!.TransactionSchema.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task GetMyTransactionSchemaHistory_AsManager_ShouldReturnTheTrailIncludingTheWithdrawal()
    {
        // Arrange
        await AuthenticateAsync(UserRole.Manager);
        await _httpClient.PutAsJsonAsync("/organizations/my/transaction-schema", ValidBody());
        await _httpClient.DeleteAsync("/organizations/my/transaction-schema");

        // Act
        var response = await _httpClient.GetAsync("/organizations/my/transaction-schema/history");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var page = await response.Content.ReadFromJsonAsync<PagedResponseDto<AuditLogResourceDto>>();
        page!.Data.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetMyTransactionSchemaHistory_WhenNoneWasEverDefined_ShouldReturnNotFound()
    {
        // Arrange
        await AuthenticateAsync(UserRole.Manager);

        // Act
        var response = await _httpClient.GetAsync("/organizations/my/transaction-schema/history");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetMyTransactionSchemaHistory_NotManagerOrAdmin_ShouldReturnForbidden()
    {
        // Arrange
        await AuthenticateAsync(UserRole.User);

        // Act
        var response = await _httpClient.GetAsync("/organizations/my/transaction-schema/history");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task SetTransactionSchema_GivenNestedArrayAndTimestampFields_ShouldReturnOkAndReadThemBack()
    {
        // Arrange
        await AuthenticateAsync(UserRole.Admin);

        // Act
        var response = await _httpClient.PutAsJsonAsync($"/organizations/{OrganizationId}/transaction-schema", StructuredBody());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var resource = await response.Content.ReadDataFromJsonAsync<TransactionSchemaResourceDto>();
        var parties = resource!.TransactionSchema.Fields.Single(x => x.Name == "parties");
        parties.ElementType.Should().Be(nameof(SchemaFieldType.Object));
        parties.Fields.Should().ContainSingle(x => x.Name == "country");
    }

    [Fact]
    public async Task SetTransactionSchema_GivenAnObjectDeclaringNoMembers_ShouldReturnBadRequest()
    {
        // Arrange
        await AuthenticateAsync(UserRole.Admin);

        var body = new SetTransactionSchemaRequestBodyDto
        {
            Fields = [new SchemaFieldDto { Name = "card", Type = nameof(SchemaFieldType.Object), IsRequired = false }]
        };

        // Act
        var response = await _httpClient.PutAsJsonAsync($"/organizations/{OrganizationId}/transaction-schema", body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ValidateRule_GivenANestedPathOnTheSchema_ShouldReturnOk()
    {
        // Arrange
        await AuthenticateAsync(UserRole.Admin);
        await _httpClient.PutAsJsonAsync($"/organizations/{OrganizationId}/transaction-schema", StructuredBody());

        // Act
        var response = await _httpClient.PostAsJsonAsync("/rules/validate", RuleBodyJson("card.bin", nameof(RuleCondition.StartsWith), "4532"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ValidateRule_GivenANestedPathTheSchemaDoesNotDeclare_ShouldReturnBadRequest()
    {
        // Arrange
        await AuthenticateAsync(UserRole.Admin);
        await _httpClient.PutAsJsonAsync($"/organizations/{OrganizationId}/transaction-schema", StructuredBody());

        // Act
        var response = await _httpClient.PostAsJsonAsync("/rules/validate", RuleBodyJson("card.scheme", nameof(RuleCondition.EqualTo), "VISA"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var problem = await response.Content.ReadFromJsonAsync<JsonObject>();
        problem!["violations"]![0]!["field"]!.GetValue<string>().Should().Be("card.scheme");
    }

    [Fact]
    public async Task ValidateRule_GivenATemporalExpressionOverADeclaredTimestamp_ShouldReturnOk()
    {
        // Arrange
        await AuthenticateAsync(UserRole.Admin);
        await _httpClient.PutAsJsonAsync($"/organizations/{OrganizationId}/transaction-schema", StructuredBody());

        var ruleBody = new JsonObject
        {
            ["condition"] = nameof(RuleCondition.LessThan),
            ["expression"] = new JsonObject
            {
                ["function"] = nameof(RuleFunction.DaysSince),
                ["arguments"] = new JsonArray(new JsonObject { ["field"] = "occurredAt" })
            },
            ["value"] = 30
        };

        // Act
        var response = await _httpClient.PostAsJsonAsync("/rules/validate", ruleBody);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ValidateRule_GivenATemporalExpressionOverANumber_ShouldReturnBadRequest()
    {
        // Arrange
        await AuthenticateAsync(UserRole.Admin);
        await _httpClient.PutAsJsonAsync($"/organizations/{OrganizationId}/transaction-schema", StructuredBody());

        var ruleBody = new JsonObject
        {
            ["condition"] = nameof(RuleCondition.LessThan),
            ["expression"] = new JsonObject
            {
                ["function"] = nameof(RuleFunction.DaysSince),
                ["arguments"] = new JsonArray(new JsonObject { ["field"] = "amount" })
            },
            ["value"] = 30
        };

        // Act
        var response = await _httpClient.PostAsJsonAsync("/rules/validate", ruleBody);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task EvaluateRule_GivenAPayloadMatchingAStructuredSchema_ShouldReturnOk()
    {
        // Arrange
        await AuthenticateAsync(UserRole.Admin);
        await _httpClient.PutAsJsonAsync($"/organizations/{OrganizationId}/transaction-schema", StructuredBody());

        var ruleEntity = await SeedFiringRuleAsync();
        await AuthenticateAsApiClientAsync();

        // Act
        var response = await _httpClient.PostAsJsonAsync($"/rules/{ruleEntity.RuleId}/evaluate", StructuredPayload());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task EvaluateRule_GivenAnArrayElementOfTheWrongType_ShouldReturnBadRequest()
    {
        // Arrange
        await AuthenticateAsync(UserRole.Admin);
        await _httpClient.PutAsJsonAsync($"/organizations/{OrganizationId}/transaction-schema", StructuredBody());

        var ruleEntity = await SeedFiringRuleAsync();
        await AuthenticateAsApiClientAsync();

        var payload = StructuredPayload();
        payload["parties"] = new JsonArray(new JsonObject { ["country"] = 44 });

        // Act
        var response = await _httpClient.PostAsJsonAsync($"/rules/{ruleEntity.RuleId}/evaluate", payload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var problem = await response.Content.ReadFromJsonAsync<JsonObject>();
        problem!["violations"]![0]!["field"]!.GetValue<string>().Should().Be("parties[0].country");
    }

    [Fact]
    public async Task EvaluateRule_GivenSomethingThatIsNotATimestamp_ShouldReturnBadRequest()
    {
        // Arrange
        await AuthenticateAsync(UserRole.Admin);
        await _httpClient.PutAsJsonAsync($"/organizations/{OrganizationId}/transaction-schema", StructuredBody());

        var ruleEntity = await SeedFiringRuleAsync();
        await AuthenticateAsApiClientAsync();

        var payload = StructuredPayload();
        payload["occurredAt"] = "yesterday";

        // Act
        var response = await _httpClient.PostAsJsonAsync($"/rules/{ruleEntity.RuleId}/evaluate", payload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var problem = await response.Content.ReadFromJsonAsync<JsonObject>();
        problem!["violations"]![0]!["field"]!.GetValue<string>().Should().Be("occurredAt");
    }

    private static JsonObject StructuredPayload()
    {
        return new JsonObject
        {
            ["amount"] = 500,
            ["currency"] = "GBP",
            ["occurredAt"] = "2026-08-16T13:30:00Z",
            ["card"] = new JsonObject { ["bin"] = "453201" },
            ["parties"] = new JsonArray(
                new JsonObject { ["country"] = "GB" },
                new JsonObject { ["country"] = "IR" })
        };
    }

    private static SetTransactionSchemaRequestBodyDto StructuredBody()
    {
        return new SetTransactionSchemaRequestBodyDto
        {
            Fields =
            [
                new SchemaFieldDto { Name = "amount", Type = nameof(SchemaFieldType.Number), IsRequired = true },
                new SchemaFieldDto { Name = "occurredAt", Type = nameof(SchemaFieldType.DateTime), IsRequired = false },
                new SchemaFieldDto
                {
                    Name = "card",
                    Type = nameof(SchemaFieldType.Object),
                    IsRequired = false,
                    Fields = [new SchemaFieldDto { Name = "bin", Type = nameof(SchemaFieldType.String), IsRequired = false }]
                },
                new SchemaFieldDto
                {
                    Name = "parties",
                    Type = nameof(SchemaFieldType.Array),
                    IsRequired = false,
                    ElementType = nameof(SchemaFieldType.Object),
                    Fields = [new SchemaFieldDto { Name = "country", Type = nameof(SchemaFieldType.String), IsRequired = false }]
                }
            ]
        };
    }

    private static JsonObject RuleBodyJson(string field, string condition, object value)
    {
        return new JsonObject
        {
            ["condition"] = condition,
            ["field"] = field,
            ["value"] = JsonValue.Create(value)
        };
    }

    private static SetTransactionSchemaRequestBodyDto ValidBody()
    {
        return new SetTransactionSchemaRequestBodyDto
        {
            Fields =
            [
                new SchemaFieldDto { Name = "amount", Type = nameof(SchemaFieldType.Number), IsRequired = true },
                new SchemaFieldDto
                {
                    Name = "currency",
                    Type = nameof(SchemaFieldType.String),
                    IsRequired = false,
                    AllowedValues = ["GBP", "EUR"]
                }
            ]
        };
    }

    private JsonObject CreateRuleBody(string field, string condition, object value)
    {
        return new JsonObject
        {
            ["friendlyName"] = _fixture.Create<string>(),
            ["ruleBody"] = new JsonObject
            {
                ["condition"] = condition,
                ["field"] = field,
                ["value"] = JsonValue.Create(value)
            },
            ["action"] = nameof(RuleAction.Review),
            ["severity"] = nameof(RuleSeverity.Medium),
            ["riskWeight"] = 0,
            ["isEnabled"] = true
        };
    }

    private async Task<RuleSetEntity> SeedRuleSetAsync()
    {
        var ruleSetEntity = FakeRuleSetEntity.CreateValid(_fixture) with
        {
            OrganizationId = OrganizationId,
            Organization = null!,
            IsEnabled = true,
            IsDeleted = false
        };

        await DatabaseHelper.CreateRuleSets(testFixture.Services, [ruleSetEntity]);

        return ruleSetEntity;
    }

    private async Task<RuleEntity> SeedFiringRuleAsync()
    {
        var ruleBody = new RuleBody
        {
            Condition = RuleCondition.GreaterThan,
            Field = new RuleField("amount"),
            Value = new RuleValue(10),
            And = null,
            Or = null,
            Not = null
        };

        var ruleEntity = FakeRuleEntity.CreateValid(_fixture) with
        {
            OrganizationId = OrganizationId,
            Organization = null!,
            RuleBody = ruleBody.ToBase64String(),
            ActionId = (int)RuleAction.Flag,
            SeverityId = (int)RuleSeverity.Medium,
            RiskWeight = 0,
            IsEnabled = true,
            IsDeleted = false
        };

        var ruleSetEntity = FakeRuleSetEntity.CreateValid(_fixture) with
        {
            RuleSetId = ruleEntity.RuleSetId,
            OrganizationId = OrganizationId,
            Organization = null!,
            IsEnabled = true,
            IsDeleted = false,
            Rules = [ruleEntity]
        };

        await DatabaseHelper.CreateRuleSets(testFixture.Services, [ruleSetEntity]);

        return ruleEntity;
    }

    private async Task AuthenticateAsync(params UserRole[] roles)
    {
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User, .. roles], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);
    }

    private async Task AuthenticateAsApiClientAsync()
    {
        var applicationUserEntity = (FakeApplicationUser.CreateValid(_fixture) with
        {
            OrganizationId = new OrganizationId(OrganizationId),
            IsDeleted = new IsDeleted(false)
        }).ToEntity();

        await DatabaseHelper.CreateApplicationUsers(testFixture.Services, [applicationUserEntity]);

        var createApiClientRequest = FakeCreateApiClientRequest.CreateValid(_fixture) with
        {
            OrganizationId = new OrganizationId(OrganizationId),
            UserId = new ApplicationUserId(applicationUserEntity.Id)
        };

        var createdApiClient = await AuthenticationHelper.CreateApiClientAsync(testFixture.Services, createApiClientRequest);
        await AuthenticationHelper.SetClientCredentialsTokenAsync(_httpClient, createdApiClient.ApiClient.ClientId, createdApiClient.ClientSecret);
        _httpClient.DefaultRequestHeaders.Add(HeaderNames.CreateCase, "true");
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
