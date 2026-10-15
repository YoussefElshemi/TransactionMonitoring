using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Moq;
using Presentation.Constants;
using Presentation.Middlewares;
using UnitTests.TestHelpers;

namespace UnitTests.Tests.Presentation.Middlewares;

public class CorrelationIdMiddlewareTests : BaseTestClass
{
    private readonly CorrelationIdMiddleware _sut = new(async _ => await Task.CompletedTask);

    [Fact]
    public async Task InvokeAsync_GivenRequest_AddsCorrelationIdToResponseHeaders()
    {
        // Arrange
        var mockHttpContext = new Mock<HttpContext>();
        var mockResponse = new Mock<HttpResponse>();
        var mockRequest = new Mock<HttpRequest>();

        var responseHeaders = new HeaderDictionary();
        mockResponse
            .Setup(r => r.Headers)
            .Returns(responseHeaders);

        mockHttpContext
            .Setup(c => c.Response)
            .Returns(mockResponse.Object);

        mockHttpContext
            .Setup(c => c.Request)
            .Returns(mockRequest.Object);

        mockResponse.Setup(r => r.OnStarting(It.IsAny<Func<Task>>()))
            .Callback<Func<Task>>(callback => callback());

        // Act
        await _sut.InvokeAsync(mockHttpContext.Object);

        // Assert
        responseHeaders.Should().ContainKey(HeaderNames.CorrelationId);
        Guid.TryParse(responseHeaders[HeaderNames.CorrelationId], out _).Should().BeTrue();
    }

    [Fact]
    public async Task InvokeAsync_GivenRequest_GivesTheRequestTheIdItPutsInTheHeader()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        string? seenByTheHandler = null;

        var sut = new CorrelationIdMiddleware(context =>
        {
            seenByTheHandler = context.TraceIdentifier;

            return Task.CompletedTask;
        });

        // Act
        await sut.InvokeAsync(httpContext);

        // Assert
        Guid.TryParse(seenByTheHandler, out _).Should().BeTrue();
        httpContext.TraceIdentifier.Should().Be(seenByTheHandler);
    }

    [Fact]
    public async Task InvokeAsync_GivenACorrelationIdOnTheRequest_DoesNotAdoptIt()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers[HeaderNames.CorrelationId] = "chosen-by-the-caller";

        // Act
        await _sut.InvokeAsync(httpContext);

        // Assert
        httpContext.TraceIdentifier.Should().NotBe("chosen-by-the-caller");
    }
}