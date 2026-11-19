using Microsoft.AspNetCore.Mvc;
using Torvin.Model.Responses;
using Torvin.Model.SearchObjects;
using Torvin.Services.Services;

namespace Torvin.API.Controllers;

// Samo čitanje, za izbor role pri upravljanju korisnicima. Role su vezane za AppRoles u kodu
[Route("roles")]
public sealed class RolesController(IRoleService service) : BaseReadController<RoleResponse, RoleSearchObject>(service);
