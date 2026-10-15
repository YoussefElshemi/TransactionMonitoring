using System.Collections.Concurrent;
using BehaviouralTests.TestHelpers;
using Core.Enums;
using Core.Interfaces.Services;
using Core.Models.RateLimits;
using Core.ValueObjects;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BehaviouralTests.Tests.RateLimiting;

// Testing turns rate limiting off for every other class; this one re-enables it with a recording store.
public class RateLimitedTestFixture : TestFixture
{
    public RecordingDistributedRateLimiter Recorder { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        builder.UseSetting("AppConfig:RateLimitConfig:Enabled", "true");
        builder.UseSetting("AppConfig:RateLimitConfig:Distributed:Enabled", "true");

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IDistributedRateLimiter>();
            services.AddSingleton<IDistributedRateLimiter>(Recorder);
        });
    }
}

// Allows everything and records keys: the test is about which buckets are charged, not exhaustion.
public class RecordingDistributedRateLimiter : IDistributedRateLimiter
{
    private readonly ConcurrentQueue<string> _keys = new();

    public IReadOnlyList<string> Keys => _keys.ToArray();

    public void Clear()
    {
        _keys.Clear();
    }

    public Task<RateLimitDecision> AcquireAsync(
        RateLimitAcquireRequest rateLimitAcquireRequest,
        CancellationToken stoppingToken)
    {
        foreach (var bucket in rateLimitAcquireRequest.Buckets)
        {
            _keys.Enqueue(bucket.Key);
        }

        return Task.FromResult(new RateLimitDecision
        {
            Outcome = RateLimitOutcome.Allowed,
            PermitLimit = new PermitLimit(1_000),
            RemainingPermits = new RemainingPermits(999),
            RetryAfter = new RetryAfter(0)
        });
    }
}
