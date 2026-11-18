extern alias worker;

using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Torvin.Model.Messages;
using worker::Torvin.Worker.Consumers;
using worker::Torvin.Worker.Email;

namespace Torvin.Tests;

public class EmailMessageHandlerTests
{
    // Sender koji padne zadani broj puta, pa uspije
    private sealed class FlakySender(int failures, Exception? error = null) : IEmailSender
    {
        public int Calls { get; private set; }

        public Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken = default)
        {
            Calls++;
            return Calls <= failures
                ? Task.FromException(error ?? new IOException("SMTP nije dostupan."))
                : Task.CompletedTask;
        }
    }

    private static (EmailMessageHandler Handler, List<TimeSpan> Delays) NewHandler(IEmailSender sender)
    {
        var delays = new List<TimeSpan>();
        var handler = new EmailMessageHandler(sender, NullLogger<EmailMessageHandler>.Instance)
        {
            Delay = (delay, _) =>
            {
                delays.Add(delay);
                return Task.CompletedTask;
            },
        };
        return (handler, delays);
    }

    private static ReadOnlyMemory<byte> Message() =>
        JsonSerializer.SerializeToUtf8Bytes(new EmailRequested(Guid.NewGuid(), "amina@torvin.local", "Naslov", "Tijelo"));

    [Fact]
    public async Task TransientFailures_AreRetried_ThenAcked()
    {
        var sender = new FlakySender(failures: 2);
        var (handler, delays) = NewHandler(sender);

        Assert.True(await handler.HandleAsync(Message(), "id", CancellationToken.None));
        Assert.Equal(3, sender.Calls);
        Assert.Equal([TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2)], delays);
    }

    [Fact]
    public async Task PersistentFailure_WaitsOneTwoFourEight_ThenGoesToDeadLetter()
    {
        var sender = new FlakySender(failures: int.MaxValue);
        var (handler, delays) = NewHandler(sender);

        Assert.False(await handler.HandleAsync(Message(), "id", CancellationToken.None));
        Assert.Equal(5, sender.Calls);
        Assert.Equal([1, 2, 4, 8], delays.Select(d => d.TotalSeconds));
    }

    [Fact]
    public async Task RejectedEmail_GoesToDeadLetterWithoutRetry()
    {
        var sender = new FlakySender(failures: int.MaxValue, new EmailRejectedException("Adresa primaoca nije ispravna.", new FormatException()));
        var (handler, delays) = NewHandler(sender);

        Assert.False(await handler.HandleAsync(Message(), "id", CancellationToken.None));
        Assert.Equal(1, sender.Calls);
        Assert.Empty(delays);
    }

    [Theory]
    [InlineData("nije json")]
    [InlineData("{\"MessageId\":\"00000000-0000-0000-0000-000000000001\",\"To\":\"\",\"Subject\":\"s\",\"Body\":\"b\"}")]
    public async Task InvalidMessage_GoesToDeadLetterWithoutSending(string body)
    {
        var sender = new FlakySender(failures: 0);
        var (handler, _) = NewHandler(sender);

        Assert.False(await handler.HandleAsync(Encoding.UTF8.GetBytes(body), "id", CancellationToken.None));
        Assert.Equal(0, sender.Calls);
    }
}
