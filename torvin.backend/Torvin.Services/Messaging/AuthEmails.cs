using Torvin.Model.Enums;
using Torvin.Model.Messages;
using Torvin.Services.Database.Entities;
using Torvin.Services.Security;

namespace Torvin.Services.Messaging;

// Tekst mailova auth toka. Rok važenja u tekstu dolazi iz istog mjesta kao rok u bazi
public static class AuthEmails
{
    public static EmailRequested Registration(User user, string code) => Create(user,
        "Torvin: kod za potvrdu emaila",
        $"Vaš kod za potvrdu emaila je {code}.",
        EmailTokenPurpose.Registration);

    public static EmailRequested PasswordReset(User user, string code) => Create(user,
        "Torvin: kod za promjenu lozinke",
        $"Vaš kod za promjenu lozinke je {code}.",
        EmailTokenPurpose.PasswordReset);

    private static EmailRequested Create(User user, string subject, string codeLine, EmailTokenPurpose purpose)
    {
        var minutes = (int)EmailCodeService.LifetimeOf(purpose).TotalMinutes;
        var body = $"""
            Pozdrav {user.FirstName},

            {codeLine}
            Kod važi {minutes} minuta i može se iskoristiti samo jednom.

            Ako niste vi tražili ovaj kod, zanemarite poruku.

            Torvin
            """;
        return new EmailRequested(Guid.NewGuid(), user.Email, subject, body);
    }
}
