using System.Net;
using System.Text.Json;
using Core.Exceptions;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Presentation.ExceptionHandlers;
using UnitTests.TestHelpers;

namespace UnitTests.Tests.Presentation.ExceptionHandlers;

public class DomainExceptionHandlerTests : BaseTestClass
{
    private readonly DomainExceptionHandler _sut = new(null);

    private sealed class TestDomainException(string message, HttpStatusCode statusCode, object? extensions = null)
        : DomainException(message)
    {
        public override HttpStatusCode GetStatusCode() => statusCode;
        public override string GetTitle() => "A test problem";
        public override object? GetExtensions() => extensions;
    }

    [Fact]
    public async Task TryHandleAsync_GivenANonDomainException_DoesNotHandleIt()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();

        // Act
        var handled = await _sut.TryHandleAsync(httpContext, new InvalidOperationException("boom"), CancellationToken.None);

        // Assert
        handled.Should().BeFalse();
    }

    [Fact]
    public async Task TryHandleAsync_TakesTheStatusCodeFromTheException()
    {
        // Arrange
        var httpContext = NewHttpContext();
        var exception = new TestDomainException("Something the caller did", HttpStatusCode.BadRequest);

        // Act
        var handled = await _sut.TryHandleAsync(httpContext, exception, CancellationToken.None);

        // Assert
        handled.Should().BeTrue();
        httpContext.Response.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);

        var problemDetails = await ReadProblemDetailsAsync(httpContext);
        problemDetails.Title.Should().Be("A test problem");
        problemDetails.Detail.Should().Be("Something the caller did");
        problemDetails.Status.Should().Be((int)HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task TryHandleAsync_GivenADifferentStatusCode_UsesThatOne()
    {
        // Arrange
        var httpContext = NewHttpContext();
        var exception = new TestDomainException("Already there", HttpStatusCode.Conflict);

        // Act
        await _sut.TryHandleAsync(httpContext, exception, CancellationToken.None);

        // Assert
        httpContext.Response.StatusCode.Should().Be((int)HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task TryHandleAsync_FlattensTheExtensionsObjectIntoCamelCasedMembers()
    {
        // Arrange
        var httpContext = NewHttpContext();
        var exception = new TestDomainException(
            "Something the caller did",
            HttpStatusCode.BadRequest,
            new { TransactionReference = "pay_9f3c21", AttemptCount = 2 });

        // Act
        await _sut.TryHandleAsync(httpContext, exception, CancellationToken.None);

        // Assert
        var body = await ReadBodyAsync(httpContext);
        body.GetProperty("transactionReference").GetString().Should().Be("pay_9f3c21");
        body.GetProperty("attemptCount").GetInt32().Should().Be(2);
    }

    [Fact]
    public async Task TryHandleAsync_GivenNoExtensions_WritesOnlyTheCodeAndTheCorrelationId()
    {
        // Arrange
        var httpContext = NewHttpContext();
        var exception = new TestDomainException("Something the caller did", HttpStatusCode.BadRequest);

        // Act
        await _sut.TryHandleAsync(httpContext, exception, CancellationToken.None);

        // Assert
        var problemDetails = await ReadProblemDetailsAsync(httpContext);
        problemDetails.Extensions.Keys.Should().BeEquivalentTo("code", "correlationId");
    }

    [Fact]
    public async Task TryHandleAsync_GivenAnExtensionNamedLikeAStandardMember_LeavesTheStandardOneIntact()
    {
        // Arrange
        var httpContext = NewHttpContext();
        var exception = new TestDomainException(
            "The real message",
            HttpStatusCode.BadRequest,
            new { Detail = "an impostor" });

        // Act
        await _sut.TryHandleAsync(httpContext, exception, CancellationToken.None);

        // Assert
        var body = await ReadBodyAsync(httpContext);
        body.GetProperty("detail").GetString().Should().Be("The real message");
    }

    [Fact]
    public async Task TryHandleAsync_WritesTheExceptionsCodeAndTheRequestsCorrelationId()
    {
        // Arrange
        var httpContext = NewHttpContext();
        httpContext.TraceIdentifier = "3f6c1e0a-8a41-4d0e-9d55-0c5f6f1f2b77";
        var exception = new TestDomainException("Something the caller did", HttpStatusCode.Conflict);

        // Act
        await _sut.TryHandleAsync(httpContext, exception, CancellationToken.None);

        // Assert
        var body = await ReadBodyAsync(httpContext);
        body.GetProperty("code").GetString().Should().Be("test_domain");
        body.GetProperty("correlationId").GetString().Should().Be("3f6c1e0a-8a41-4d0e-9d55-0c5f6f1f2b77");
    }

    [Fact]
    public async Task TryHandleAsync_GivenAnExtensionNamedCode_LeavesThePublishedCodeIntact()
    {
        // Arrange
        var httpContext = NewHttpContext();
        var exception = new TestDomainException(
            "Something the caller did",
            HttpStatusCode.BadRequest,
            new { Code = "an impostor" });

        // Act
        await _sut.TryHandleAsync(httpContext, exception, CancellationToken.None);

        // Assert
        var body = await ReadBodyAsync(httpContext);
        body.GetProperty("code").GetString().Should().Be("test_domain");
    }

    private static DefaultHttpContext NewHttpContext()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Response.Body = new MemoryStream();
        httpContext.Request.Path = "/rule-sets/evaluate";

        return httpContext;
    }

    private static async Task<JsonElement> ReadBodyAsync(HttpContext httpContext)
    {
        httpContext.Response.Body.Seek(0, SeekOrigin.Begin);
        using var document = await JsonDocument.ParseAsync(httpContext.Response.Body);

        return document.RootElement.Clone();
    }

    private static async Task<ProblemDetails> ReadProblemDetailsAsync(HttpContext httpContext)
    {
        httpContext.Response.Body.Seek(0, SeekOrigin.Begin);

        return (await JsonSerializer.DeserializeAsync<ProblemDetails>(
            httpContext.Response.Body,
            new JsonSerializerOptions(JsonSerializerDefaults.Web)))!;
    }
}
