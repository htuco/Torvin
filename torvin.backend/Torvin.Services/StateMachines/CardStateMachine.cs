using Torvin.Model.Constants;
using Torvin.Model.Exceptions;

namespace Torvin.Services.StateMachines;

public enum CardActor
{
    Owner = 1,
    Admin = 2,
}

// Jedino mjesto koje odlučuje koji prelaz statusa kartice je dozvoljen i kome. Radi nad stalnim kodovima statusa,
// a pravila koja traže bazu (balans, ko je blokirao) provjerava servis poslije ove provjere
public static class CardStateMachine
{
    public const string SameStatus = "Kartica je već u tom statusu.";
    public const string StolenCannotBeActivated = "Kartica prijavljena kao ukradena ne može se ponovo aktivirati.";
    public const string LostCannotBeActivated = "Kartica prijavljena kao izgubljena ne može se ponovo aktivirati.";
    public const string ClosedIsFinal = "Zatvorena kartica ne može promijeniti status.";
    public const string PendingNeedsCode = "Kartica još nije aktivirana kodom iz emaila.";
    public const string OnlyAdminCloses = "Karticu može zatvoriti samo administrator.";
    public const string NotAllowed = "Ovaj prelaz statusa kartice nije dozvoljen.";

    // Lost, Stolen i Closed nemaju izlaz: kartica se ne vraća u upotrebu, a historija ostaje
    private static readonly HashSet<(string From, string To)> OwnerTransitions =
    [
        (CardStatusCodes.Pending, CardStatusCodes.Active),
        (CardStatusCodes.Active, CardStatusCodes.Blocked),
        (CardStatusCodes.Blocked, CardStatusCodes.Active),
        (CardStatusCodes.Active, CardStatusCodes.Lost),
        (CardStatusCodes.Active, CardStatusCodes.Stolen),
        (CardStatusCodes.Blocked, CardStatusCodes.Lost),
        (CardStatusCodes.Blocked, CardStatusCodes.Stolen),
    ];

    // Admin ne aktivira karticu na čekanju, jer to dokazuje samo vlasnik kodom iz svog emaila
    private static readonly HashSet<(string From, string To)> AdminTransitions =
    [
        (CardStatusCodes.Active, CardStatusCodes.Blocked),
        (CardStatusCodes.Blocked, CardStatusCodes.Active),
        (CardStatusCodes.Active, CardStatusCodes.Lost),
        (CardStatusCodes.Active, CardStatusCodes.Stolen),
        (CardStatusCodes.Blocked, CardStatusCodes.Lost),
        (CardStatusCodes.Blocked, CardStatusCodes.Stolen),
        (CardStatusCodes.Active, CardStatusCodes.Closed),
        (CardStatusCodes.Blocked, CardStatusCodes.Closed),
        (CardStatusCodes.Lost, CardStatusCodes.Closed),
        (CardStatusCodes.Stolen, CardStatusCodes.Closed),
    ];

    public static bool CanChange(string fromCode, string toCode, CardActor actor) =>
        (actor == CardActor.Admin ? AdminTransitions : OwnerTransitions).Contains((fromCode, toCode));

    public static void EnsureCanChange(string fromCode, string toCode, CardActor actor)
    {
        if (!CanChange(fromCode, toCode, actor))
        {
            throw new BusinessException(Reason(fromCode, toCode, actor));
        }
    }

    // Poruka kaže korisniku zašto prelaz nije moguć, a ne samo da nije
    private static string Reason(string fromCode, string toCode, CardActor actor) => (fromCode, toCode) switch
    {
        _ when fromCode == toCode => SameStatus,
        (CardStatusCodes.Stolen, CardStatusCodes.Active) => StolenCannotBeActivated,
        (CardStatusCodes.Lost, CardStatusCodes.Active) => LostCannotBeActivated,
        (CardStatusCodes.Closed, _) => ClosedIsFinal,
        (CardStatusCodes.Pending, _) => PendingNeedsCode,
        (_, CardStatusCodes.Closed) when actor == CardActor.Owner => OnlyAdminCloses,
        _ => NotAllowed,
    };
}
