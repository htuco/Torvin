using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Torvin.Model.Constants;
using Torvin.Services.Security;

namespace Torvin.Tests;

// Postoji samo u testovima: zaštićeni endpointi za provjeru tokena, opoziva i role
[ApiController]
[Route("test")]
[Authorize]
public sealed class TestController(ICurrentUserService currentUser) : ControllerBase
{
    [HttpGet("me")]
    public int Me() => currentUser.UserId;

    [HttpGet("admin")]
    [Authorize(Roles = AppRoles.Admin)]
    public IActionResult Admin() => NoContent();
}
