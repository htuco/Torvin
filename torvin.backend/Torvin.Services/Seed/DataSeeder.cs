using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Torvin.Model.Constants;
using Torvin.Model.Enums;
using Torvin.Services.Database;
using Torvin.Services.Database.Entities;
using Torvin.Services.Security;

namespace Torvin.Services.Seed;

// Demo podaci s kojima se može testirati svaki ekran. Upisuju se jednom, na bazi bez korisnika, u jednoj
// DB transakciji (sve ili ništa). Datumi su relativni na trenutak seedanja, da "ovaj mjesec" i grafikoni
// zadnja tri mjeseca uvijek imaju podatke, a raspored je isti pri svakom seedanju (fiksan seed za Random)
public sealed class DataSeeder(TorvinDbContext context, ILogger<DataSeeder> logger)
{
    public const string DemoPassword = "test";
    internal const decimal MobileMainInitialBalance = 6000m;
    private const string BamCode = CurrencyCodes.BAM;

    private static readonly string[] CategoryNames = ["Hrana", "Prijevoz", "Režije", "Zabava", "Zdravlje", "Kupovina", "Ostalo"];

    private static readonly Dictionary<string, string[]> NotesByCategory = new()
    {
        ["Hrana"] = ["Ručak", "Pizza za ekipu", "Namirnice", "Večera"],
        ["Prijevoz"] = ["Gorivo", "Taksi", "Karta za autobus"],
        ["Režije"] = ["Struja", "Internet", "Dio računa za vodu"],
        ["Zabava"] = ["Kino", "Koncert", "Kuglanje"],
        ["Zdravlje"] = ["Apoteka", "Teretana", "Pregled"],
        ["Kupovina"] = ["Poklon", "Knjige", "Patike"],
        ["Ostalo"] = ["Vraćam dug", "Za sitnice", "Hvala"],
    };

    private readonly Random _random = new(230181);
    private readonly HashSet<string> _referenceNumbers = [];
    private readonly Dictionary<Card, decimal> _initialBalances = [];
    private readonly List<Transaction> _transactions = [];
    private readonly List<SavingsMovement> _movements = [];

    private DateTime _now;
    private DateTime _mobileCardBlockedAt;
    private DateTime _dinoBlockedAt;
    private Dictionary<string, Role> _roles = [];
    private Dictionary<string, CardType> _cardTypes = [];
    private Dictionary<string, CardStatus> _cardStatuses = [];
    private Dictionary<string, Currency> _currencies = [];
    private Dictionary<string, TransactionType> _transactionTypes = [];
    private Dictionary<string, TransactionStatus> _transactionStatuses = [];
    private Dictionary<string, TransactionCategory> _categories = [];

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (await context.Users.AnyAsync(cancellationToken))
        {
            logger.LogInformation("Demo podaci već postoje, seed se preskače.");
            return;
        }

        _now = DateTime.UtcNow;
        _mobileCardBlockedAt = DaysAgo(15);
        _dinoBlockedAt = DaysAgo(7);
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        await LoadReferenceDataAsync(cancellationToken);

        var users = CreateUsers();
        var cards = CreateCards(users);
        CreateTransfers(cards);
        var reversals = CreateReversalRequests(users, cards);
        var moneyRequests = CreateMoneyRequests(users, cards);
        var standingOrders = CreateStandingOrders(users, cards);
        var goals = CreateSavingsGoals(users, cards);
        var budgets = CreateBudgets(users, cards);
        var news = CreateNews(users);
        CreateFavoriteRecipients(users);
        SetBalances();
        await context.SaveChangesAsync(cancellationToken);

