using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Torvin.Model.Constants;
using Torvin.Model.Exceptions;
using Torvin.Model.Responses;
using Torvin.Model.SearchObjects;
using Torvin.Services.Database;
using Torvin.Services.Database.Entities;
using Torvin.Services.Security;

namespace Torvin.Services.Services;

public interface IRecipientService
{
    Task<PagedResult<RecipientResponse>> SearchAsync(RecipientSearchObject search, CancellationToken cancellationToken = default);
}

// Pretraga korisnika kojima se može poslati novac. Ograničena je (najmanje 3 znaka, najviše 10 rezultata, email samo
// tačno), da se kroz nju ne može izlistati spisak korisnika ni njihovih adresa
public sealed class RecipientService(TorvinDbContext context, ICurrentUserService currentUser) : IRecipientService
{
    public const int MinQueryLength = 3;
    public const int MaxResults = 10;

    public const string QueryTooShort = "Unesite najmanje 3 znaka za pretragu.";

    // Ko stvarno može primiti novac: aktivan, potvrđen korisnik (ne admin) s bar jednom aktivnom karticom. Isto pravilo
    // važi za pretragu, omiljene primaoce i QR kod
    public static readonly Expression<Func<User, bool>> CanReceiveMoney = u => u.IsActive && u.EmailConfirmed
        && u.Role.Name == AppRoles.User && u.Cards.Any(c => c.CardStatus.Code == CardStatusCodes.Active);

    public Task<PagedResult<RecipientResponse>> SearchAsync(RecipientSearchObject search,
        CancellationToken cancellationToken = default)
    {
        var fts = (search.Fts ?? "").Trim();
        if (fts.Length < MinQueryLength)
        {
            throw new ValidationException(new Dictionary<string, string[]> { ["fts"] = [QueryTooShort] });
        }

        var userId = currentUser.UserId;
        search.PageSize = Math.Min(search.PageSize, MaxResults);

        return context.Users.AsNoTracking()
            .Where(u => u.Id != userId)
            .Where(CanReceiveMoney)
            .Where(u => u.FirstName.Contains(fts) || u.LastName.Contains(fts) || u.Username.Contains(fts)
                || (u.FirstName + " " + u.LastName).Contains(fts) || u.Email == fts)
            .OrderBy(u => u.LastName)
            .ThenBy(u => u.FirstName)
            .ThenBy(u => u.Id)
            .Select(u => new RecipientResponse(u.Id, u.FirstName, u.LastName, u.Username))
            .ToPagedResultAsync(search, cancellationToken);
    }
}
