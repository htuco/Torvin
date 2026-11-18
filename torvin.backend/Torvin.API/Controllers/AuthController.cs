using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Torvin.Model.Requests;
using Torvin.Model.Responses;
using Torvin.Services.Services;

namespace Torvin.API.Controllers;

[ApiController]
[Route("auth")]
[Authorize]
public sealed class AuthController(IAuthService authService) : ControllerBase
{
    // Prijava je anonimna po prirodi stvari (upute §5)
    [HttpPost("login")]
    [AllowAnonymous]
    public Task<LoginResponse> Login(LoginRequest request, CancellationToken cancellationToken) =>
        authService.LoginAsync(request, cancellationToken);

    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        await authService.LogoutAsync(cancellationToken);
        return NoContent();
    }
}