        // Audit i notifikacije pokazuju na ID zapisa, a ID-evi postoje tek poslije prvog snimanja
        CreateAuditLogs(users, cards, reversals);
        CreateNotifications(users, cards, reversals, moneyRequests, standingOrders, goals, budgets, news);
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        logger.LogInformation("Demo podaci su upisani: {Users} korisnika, {Cards} kartica, {Transactions} transakcija.",
            users.Count, _initialBalances.Count, _transactions.Count);
    }

    private async Task LoadReferenceDataAsync(CancellationToken cancellationToken)
    {
        _roles = await context.Roles.ToDictionaryAsync(r => r.Name, cancellationToken);
        _cardTypes = await context.CardTypes.ToDictionaryAsync(t => t.Name, cancellationToken);
        _cardStatuses = await context.CardStatuses.ToDictionaryAsync(s => s.Code, cancellationToken);
        _currencies = await context.Currencies.ToDictionaryAsync(c => c.Code.Trim(), cancellationToken);
        _transactionTypes = await context.TransactionTypes.ToDictionaryAsync(t => t.Code, cancellationToken);
        _transactionStatuses = await context.TransactionStatuses.ToDictionaryAsync(s => s.Code, cancellationToken);
        _categories = await context.TransactionCategories.ToDictionaryAsync(c => c.Name, cancellationToken);
    }

    // ---------- Korisnici i kartice

    private Dictionary<string, User> CreateUsers()
    {
        var users = new Dictionary<string, User>
        {
            ["desktop"] = NewUser("desktop", "Lejla", "Mujić", AppRoles.Admin, null, 150),
            ["mobile"] = NewUser("mobile", "Emir", "Kovačević", AppRoles.User, "avatar-1.jpg", 120),
            ["amina"] = NewUser("amina.hodzic", "Amina", "Hodžić", AppRoles.User, "avatar-2.jpg", 118),
            ["tarik"] = NewUser("tarik.begic", "Tarik", "Begić", AppRoles.User, "avatar-3.jpg", 115),
            ["selma"] = NewUser("selma.delic", "Selma", "Delić", AppRoles.User, null, 112),
            ["kenan"] = NewUser("kenan.ibrahimovic", "Kenan", "Ibrahimović", AppRoles.User, null, 110),
            ["ajla"] = NewUser("ajla.softic", "Ajla", "Softić", AppRoles.User, null, 108),
            ["dino"] = NewUser("dino.mehmedovic", "Dino", "Mehmedović", AppRoles.User, null, 105),
        };

        // Račun koji je administrator blokirao, za ekran blokiranja i aktiviranja
        users["dino"].IsActive = false;
        return users;
    }

    private User NewUser(string username, string firstName, string lastName, string role, string? avatar, int registeredDaysAgo)
    {
        var registeredAt = DaysAgo(registeredDaysAgo);
        var user = new User
        {
            Username = username,
            FirstName = firstName,
            LastName = lastName,
            Email = $"{username}@torvin.local",
            PhoneNumber = $"+387 61 {_random.Next(100, 1000)} {_random.Next(100, 1000)}",
            Role = _roles[role],
            EmailConfirmed = true,
            CreatedAt = registeredAt,
        };

        if (avatar is not null)
        {
            user.ProfileImage = LoadImage(avatar);
            user.ProfileImageContentType = "image/jpeg";
        }

        context.Users.Add(user);
        context.UserPasswords.Add(new UserPassword { User = user, PasswordHash = PasswordHasher.Hash(DemoPassword), CreatedAt = registeredAt });
        return user;
    }

    private Dictionary<string, Card> CreateCards(Dictionary<string, User> users)
    {
        return new Dictionary<string, Card>
        {
            ["mobile"] = NewCard(users["mobile"], "Debitna", CardStatusCodes.Active, BamCode, MobileMainInitialBalance, isDefault: true, "Glavna"),
            ["mobileEur"] = NewCard(users["mobile"], "Prepaid", CardStatusCodes.Active, CurrencyCodes.EUR, 400m, isDefault: false, "Putovanja"),
            ["mobileBlocked"] = NewCard(users["mobile"], "Poslovna", CardStatusCodes.Blocked, BamCode, 300m, isDefault: false, "Posao"),
            ["amina"] = NewCard(users["amina"], "Debitna", CardStatusCodes.Active, BamCode, 3000m, isDefault: true, null),
            ["aminaLost"] = NewCard(users["amina"], "Prepaid", CardStatusCodes.Lost, BamCode, 120m, isDefault: false, null),
            ["tarik"] = NewCard(users["tarik"], "Debitna", CardStatusCodes.Active, BamCode, 3000m, isDefault: true, null),
            ["tarikEur"] = NewCard(users["tarik"], "Poslovna", CardStatusCodes.Active, CurrencyCodes.EUR, 900m, isDefault: false, null),
            ["selma"] = NewCard(users["selma"], "Debitna", CardStatusCodes.Active, BamCode, 3000m, isDefault: true, null),
            ["selmaPending"] = NewCard(users["selma"], "Prepaid", CardStatusCodes.Pending, BamCode, 0m, isDefault: false, null),
            ["kenan"] = NewCard(users["kenan"], "Debitna", CardStatusCodes.Active, BamCode, 3000m, isDefault: true, null),
            ["kenanStolen"] = NewCard(users["kenan"], "Debitna", CardStatusCodes.Stolen, BamCode, 80m, isDefault: false, null),
            ["ajla"] = NewCard(users["ajla"], "Prepaid", CardStatusCodes.Active, BamCode, 3000m, isDefault: true, null),
            ["ajlaClosed"] = NewCard(users["ajla"], "Debitna", CardStatusCodes.Closed, BamCode, 0m, isDefault: false, null),
            ["dino"] = NewCard(users["dino"], "Debitna", CardStatusCodes.Blocked, BamCode, 350m, isDefault: true, null),
        };
    }

    private Card NewCard(User user, string type, string statusCode, string currencyCode, decimal initialBalance, bool isDefault, string? name)
    {
        var card = new Card
        {
            User = user,
            CardType = _cardTypes[type],
            CardStatus = _cardStatuses[statusCode],
            Currency = _currencies[currencyCode],
            Name = name,
            Last4Digits = _random.Next(0, 10_000).ToString("D4", CultureInfo.InvariantCulture),
            ExpiresAt = DateOnly.FromDateTime(_now).AddYears(_random.Next(1, 5)),
            IsDefault = isDefault,
            CreatedAt = user.CreatedAt.AddDays(1),
        };

        context.Cards.Add(card);
        _initialBalances[card] = initialBalance;
        return card;
    }

    // ---------- Transakcije

    private void CreateTransfers(Dictionary<string, Card> cards)
    {
        var mobile = cards["mobile"];
        Card[] others = [cards["amina"], cards["tarik"], cards["selma"], cards["kenan"], cards["ajla"]];

        // Dvije trećine transfera uključuje korisnika mobile, da njegova historija i dashboard budu puni
        for (var i = 0; i < 64; i++)
        {
            Card sender;
            Card receiver;
            if (i % 3 != 2)
            {
                var other = others[_random.Next(others.Length)];
                (sender, receiver) = _random.Next(2) == 0 ? (mobile, other) : (other, mobile);
            }
            else
            {
                sender = others[_random.Next(others.Length)];
                do
                {
                    receiver = others[_random.Next(others.Length)];
                } while (receiver == sender);
            }

            var category = CategoryNames[i % CategoryNames.Length];
            AddTransaction(sender, receiver, Money(5, 180), TransactionTypeCodes.Transfer, DaysAgo(_random.Next(3, 100)), category);
        }

        // Potrošnja u tekućem mjesecu, iz koje se računaju budžeti (jedan blizu limita, jedan prekoračen)
        (string Category, Card Receiver)[] thisMonth =
        [
            ("Hrana", others[0]), ("Hrana", others[1]), ("Hrana", others[2]), ("Hrana", others[3]),
            ("Prijevoz", others[1]), ("Prijevoz", others[4]), ("Zabava", others[2]), ("Zabava", others[0]),
            ("Režije", others[3]),
        ];
        foreach (var (category, receiver) in thisMonth)
        {
            AddTransaction(mobile, receiver, Money(15, 90), TransactionTypeCodes.Transfer, ThisMonth(), category);
        }

        // Današnje transakcije, da dashboard ima dnevni broj i promet i na dan pokretanja
        AddTransaction(mobile, others[0], Money(10, 40), TransactionTypeCodes.Transfer, Today(), "Hrana");
        AddTransaction(others[1], mobile, Money(20, 60), TransactionTypeCodes.Transfer, Today(), "Ostalo");
        AddTransaction(others[2], others[3], Money(15, 50), TransactionTypeCodes.Transfer, Today(), "Prijevoz");

        // Neuspjele transakcije ne mijenjaju balans, ali postoje za filter po statusu
        AddTransaction(mobile, others[4], 250m, TransactionTypeCodes.Transfer, DaysAgo(45), "Kupovina",
            "Nova jakna", TransactionStatusCodes.Failed);
        AddTransaction(others[3], others[2], 90m, TransactionTypeCodes.Transfer, DaysAgo(22), "Zabava",
            null, TransactionStatusCodes.Failed);

        // Transferi u eurima idu samo između EUR kartica, jer se plaća u valuti kartice pošiljaoca
        AddTransaction(cards["mobileEur"], cards["tarikEur"], 60m, TransactionTypeCodes.Transfer, DaysAgo(40), "Kupovina");
        AddTransaction(cards["tarikEur"], cards["mobileEur"], 85.5m, TransactionTypeCodes.Transfer, DaysAgo(25), "Ostalo");
        AddTransaction(cards["mobileEur"], cards["tarikEur"], 32m, TransactionTypeCodes.Transfer, DaysAgo(6), "Hrana");
    }

    private Transaction AddTransaction(Card sender, Card receiver, decimal amount, string typeCode, DateTime at,
        string? category, string? note = null, string statusCode = TransactionStatusCodes.Completed)
    {
        var transaction = new Transaction
        {
            ReferenceNumber = NewReferenceNumber(at),
            SenderUser = sender.User,
            ReceiverUser = receiver.User,
            SenderCard = sender,
            ReceiverCard = receiver,
            Amount = amount,
            Currency = sender.Currency,
            TransactionType = _transactionTypes[typeCode],
            TransactionStatus = _transactionStatuses[statusCode],
            TransactionCategory = category is null ? null : _categories[category],
            Note = note ?? (category is null ? null : Pick(NotesByCategory[category])),
            CreatedAt = at,
        };

        context.Transactions.Add(transaction);
        _transactions.Add(transaction);
        return transaction;
    }

    // ---------- Poništenja i zahtjevi za novac

    private List<ReversalRequest> CreateReversalRequests(Dictionary<string, User> users, Dictionary<string, Card> cards)
    {
        // Samo transakcije starije od tri dana, da zahtjev i odluka o njemu ne padnu u budućnost
        var mobileSent = _transactions
            .Where(t => t.SenderCard == cards["mobile"] && t.TransactionType.Code == TransactionTypeCodes.Transfer
                && t.TransactionStatus.Code == TransactionStatusCodes.Completed && t.CreatedAt < _now.AddDays(-3))
            .OrderBy(t => t.CreatedAt)
            .ToList();
        var admin = users["desktop"];
        var requests = new List<ReversalRequest>();

        // Najstarije transakcije su odobrene (s korektivnom transakcijom), najnovije još čekaju odluku
        foreach (var original in mobileSent.Take(2))
        {
            original.TransactionStatus = _transactionStatuses[TransactionStatusCodes.Reversed];
            var decidedAt = original.CreatedAt.AddDays(2);
            var correction = AddTransaction(original.ReceiverCard!, original.SenderCard!, original.Amount,
                TransactionTypeCodes.Correction, decidedAt, null, $"Povrat za {original.ReferenceNumber}");
            correction.OriginalTransaction = original;

            requests.Add(NewReversal(original, users["mobile"], "Greškom sam poslao novac pogrešnoj osobi.",
                ReversalRequestStatus.Approved, admin, null, correction));
        }

        requests.Add(NewReversal(mobileSent[2], users["mobile"], "Iznos je dvaput naplaćen za istu stvar.",
            ReversalRequestStatus.Rejected, admin,
            "Primalac je potvrdio prijem i radi se o dvije odvojene kupovine, pa transakcija ostaje.", null));

        foreach (var original in mobileSent.TakeLast(2))
        {
            requests.Add(NewReversal(original, users["mobile"], "Usluga nije izvršena, molim povrat novca.",
                ReversalRequestStatus.Pending, null, null, null));
        }

        return requests;
    }

    private ReversalRequest NewReversal(Transaction transaction, User requestedBy, string reason, ReversalRequestStatus status,
        User? decidedBy, string? decisionReason, Transaction? correction)
    {
        var requestedAt = transaction.CreatedAt.AddDays(1);
        var request = new ReversalRequest
        {
            Transaction = transaction,
            RequestedByUser = requestedBy,
            Reason = reason,
            Status = status,
            DecidedByUser = decidedBy,
            DecisionReason = decisionReason,
            DecidedAt = decidedBy is null ? null : transaction.CreatedAt.AddDays(2),
            CorrectiveTransaction = correction,
            CreatedAt = requestedAt,
        };

        context.ReversalRequests.Add(request);
        return request;
    }

    private List<MoneyRequest> CreateMoneyRequests(Dictionary<string, User> users, Dictionary<string, Card> cards)
    {
        var requests = new List<MoneyRequest>
        {
            NewMoneyRequest(users["amina"], users["mobile"], 45m, "Pola računa za večeru", MoneyRequestStatus.Paid, 30),
            NewMoneyRequest(users["mobile"], users["tarik"], 120m, "Karte za koncert", MoneyRequestStatus.Paid, 18),
            NewMoneyRequest(users["tarik"], users["mobile"], 25m, "Kafa i doručak", MoneyRequestStatus.Accepted, 4),
            NewMoneyRequest(users["ajla"], users["mobile"], 200m, "Pozajmica do plate", MoneyRequestStatus.Rejected, 12),
            NewMoneyRequest(users["selma"], users["mobile"], 30m, "Poklon za Ajlu", MoneyRequestStatus.Pending, 2),
            NewMoneyRequest(users["mobile"], users["kenan"], 60m, "Gorivo za put", MoneyRequestStatus.Pending, 1),
        };

        // Plaćen zahtjev je transfer od onoga od koga se traži prema onome ko traži
        foreach (var request in requests.Where(r => r.Status == MoneyRequestStatus.Paid))
        {
            var payerCard = DefaultCard(cards, request.PayerUser);
            var requesterCard = DefaultCard(cards, request.RequesterUser);
            request.Transaction = AddTransaction(payerCard, requesterCard, request.Amount, TransactionTypeCodes.MoneyRequest,
                request.RespondedAt!.Value, "Ostalo", request.Note);
        }

        return requests;
    }

    private MoneyRequest NewMoneyRequest(User requester, User payer, decimal amount, string note, MoneyRequestStatus status, int daysAgo)
    {
        var createdAt = DaysAgo(daysAgo);
        var request = new MoneyRequest
        {
            RequesterUser = requester,
            PayerUser = payer,
            Amount = amount,
            Currency = _currencies[BamCode],
            Note = note,
            Status = status,
            RespondedAt = status == MoneyRequestStatus.Pending ? null : createdAt.AddHours(3),
            CreatedAt = createdAt,
        };

        context.MoneyRequests.Add(request);
        return request;
    }

    // ---------- Trajni nalozi

    private List<StandingOrder> CreateStandingOrders(Dictionary<string, User> users, Dictionary<string, Card> cards)
    {
        var today = DateOnly.FromDateTime(_now);

        // Mjesečni: tri izvršenja iza nas, sljedeće za dva dana
        var monthlyStart = today.AddMonths(-3).AddDays(2);
        var monthly = NewStandingOrder(users["mobile"], cards["mobile"], users["selma"], 150m, "Kirija (moj dio)",
            StandingOrderFrequency.Monthly, StandingOrderStatus.Active, monthlyStart, At(monthlyStart.AddMonths(3)));
        for (var month = 0; month < 3; month++)
        {
            AddExecution(monthly, cards, At(monthlyStart.AddMonths(month)), attempt: 1, succeeded: true, "Režije");
        }

        // Sedmični: jedno izvršenje prvi put ne uspije, pa uspije u drugom pokušaju
        var weeklyStart = today.AddDays(-33);
        var weekly = NewStandingOrder(users["mobile"], cards["mobile"], users["tarik"], 20m, "Teretana",
            StandingOrderFrequency.Weekly, StandingOrderStatus.Active, weeklyStart, At(weeklyStart.AddDays(35)));
        for (var week = 0; week < 5; week++)
        {
            var scheduledFor = At(weeklyStart.AddDays(7 * week));
            if (week == 2)
            {
                AddExecution(weekly, cards, scheduledFor, attempt: 1, succeeded: false, "Zdravlje");
                AddExecution(weekly, cards, scheduledFor, attempt: 2, succeeded: true, "Zdravlje");
            }
            else
            {
                AddExecution(weekly, cards, scheduledFor, attempt: 1, succeeded: true, "Zdravlje");
            }
        }

        var pausedStart = today.AddMonths(-2);
        var paused = NewStandingOrder(users["mobile"], cards["mobile"], users["amina"], 50m, "Članarina u klubu",
            StandingOrderFrequency.Monthly, StandingOrderStatus.Paused, pausedStart, null);
        AddExecution(paused, cards, At(pausedStart), attempt: 1, succeeded: true, "Zabava");

        var cancelled = NewStandingOrder(users["tarik"], cards["tarik"], users["mobile"], 100m, "Zajednički račun",
            StandingOrderFrequency.Monthly, StandingOrderStatus.Cancelled, today.AddMonths(-1), null);

        var onceDate = today.AddDays(-20);
        var once = NewStandingOrder(users["kenan"], cards["kenan"], users["mobile"], 75m, "Vraćanje duga",
            StandingOrderFrequency.Once, StandingOrderStatus.Completed, onceDate, null);
        once.EndDate = onceDate;
        AddExecution(once, cards, At(onceDate), attempt: 1, succeeded: true, "Ostalo");

        return [monthly, weekly, paused, cancelled, once];
    }

    private StandingOrder NewStandingOrder(User owner, Card sourceCard, User recipient, decimal amount, string note,
        StandingOrderFrequency frequency, StandingOrderStatus status, DateOnly startDate, DateTime? nextExecutionAt)
    {
        var order = new StandingOrder
        {
            User = owner,
            SourceCard = sourceCard,
            RecipientUser = recipient,
            Amount = amount,
            Note = note,
            Frequency = frequency,
            Status = status,
            StartDate = startDate,
            NextExecutionAt = nextExecutionAt,
            CreatedAt = At(startDate).AddDays(-3),
        };

        context.StandingOrders.Add(order);
        return order;
    }

    private void AddExecution(StandingOrder order, Dictionary<string, Card> cards, DateTime scheduledFor, int attempt,
        bool succeeded, string category)
    {
        // Ponovni pokušaj ide sat poslije neuspjelog
        var executedAt = scheduledFor.AddHours(attempt - 1).AddMinutes(1);
        var execution = new StandingOrderExecution
        {
            StandingOrder = order,
            ScheduledFor = scheduledFor,
            AttemptNumber = attempt,
            Status = succeeded ? StandingOrderExecutionStatus.Succeeded : StandingOrderExecutionStatus.Failed,
            FailureReason = succeeded ? null : "Nedovoljno sredstava na kartici.",
            CreatedAt = executedAt,
        };

        if (succeeded)
        {
            execution.Transaction = AddTransaction(order.SourceCard, DefaultCard(cards, order.RecipientUser), order.Amount,
                TransactionTypeCodes.StandingOrder, executedAt, category, order.Note);
        }

        context.StandingOrderExecutions.Add(execution);
    }

    // ---------- Štednja i budžeti

    private List<SavingsGoal> CreateSavingsGoals(Dictionary<string, User> users, Dictionary<string, Card> cards)
    {
        var reserve = NewGoal(users["mobile"], "Rezervni fond", 1000m, null, 90);
        AddMovement(reserve, cards["mobile"], SavingsMovementType.Deposit, 400m, 80);
        AddMovement(reserve, cards["mobile"], SavingsMovementType.Deposit, 350m, 50);
        var lastDeposit = AddMovement(reserve, cards["mobile"], SavingsMovementType.Deposit, 250m, 20);
        reserve.AchievedAt = lastDeposit.CreatedAt;

        var vacation = NewGoal(users["mobile"], "Ljetovanje", 1500m, DateOnly.FromDateTime(_now).AddMonths(5), 65);
        AddMovement(vacation, cards["mobile"], SavingsMovementType.Deposit, 200m, 60);
        AddMovement(vacation, cards["mobile"], SavingsMovementType.Deposit, 300m, 30);
        AddMovement(vacation, cards["mobile"], SavingsMovementType.Withdrawal, 100m, 10);

        var laptop = NewGoal(users["mobile"], "Novi laptop", 2000m, null, 45);
        AddMovement(laptop, cards["mobile"], SavingsMovementType.Deposit, 150m, 40);
        AddMovement(laptop, cards["mobile"], SavingsMovementType.Deposit, 150m, 5);

        var car = NewGoal(users["amina"], "Auto", 8000m, DateOnly.FromDateTime(_now).AddYears(1), 75);
        AddMovement(car, cards["amina"], SavingsMovementType.Deposit, 500m, 70);
        AddMovement(car, cards["amina"], SavingsMovementType.Deposit, 300m, 15);

        return [reserve, vacation, laptop, car];
    }

    private SavingsGoal NewGoal(User user, string name, decimal target, DateOnly? deadline, int createdDaysAgo)
    {
        var goal = new SavingsGoal
        {
            User = user,
            Name = name,
            TargetAmount = target,
            Currency = _currencies[BamCode],
            Deadline = deadline,
            CreatedAt = DaysAgo(createdDaysAgo),
        };

        context.SavingsGoals.Add(goal);
        return goal;
    }

    private SavingsMovement AddMovement(SavingsGoal goal, Card card, SavingsMovementType type, decimal amount, int daysAgo)
    {
        var movement = new SavingsMovement { SavingsGoal = goal, Card = card, Type = type, Amount = amount, CreatedAt = DaysAgo(daysAgo) };
        goal.CurrentAmount += type == SavingsMovementType.Deposit ? amount : -amount;

        context.SavingsMovements.Add(movement);
        _movements.Add(movement);
        return movement;
    }

    private Dictionary<string, Budget> CreateBudgets(Dictionary<string, User> users, Dictionary<string, Card> cards)
    {
        var mobile = users["mobile"];
        var monthStart = new DateTime(_now.Year, _now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        decimal Spent(string category) => _transactions
            .Where(t => t.SenderCard == cards["mobile"] && t.CreatedAt >= monthStart && t.TransactionCategory?.Name == category
                && t.TransactionStatus.Code == TransactionStatusCodes.Completed)
            .Sum(t => t.Amount);

        // Tekući mjesec: Hrana oko 85 % limita, Zabava prekoračena, ostalo s rezervom
        var current = new Dictionary<string, Budget>
        {
            ["Hrana"] = AddBudget(mobile, "Hrana", _now.Year, _now.Month, Math.Ceiling(Spent("Hrana") / 0.85m)),
            ["Zabava"] = AddBudget(mobile, "Zabava", _now.Year, _now.Month, Math.Floor(Spent("Zabava") * 0.8m)),
            ["Prijevoz"] = AddBudget(mobile, "Prijevoz", _now.Year, _now.Month, Math.Ceiling(Spent("Prijevoz") * 1.5m) + 50m),
            ["Režije"] = AddBudget(mobile, "Režije", _now.Year, _now.Month, Math.Ceiling(Spent("Režije")) + 150m),
        };

        for (var monthsBack = 1; monthsBack <= 2; monthsBack++)
        {
            var month = monthStart.AddMonths(-monthsBack);
            AddBudget(mobile, "Hrana", month.Year, month.Month, 400m);
            AddBudget(mobile, "Zabava", month.Year, month.Month, 150m);
        }

        return current;
    }

    private Budget AddBudget(User user, string category, int year, int month, decimal limit)
    {
        var budget = new Budget
        {
            User = user,
            TransactionCategory = _categories[category],
            Currency = _currencies[BamCode],
            Year = year,
            Month = month,
            LimitAmount = limit,
            // Ponoć prvog dana, pa ni budžet tekućeg mjeseca nije u budućnosti
            CreatedAt = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc),
        };

        context.Budgets.Add(budget);
        return budget;
    }

    // ---------- Obavijesti, omiljeni primaoci, audit, notifikacije

    private List<News> CreateNews(Dictionary<string, User> users)
    {
        (string Title, string Content, string Image, int DaysAgo)[] items =
        [
            ("Dobrodošli u Torvin",
                "Torvin je vaš digitalni novčanik: virtuelne kartice, transferi između korisnika, zahtjevi za novac, štednja i budžet na jednom mjestu. Sve promjene balansa vidite odmah, a za svaku transakciju dobijate notifikaciju.",
                "news-1.jpg", 80),
            ("Nova funkcija: trajni nalozi",
                "Ponavljajuće uplate više ne morate pamtiti. Zakažite jednokratni budući ili sedmični i mjesečni transfer, a nalog možete pauzirati ili otkazati u bilo kojem trenutku. O svakom izvršenju dobijate obavještenje.",
                "news-2.jpg", 55),
            ("Sigurnost vaše kartice",
                "Ako izgubite karticu ili posumnjate na zloupotrebu, blokirajte je u aplikaciji jednim dodirom i prijavite kao izgubljenu ili ukradenu. Torvin nikad ne traži vašu lozinku ni kod iz emaila putem poruke.",
                "news-3.jpg", 35),
            ("Planirano održavanje",
                "U noći sa subote na nedjelju između 02:00 i 03:00 sistem će biti kratko nedostupan zbog održavanja. Zakazani trajni nalozi izvršiće se odmah nakon završetka radova.",
                "news-4.jpg", 20),
            ("Štednja s ciljem",
                "Postavite cilj, na primjer ljetovanje ili novi laptop, odredite iznos i rok, a Torvin vam pokazuje koliko vam još nedostaje. Uplate i povlačenja su odmah vidljivi u napretku cilja.",
                "news-5.jpg", 9),
            ("Mjesečni budžet",
                "Odredite limit potrošnje po kategoriji i pratite koliko ste potrošili ovog mjeseca. Kad se približite limitu ili ga pređete, dobićete upozorenje.",
                "news-6.jpg", 2),
        ];

        var news = new List<News>();
        foreach (var (title, content, image, daysAgo) in items)
        {
            var publishedAt = DaysAgo(daysAgo);
            var item = new News
            {
                AuthorUser = users["desktop"],
                Title = title,
                Content = content,
                Image = LoadImage(image),
                ImageContentType = "image/jpeg",
                PublishedAt = publishedAt,
                CreatedAt = publishedAt,
            };
            context.News.Add(item);
            news.Add(item);
        }

        return news;
    }

    private void CreateFavoriteRecipients(Dictionary<string, User> users)
    {
        (string Recipient, string Nickname, int DaysAgo)[] favorites = [("amina", "Amina", 90), ("tarik", "Tarik (teretana)", 60), ("selma", "Cimerka", 95)];
        foreach (var (recipient, nickname, daysAgo) in favorites)
        {
            context.FavoriteRecipients.Add(new FavoriteRecipient
            {
                User = users["mobile"],
                RecipientUser = users[recipient],
                Nickname = nickname,
                CreatedAt = DaysAgo(daysAgo),
            });
        }
    }

    private void CreateAuditLogs(Dictionary<string, User> users, Dictionary<string, Card> cards, List<ReversalRequest> reversals)
    {
        var admin = users["desktop"];
        AddCardBlockedAudit(users["mobile"], cards["mobileBlocked"], "Korisnik je blokirao karticu.", _mobileCardBlockedAt);
        AddCardBlockedAudit(admin, cards["dino"], "Administrator je blokirao karticu uz blokadu računa.", _dinoBlockedAt);

        context.AuditLogs.Add(new AuditLog
        {
            ActorUser = admin,
            Action = AuditActions.UserDeactivated,
            EntityType = nameof(User),
            EntityId = users["dino"].Id,
            OldValues = """{"IsActive":true}""",
            NewValues = """{"IsActive":false}""",
            Description = "Račun blokiran zbog sumnjive aktivnosti.",
            CreatedAt = _dinoBlockedAt,
        });

        foreach (var reversal in reversals.Where(r => r.Status != ReversalRequestStatus.Pending))
        {
            var approved = reversal.Status == ReversalRequestStatus.Approved;
            context.AuditLogs.Add(new AuditLog
            {
                ActorUser = admin,
                Action = approved ? AuditActions.ReversalApproved : AuditActions.ReversalRejected,
                EntityType = nameof(ReversalRequest),
                EntityId = reversal.Id,
                OldValues = $$"""{"Status":"{{ReversalRequestStatus.Pending}}"}""",
                NewValues = $$"""{"Status":"{{reversal.Status}}"}""",
                Description = reversal.DecisionReason ?? "Odobren povrat, napravljena korektivna transakcija.",
                CreatedAt = reversal.DecidedAt!.Value,
            });
        }
    }

    private void AddCardBlockedAudit(User actor, Card card, string description, DateTime at)
    {
        context.AuditLogs.Add(new AuditLog
        {
            ActorUser = actor,
            Action = AuditActions.CardBlocked,
            EntityType = nameof(Card),
            EntityId = card.Id,
            OldValues = $$"""{"Status":"{{CardStatusCodes.Active}}"}""",
            NewValues = $$"""{"Status":"{{CardStatusCodes.Blocked}}"}""",
            Description = description,
            CreatedAt = at,
        });
    }

    // Notifikacija pokazuje na zapis preko RelatedEntityId, a vrsta zapisa slijedi iz tipa notifikacije
    private void CreateNotifications(Dictionary<string, User> users, Dictionary<string, Card> cards,
        List<ReversalRequest> reversals, List<MoneyRequest> moneyRequests, List<StandingOrder> standingOrders,
        List<SavingsGoal> goals, Dictionary<string, Budget> budgets, List<News> news)
    {
        var mobile = users["mobile"];

        var received = _transactions
            .Where(t => t.ReceiverUser == mobile && t.TransactionType.Code == TransactionTypeCodes.Transfer
                && t.TransactionStatus.Code == TransactionStatusCodes.Completed)
            .OrderByDescending(t => t.CreatedAt)
            .Take(5);
        var isRead = false;
        foreach (var transaction in received)
        {
            Notify(mobile, NotificationType.TransactionReceived, "Primljena uplata",
                $"{FullName(transaction.SenderUser)} vam je poslao/la {Amount(transaction)}.", transaction.CreatedAt, isRead, transaction.Id);
            isRead = true;
        }

        foreach (var reversal in reversals.Where(r => r.Status != ReversalRequestStatus.Pending))
        {
            var approved = reversal.Status == ReversalRequestStatus.Approved;
            Notify(mobile, NotificationType.TransactionReversed,
                approved ? "Poništenje odobreno" : "Poništenje odbijeno",
                approved
                    ? $"Transakcija {reversal.Transaction.ReferenceNumber} je poništena i {Amount(reversal.Transaction)} je vraćeno na vašu karticu."
                    : $"Zahtjev za poništenje transakcije {reversal.Transaction.ReferenceNumber} je odbijen: {reversal.DecisionReason}",
                reversal.DecidedAt!.Value, isRead: true, reversal.Id);
        }

        foreach (var request in moneyRequests.Where(r => r.Status == MoneyRequestStatus.Pending))
        {
            Notify(request.PayerUser, NotificationType.MoneyRequest, "Novi zahtjev za novac",
                $"{FullName(request.RequesterUser)} traži {Km(request.Amount)}: {request.Note}", request.CreatedAt, isRead: false, request.Id);
        }

        var accepted = moneyRequests.First(r => r.Status == MoneyRequestStatus.Accepted);
        Notify(accepted.RequesterUser, NotificationType.MoneyRequest, "Zahtjev prihvaćen",
            $"{FullName(accepted.PayerUser)} je prihvatio/la vaš zahtjev za {Km(accepted.Amount)}.", accepted.RespondedAt!.Value,
            isRead: false, accepted.Id);
        var rejected = moneyRequests.First(r => r.Status == MoneyRequestStatus.Rejected);
        Notify(rejected.RequesterUser, NotificationType.MoneyRequest, "Zahtjev odbijen",
            $"{FullName(rejected.PayerUser)} je odbio/la vaš zahtjev za {Km(rejected.Amount)}.", rejected.RespondedAt!.Value,
            isRead: true, rejected.Id);

        var monthly = standingOrders[0];
        var lastMonthly = monthly.Executions.MaxBy(e => e.ScheduledFor)!;
        Notify(mobile, NotificationType.StandingOrderExecuted, "Trajni nalog izvršen",
            $"Nalog \"{monthly.Note}\" je izvršen: {Km(monthly.Amount)} za {FullName(monthly.RecipientUser)}.",
            lastMonthly.CreatedAt, isRead: true, monthly.Id);
        var weekly = standingOrders[1];
        var failed = weekly.Executions.First(e => e.Status == StandingOrderExecutionStatus.Failed);
        Notify(mobile, NotificationType.StandingOrderExecuted, "Trajni nalog nije izvršen",
            $"Nalog \"{weekly.Note}\" nije izvršen: {failed.FailureReason} Ponovni pokušaj je za sat vremena.",
            failed.CreatedAt, isRead: true, weekly.Id);

        Notify(mobile, NotificationType.BudgetWarning, "Budžet skoro potrošen",
            "Potrošili ste oko 85 % budžeta za kategoriju Hrana ovog mjeseca.", ThisMonth(), isRead: false, budgets["Hrana"].Id);
        Notify(mobile, NotificationType.BudgetWarning, "Budžet prekoračen",
            "Prešli ste mjesečni budžet za kategoriju Zabava.", ThisMonth(), isRead: false, budgets["Zabava"].Id);

        var achieved = goals.First(g => g.AchievedAt is not null);
        Notify(mobile, NotificationType.SavingsGoalReached, "Cilj dostignut",
            $"Čestitamo! Dostigli ste cilj \"{achieved.Name}\" od {Km(achieved.TargetAmount)}.", achieved.AchievedAt!.Value,
            isRead: true, achieved.Id);

        Notify(mobile, NotificationType.CardBlocked, "Kartica blokirana",
            $"Kartica •••• {cards["mobileBlocked"].Last4Digits} je blokirana.", _mobileCardBlockedAt, isRead: true, cards["mobileBlocked"].Id);
        Notify(users["dino"], NotificationType.CardBlocked, "Kartica blokirana",
            $"Administrator je blokirao vašu karticu •••• {cards["dino"].Last4Digits}.", _dinoBlockedAt, isRead: false, cards["dino"].Id);

        foreach (var item in news.TakeLast(2))
        {
            Notify(mobile, NotificationType.System, "Nova obavijest", item.Title, item.PublishedAt, isRead: false, item.Id);
        }
    }

    private void Notify(User user, NotificationType type, string title, string message, DateTime at, bool isRead, int relatedEntityId)
    {
        context.Notifications.Add(new Notification
        {
            User = user,
            Type = type,
            Title = title,
            Message = message,
            IsRead = isRead,
            ReadAt = isRead ? at.AddHours(1) : null,
            RelatedEntityId = relatedEntityId,
            CreatedAt = at,
        });
    }

    // ---------- Balansi

    // Balans je početni iznos plus primljeno minus poslano, plus i minus štednja. Neuspjela transakcija ne
    // mijenja balans. Tako balans uvijek odgovara historiji, a seed puca umjesto da upiše negativan balans
    private void SetBalances()
    {
        foreach (var (card, initial) in _initialBalances)
        {
            card.Balance = initial;
        }

        foreach (var transaction in _transactions.Where(t => t.TransactionStatus.Code != TransactionStatusCodes.Failed))
        {
            transaction.SenderCard!.Balance -= transaction.Amount;
            transaction.ReceiverCard!.Balance += transaction.Amount;
        }

        foreach (var movement in _movements)
        {
            movement.Card.Balance += movement.Type == SavingsMovementType.Deposit ? -movement.Amount : movement.Amount;
        }

        var negative = _initialBalances.Keys.FirstOrDefault(c => c.Balance < 0);
        if (negative is not null)
        {
            throw new InvalidOperationException($"Seed bi ostavio negativan balans na kartici •••• {negative.Last4Digits}.");
        }
    }

    // ---------- Pomoćne metode

    private static Card DefaultCard(Dictionary<string, Card> cards, User user) =>
        cards.Values.Single(c => c.User == user && c.IsDefault);

    private DateTime DaysAgo(int days) =>
        _now.Date.AddDays(-days).AddHours(_random.Next(7, 22)).AddMinutes(_random.Next(60));

    // Trenutak između početka mjeseca i sada, da potrošnja "ovog mjeseca" postoji i prvog dana u mjesecu
    private DateTime ThisMonth()
    {
        var monthStart = new DateTime(_now.Year, _now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        return monthStart.AddTicks((long)((_now - monthStart).Ticks * _random.NextDouble()));
    }

    // Trenutak između ponoći i sada
    private DateTime Today() => _now.Date.AddTicks((long)((_now - _now.Date).Ticks * _random.NextDouble()));

    private static DateTime At(DateOnly date) => date.ToDateTime(new TimeOnly(8, 0), DateTimeKind.Utc);

    private decimal Money(int min, int max) => Math.Round((decimal)(min + _random.NextDouble() * (max - min)), 2);

    private string Pick(string[] values) => values[_random.Next(values.Length)];

    private string NewReferenceNumber(DateTime at)
    {
        string number;
        do
        {
            number = $"TRV{at:yyyyMMdd}{_random.Next(0, 1_000_000):D6}";
        } while (!_referenceNumbers.Add(number));

        return number;
    }

    private static string FullName(User user) => $"{user.FirstName} {user.LastName}";

    private static string Amount(Transaction transaction) => Format(transaction.Amount, transaction.Currency.Symbol);

    private static string Km(decimal amount) => Format(amount, "KM");

    // 1234.5 → "1 234,50 KM", bez zavisnosti od kulture servera
    private static string Format(decimal amount, string symbol) =>
        $"{amount.ToString("N2", CultureInfo.InvariantCulture).Replace(",", " ").Replace(".", ",")} {symbol}";

    private static byte[] LoadImage(string fileName)
    {
        using var stream = typeof(DataSeeder).Assembly.GetManifestResourceStream($"SeedImages.{fileName}")
            ?? throw new InvalidOperationException($"Seed slika {fileName} nije ugrađena u Torvin.Services.");
        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        return memory.ToArray();
    }
}
