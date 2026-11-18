using Torvin.Model.Responses;
using Torvin.Model.SearchObjects;
using Torvin.Services.Database;
using Torvin.Services.Database.Entities;

namespace Torvin.Services.Services;

public interface IRoleService : IReadService<RoleResponse, RoleSearchObject>;

// Samo čitanje: role su vezane za AppRoles u kodu, pa nova rola ne bi imala nikakvo dejstvo
public sealed class RoleService(TorvinDbContext context, ReferenceDataCache cache)
    : BaseReadService<RoleResponse, RoleSearchObject, Role>(context, cache), IRoleService
{
    protected override string NotFoundMessage => "Rola nije pronađena.";

    protected override IQueryable<Role> AddFilter(IQueryable<Role> query, RoleSearchObject search)
    {
        if (!string.IsNullOrWhiteSpace(search.Fts))
        {
            var fts = search.Fts.Trim();
            query = query.Where(role => role.Name.Contains(fts));
        }

        return query;
    }
}
