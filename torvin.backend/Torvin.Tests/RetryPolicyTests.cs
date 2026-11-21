extern alias worker;

using Microsoft.Extensions.Logging.Abstractions;
using worker::Torvin.Worker.Consumers;

namespace Torvin.Tests;

public class RetryPolicyTests
{
    private static (RetryPolicy Policy, List<TimeSpan> Delays) NewPolicy()
    {
        var delays = new List<TimeSpan>();
        var policy = new RetryPolicy(NullLogger<RetryPolicy>.Instance)
        {
            Delay = (delay, _) =>
            {
                delays.Add(delay);
                return Task.CompletedTask;
            },
        };
        return (policy, delays);
    }

    // Pokušaj koji padne zadani broj puta, pa vrati zadani ishod
    private static Func<CancellationToken, Task<bool>> Flaky(int failures, Counter calls, bool result = true,
        Exception? error = null) => _ =>
    {
        calls.Value++;
        return calls.Value <= failures
            ? Task.FromException<bool>(error ?? new TimeoutException())
            : Task.FromResult(result);
    };

    private sealed class Counter
    {
        public int Value { get; set; }
    }

    [Fact]
    public async Task TransientFailures_AreRetried_ThenAcknowledged()
    {
        var (policy, delays) = NewPolicy();
        var calls = new Counter();

        Assert.True(await policy.RunAsync("id", "Test", Flaky(2, calls), _ => false, CancellationToken.None));
        Assert.Equal(3, calls.Value);
        Assert.Equal([1, 2], delays.Select(d => d.TotalSeconds));
    }

    [Fact]
    public async Task PersistentFailure_WaitsOneTwoFourEight_ThenGivesUp()
    {
        var (policy, delays) = NewPolicy();
        var calls = new Counter();

        Assert.False(await policy.RunAsync("id", "Test", Flaky(int.MaxValue, calls), _ => false, CancellationToken.None));
        Assert.Equal(5, calls.Value);
        Assert.Equal([1, 2, 4, 8], delays.Select(d => d.TotalSeconds));
    }

    [Fact]
    public async Task PermanentFailure_IsNotRetried()
    {
        var (policy, delays) = NewPolicy();
        var calls = new Counter();

        Assert.False(await policy.RunAsync("id", "Test", Flaky(int.MaxValue, calls, error: new InvalidOperationException()),
            ex => ex is InvalidOperationException, CancellationToken.None));
        Assert.Equal(1, calls.Value);
        Assert.Empty(delays);
    }

    [Fact]
    public async Task AttemptCanSendMessageToDeadLetterWithoutRetry()
    {
        var (policy, delays) = NewPolicy();
        var calls = new Counter();

        Assert.False(await policy.RunAsync("id", "Test", Flaky(0, calls, result: false), _ => false, CancellationToken.None));
        Assert.Equal(1, calls.Value);
        Assert.Empty(delays);
    }
}
