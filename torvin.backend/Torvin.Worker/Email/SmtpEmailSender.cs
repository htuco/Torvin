using MailKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using Torvin.Services.Configuration;

namespace Torvin.Worker.Email;

// Svaki mail otvara svoju SMTP vezu. Mailova je malo, a tako nema veze koja je pukla dok je čekala
public sealed class SmtpEmailSender(IOptions<SmtpOptions> options) : IEmailSender
{
    public async Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken = default)
    {
        var smtp = options.Value;

        using var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse(smtp.From));
        if (!MailboxAddress.TryParse(to, out var recipient))
        {
            throw new EmailRejectedException("Adresa primaoca nije ispravna.", new FormatException());
        }

        message.To.Add(recipient);
        message.Subject = subject;
        message.Body = new TextPart("plain") { Text = body };

        using var client = new SmtpClient();
        // Bez SSL-a se veza ipak šifruje kad server to podržava (STARTTLS), a Mailpit radi i bez toga
        var security = smtp.UseSsl ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTlsWhenAvailable;
        await client.ConnectAsync(smtp.Host, smtp.Port, security, cancellationToken);

        // Mailpit u razvoju ne traži prijavu, pa se prijavljuje samo kad je korisnik zadan u .env
        if (!string.IsNullOrEmpty(smtp.User))
        {
            await client.AuthenticateAsync(smtp.User, smtp.Password, cancellationToken);
        }

        try
        {
            await client.SendAsync(new SendRequest(message), cancellationToken);
        }
        // 5xx znači da server trajno odbija mail (npr. nepostojeći primalac), a 4xx je prolazna greška za retry
        catch (SmtpCommandException ex) when ((int)ex.StatusCode >= 500)
        {
            throw new EmailRejectedException($"SMTP server je trajno odbio mail (status {(int)ex.StatusCode}).", ex);
        }

        await client.DisconnectAsync(quit: true, cancellationToken);
    }
}
