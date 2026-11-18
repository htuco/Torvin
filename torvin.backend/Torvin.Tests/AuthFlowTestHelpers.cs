using System.Text.RegularExpressions;
using Torvin.Model.Messages;
using Torvin.Services.Database;

namespace Torvin.Tests;

internal static partial class AuthFlowTestHelpers
{
    public static async Task<TorvinDbContext> NewContextAsync(Microsoft.EntityFrameworkCore.DbContextOptions<TorvinDbContext> options)
    {
        var context = new TorvinDbContext(options);
        await context.Database.EnsureCreatedAsync();
        return context;
    }

    // Kod iz tijela posljednjeg objavljenog maila, kao što bi ga korisnik pročitao
    public static string LastCode(FakeMessagePublisher publisher)
    {
        var email = Assert.IsType<EmailRequested>(publisher.Published[^1].Message);
        return CodePattern().Match(email.Body).Groups[1].Value;
    }

    [GeneratedRegex(@"je ([0-9]{6})\.")]
    private static partial Regex CodePattern();
}
