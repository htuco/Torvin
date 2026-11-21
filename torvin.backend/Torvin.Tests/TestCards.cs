using Microsoft.EntityFrameworkCore;
using Torvin.Model.Constants;
using Torvin.Model.Enums;
using Torvin.Services.Database;
using Torvin.Services.Database.Entities;

namespace Torvin.Tests;

// Kartica upisana direktno u bazu, u traženom statusu, valuti i s balansom (za testove prenosa)
internal static class TestCards
{
    public static async Task<Card> AddAsync(TorvinDbContext context, User user, decimal balance,
        string statusCode = CardStatusCodes.Active, string currencyCode = CurrencyCodes.BAM, bool isDefault = false,
        string last4 = "4821")
    {
        var card = new Card
        {
            UserId = user.Id,
            CardTypeId = await context.CardTypes.Select(t => t.Id).FirstAsync(),
            CardStatusId = await context.CardStatuses.Where(s => s.Code == statusCode).Select(s => s.Id).SingleAsync(),
            CurrencyId = await context.Currencies.Where(c => c.Code == currencyCode).Select(c => c.Id).SingleAsync(),
            Network = CardNetwork.Visa,
            Last4Digits = last4,
            ExpiresAt = new DateOnly(2030, 12, 31),
            Balance = balance,
            IsDefault = isDefault,
            RowVersion = [1],
        };
        context.Cards.Add(card);
        await context.SaveChangesAsync();
        return card;
    }
}
