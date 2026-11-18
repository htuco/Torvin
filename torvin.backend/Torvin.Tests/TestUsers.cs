using Microsoft.EntityFrameworkCore;
using Torvin.Services.Database;
using Torvin.Services.Database.Entities;
using Torvin.Services.Security;

namespace Torvin.Tests;

internal static class TestUsers
{
    public const string Password = "Lozinka.123";

    // Hash se računa jednom, jer PBKDF2 sa 600 000 iteracija traje
    private static readonly Lazy<string> PasswordHash = new(() => PasswordHasher.Hash(Password));

    public static async Task<User> AddAsync(TorvinDbContext context, string username, string role,
        bool isActive = true, bool emailConfirmed = true)
    {
        var user = new User
        {
            Username = username,
            Email = $"{username}@torvin.local",
            FirstName = "Test",
            LastName = username,
            Role = await context.Roles.SingleAsync(r => r.Name == role),
            IsActive = isActive,
            EmailConfirmed = emailConfirmed,
        };
        context.Users.Add(user);
        context.UserPasswords.Add(new UserPassword { User = user, PasswordHash = PasswordHash.Value });
        await context.SaveChangesAsync();
        return user;
    }
}
