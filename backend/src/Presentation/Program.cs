using System.Text.Json.Serialization;
using Core.Configs;
using Core.Enums;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Presentation.Attributes;
using Presentation.Constants;
using Presentation.Converters;
using Presentation.Extensions;
using Presentation.Filters;
using Presentation.Middlewares;
using Quartz.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.RegisterConfigurations(builder.Configuration);
var appConfig = new AppConfig();
builder.Configuration.GetSection(nameof(AppConfig)).Bind(appConfig);

PayloadProtectionGuard.Enforce(appConfig);

builder.ConfigureSerilog(appConfig);

builder.Services
    .RegisterOpenTelemetry(appConfig)
    .RegisterDataAccess(builder.Configuration)
    .RegisterQuartz(builder.Configuration)
    .RegisterAuth()
    .RegisterServices()
    .RegisterResponseFactories()
    .RegisterValidators()
    .RegisterExceptionHandlers()
    .RegisterHealthChecks()
    .RegisterRateLimiting(builder.Configuration)
    .AddHttpClient()
    .AddCors(o => o.AddPolicy("CorsPolicy", corsPolicyBuilder =>
    {
        corsPolicyBuilder
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials()
            .SetIsOriginAllowed(_ => true)
            // Response headers are invisible to browser script unless exposed here.
            .WithExposedHeaders(
                HeaderNames.CorrelationId,
                HeaderNames.IdempotentReplay,
                HeaderNames.ContentDisposition,
                HeaderNames.RetryAfter,
                HeaderNames.RateLimitLimit,
                HeaderNames.RateLimitRemaining,
                HeaderNames.RateLimitReset,
                HeaderNames.ApiVersion,
                HeaderNames.Deprecation,
                HeaderNames.Sunset,
                HeaderNames.Link);
    }))
    .AddProblemDetails(options => options.CustomizeProblemDetails = context =>
        context.ProblemDetails.EnsureContract(context.HttpContext))
    .AddEndpointsApiExplorer()
    .AddSwaggerGen()
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        options.JsonSerializerOptions.Converters.Add(new PatchJsonConverterFactory());
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    })
    .AddMvcOptions(options =>
    {
        options.MaxModelValidationErrors = int.MaxValue;
        options.MaxValidationDepth = int.MaxValue;
        options.MaxModelBindingCollectionSize = int.MaxValue;

        options.Filters.Add<SandboxWriteMeterFilter>();
        options.MaxModelBindingRecursionDepth = int.MaxValue;
    });

var app = builder.Build();

// Outermost so the exception handler's log line carries the correlation id.
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseExceptionHandler();
app.UseCors("CorsPolicy");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

// After authentication (needs the organisation), before the rest so the stopwatch covers what the caller waited for.
app.UseMiddleware<AvailabilityMeasurementMiddleware>();

// Before the rate limiter so a 429 still carries and counts its version.
app.UseMiddleware<ApiVersionMiddleware>();

// After authentication so buckets are per caller; before the middlewares below because they hit the database.
app.UseMiddleware<RateLimitMiddleware>();

app.UseMiddleware<ModeContextMiddleware>();

app.UseMiddleware<EnsureUserExistsMiddleware>();
app.UseMiddleware<AuditContextMiddleware>();

app.MapControllers();
app.MapQuartzApi().RequireAuthorization(a => a.RequireRole(nameof(UserRole.Admin)));

app.MapHealthChecks("/health").WithMetadata(new RateLimitExemptAttribute());
app.MapHealthChecks("/health/detailed", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
}).WithMetadata(new RateLimitExemptAttribute());

app.Run();

namespace Presentation
{
    public class Program { }
}