using Torvin.Model.Constants;
using Torvin.Model.Exceptions;
using Torvin.Services.StateMachines;

namespace Torvin.Tests;

public class CardStateMachineTests
{
    public static TheoryData<string, string, CardActor> Allowed => new()
    {
        { CardStatusCodes.Pending, CardStatusCodes.Active, CardActor.Owner },
        { CardStatusCodes.Active, CardStatusCodes.Blocked, CardActor.Owner },
        { CardStatusCodes.Blocked, CardStatusCodes.Active, CardActor.Owner },
        { CardStatusCodes.Active, CardStatusCodes.Lost, CardActor.Owner },
        { CardStatusCodes.Active, CardStatusCodes.Stolen, CardActor.Owner },
        { CardStatusCodes.Blocked, CardStatusCodes.Lost, CardActor.Owner },
        { CardStatusCodes.Blocked, CardStatusCodes.Stolen, CardActor.Owner },
        { CardStatusCodes.Active, CardStatusCodes.Blocked, CardActor.Admin },
        { CardStatusCodes.Blocked, CardStatusCodes.Active, CardActor.Admin },
        { CardStatusCodes.Active, CardStatusCodes.Lost, CardActor.Admin },
        { CardStatusCodes.Active, CardStatusCodes.Stolen, CardActor.Admin },
        { CardStatusCodes.Blocked, CardStatusCodes.Lost, CardActor.Admin },
        { CardStatusCodes.Blocked, CardStatusCodes.Stolen, CardActor.Admin },
        { CardStatusCodes.Active, CardStatusCodes.Closed, CardActor.Admin },
        { CardStatusCodes.Blocked, CardStatusCodes.Closed, CardActor.Admin },
        { CardStatusCodes.Lost, CardStatusCodes.Closed, CardActor.Admin },
        { CardStatusCodes.Stolen, CardStatusCodes.Closed, CardActor.Admin },
    };

    public static TheoryData<string, string, CardActor, string> Forbidden => new()
    {
        { CardStatusCodes.Stolen, CardStatusCodes.Active, CardActor.Owner, CardStateMachine.StolenCannotBeActivated },
        { CardStatusCodes.Stolen, CardStatusCodes.Active, CardActor.Admin, CardStateMachine.StolenCannotBeActivated },
        { CardStatusCodes.Lost, CardStatusCodes.Active, CardActor.Owner, CardStateMachine.LostCannotBeActivated },
        { CardStatusCodes.Closed, CardStatusCodes.Active, CardActor.Admin, CardStateMachine.ClosedIsFinal },
        { CardStatusCodes.Pending, CardStatusCodes.Blocked, CardActor.Owner, CardStateMachine.PendingNeedsCode },
        { CardStatusCodes.Pending, CardStatusCodes.Active, CardActor.Admin, CardStateMachine.PendingNeedsCode },
        { CardStatusCodes.Active, CardStatusCodes.Closed, CardActor.Owner, CardStateMachine.OnlyAdminCloses },
        { CardStatusCodes.Active, CardStatusCodes.Active, CardActor.Owner, CardStateMachine.SameStatus },
        { CardStatusCodes.Lost, CardStatusCodes.Stolen, CardActor.Admin, CardStateMachine.NotAllowed },
        { CardStatusCodes.Active, CardStatusCodes.Pending, CardActor.Admin, CardStateMachine.NotAllowed },
        { "Expired", CardStatusCodes.Active, CardActor.Admin, CardStateMachine.NotAllowed },
    };

    [Theory]
    [MemberData(nameof(Allowed))]
    public void AllowedTransition_Passes(string from, string to, CardActor actor)
    {
        Assert.True(CardStateMachine.CanChange(from, to, actor));
        CardStateMachine.EnsureCanChange(from, to, actor);
    }

    [Theory]
    [MemberData(nameof(Forbidden))]
    public void ForbiddenTransition_ThrowsWithReason(string from, string to, CardActor actor, string message)
    {
        var exception = Assert.Throws<BusinessException>(() => CardStateMachine.EnsureCanChange(from, to, actor));

        Assert.False(CardStateMachine.CanChange(from, to, actor));
        Assert.Equal(message, exception.Message);
    }

    // Iz konačnih statusa nema izlaza ni za vlasnika ni za admina, osim što admin zatvara izgubljenu i ukradenu
    [Theory]
    [InlineData(CardStatusCodes.Lost)]
    [InlineData(CardStatusCodes.Stolen)]
    [InlineData(CardStatusCodes.Closed)]
    public void FinalStatus_HasNoWayBack(string final)
    {
        foreach (var target in CardStatusCodes.All.Where(code => code != CardStatusCodes.Closed))
        {
            Assert.False(CardStateMachine.CanChange(final, target, CardActor.Owner));
            Assert.False(CardStateMachine.CanChange(final, target, CardActor.Admin));
        }
    }
}
