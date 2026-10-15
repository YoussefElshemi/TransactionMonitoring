using System.Net;
using AutoFixture;
using Core.Enums;
using Core.Interfaces;
using Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Moq.Protected;
using Serilog;
using UnitTests.TestHelpers.FixtureCustomizations;

namespace UnitTests.TestHelpers;

public class BaseTestClass
{
    protected readonly IFixture Fixture = new Fixture();
    protected readonly Mock<ILogger> MockLogger = new();
    protected readonly TimeProvider MockTimeProvider = new MockTimeProvider();

    protected readonly IModeContext TestModeContext = new ModeContext { Current = Mode.Live };

    protected BaseTestClass()
    {
        Fixture.Customize(new ValidDateOnlyCustomization());
        Fixture.Customize(new ValidDateTimeOffsetCustomization());
    }

    protected static IServiceScopeFactory SetUpServiceScopeFactory(params (Type ServiceType, object Implementation)[] services)
    {
        var mockScopeFactory = new Mock<IServiceScopeFactory>();
        var mockScope = new Mock<IServiceScope>();
        var mockServiceProvider = new Mock<IServiceProvider>();

        mockScopeFactory
            .Setup(f => f.CreateScope())
            .Returns(mockScope.Object);

        mockScope
            .Setup(s => s.ServiceProvider)
            .Returns(mockServiceProvider.Object);

        mockServiceProvider
            .Setup(p => p.GetService(It.IsAny<Type>()))
            .Returns<Type>(type =>
            {
                var service = services.FirstOrDefault(s => s.ServiceType == type).Implementation;
                return service;
            });

        return mockScopeFactory.Object;
    }

    protected static HttpClient SetUpHttpClient(HttpStatusCode statusCode, string? content = null)
    {
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = !string.IsNullOrWhiteSpace(content) ? new StringContent(content) : null
            });

        return new HttpClient(handlerMock.Object);
    }
}