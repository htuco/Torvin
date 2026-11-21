using Microsoft.EntityFrameworkCore;
using Torvin.Model.Constants;
using Torvin.Model.Enums;
using Torvin.Model.Messages;
using Torvin.Services.Database;
using Torvin.Services.Database.Entities;
using static Torvin.Tests.NotificationHandlerFixture;

namespace Torvin.Tests;

public class CardStatusChangedHandlerTests
{
    private static async Task<(TorvinDbContext Context, User Owner, User Admin, Card Card)> NewAsync()
    {
        var context = await NewContextAsync();
        var owner = await TestUsers.AddAsync(context, "vlasnik", AppRoles.User);
        var admin = await TestUsers.AddAsync(context, "admin", AppRoles.Admin);
        var card = await TestCards.AddAsync(context, owner, 0m, last4: "4821");
        return (context, owner, admin, card);
    }

    private static CardStatusChanged Changed(Card card, string from, string to, User by, string? reason = null) =>
        new(Guid.NewGuid(), card.Id, from, to, by.Id, reason);

    private static async Task<Notification> SingleAsync(TorvinDbContext context) =>
        await context.Notifications.AsNoTracking().SingleAsync();

    [Fact]
    public async Task OwnerBlocksCard_GetsCardBlocked()
    {
        var (context, owner, _, card) = await NewAsync();

        Assert.True(await CardHandler(context).HandleAsync(
            Body(Changed(card, CardStatusCodes.Active, CardStatusCodes.Blocked, owner)), "id", default));

        var notification = await SingleAsync(context);
        Assert.Equal(NotificationType.CardBlocked, notification.Type);
        Assert.Equal(owner.Id, notification.UserId);
        Assert.Equal("Kartica •••• 4821 je blokirana.", notification.Message);
        Assert.Equal(card.Id, notification.RelatedEntityId);
    }

    [Fact]
    public async Task StolenCard_GetsStatusChanged()
    {
        var (context, owner, _, card) = await NewAsync();

        await CardHandler(context).HandleAsync(Body(Changed(card, CardStatusCodes.Active, CardStatusCodes.Stolen, owner)), "id", default);

        var notification = await SingleAsync(context);
        Assert.Equal(NotificationType.CardStatusChanged, notification.Type);
        Assert.Equal("Kartica prijavljena kao ukradena", notification.Title);
    }

    [Fact]
    public async Task OwnerUnblocksCard_IsAcknowledged_WithoutNotification()
    {
        var (context, owner, _, card) = await NewAsync();

        Assert.True(await CardHandler(context).HandleAsync(
            Body(Changed(card, CardStatusCodes.Blocked, CardStatusCodes.Active, owner)), "id", default));

        Assert.False(await context.Notifications.AnyAsync());
    }

    [Fact]
    public async Task AdminClosesCard_NotifiesOwnerWithStatusNameAndReason()
    {
        var (context, owner, admin, card) = await NewAsync();

        await CardHandler(context).HandleAsync(Body(Changed(card, CardStatusCodes.Stolen, CardStatusCodes.Closed, admin,
            "Zatvaranje poslije prijave krađe.")), "id", default);

        var notification = await SingleAsync(context);
        Assert.Equal(owner.Id, notification.UserId);
        Assert.Equal(NotificationType.CardStatusChanged, notification.Type);
        Assert.Equal("Administrator je promijenio status kartice •••• 4821 u \"Zatvorena\". Razlog: Zatvaranje poslije prijave krađe.",
            notification.Message);
    }

    [Fact]
    public async Task SameMessageTwice_WritesOnce()
    {
        var (context, owner, _, card) = await NewAsync();
        var body = Body(Changed(card, CardStatusCodes.Active, CardStatusCodes.Blocked, owner));
        var handler = CardHandler(context);

        await handler.HandleAsync(body, "id", default);
        await handler.HandleAsync(body, "id", default);

        Assert.Equal(1, await context.Notifications.CountAsync());
    }

    [Fact]
    public async Task MissingCard_GoesToDeadLetter()
    {
        var (context, owner, _, _) = await NewAsync();
        var message = new CardStatusChanged(Guid.NewGuid(), 9999, CardStatusCodes.Active, CardStatusCodes.Blocked, owner.Id, null);

        Assert.False(await CardHandler(context).HandleAsync(Body(message), "id", default));
    }
}
