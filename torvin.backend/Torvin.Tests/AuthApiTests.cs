using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Torvin.API.Infrastructure;
using Torvin.Model.Constants;
using Torvin.Model.Messages;
using Torvin.Model.Responses;
using Torvin.Services.Database;
using Torvin.Services.Security;
using Torvin.Services.Services;

namespace Torvin.Tests;

public class AuthApiTests(ApiFactory factory) : IClassFixture<ApiFactory>, IAsyncLifetime
{
    public Task InitializeAsync() => factory.SeedUsersAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    private async Task<string> LoginAsync(HttpClient client, string username, string app)
    {
        var response = await client.PostAsJsonAsync("/auth/login", new { login = username, password = TestUsers.Password, app });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<LoginResponse>())!.Token;
    }

    private static HttpRequestMessage Get(string path, string? token)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, path);
        if (token is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return request;
    }

    [Fact]
    public async Task Login_TokenWorksOnProtectedEndpoint()
    {
        using var client = factory.CreateClient();
        var token = await LoginAsync(client, ApiFactory.UserUsername, "Mobile");

        var response = await client.SendAsync(Get("/test/me", token));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Logout_SameTokenIsRejected()
    {
        using var client = factory.CreateClient();
        var token = await LoginAsync(client, ApiFactory.UserUsername, "Mobile");

        using var logout = new HttpRequestMessage(HttpMethod.Post, "/auth/logout");
        logout.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        Assert.Equal(HttpStatusCode.NoContent, (await client.SendAsync(logout)).StatusCode);

        var response = await client.SendAsync(Get("/test/me", token));
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(JwtAuthenticationExtensions.UnauthorizedMessage,
            (await response.Content.ReadFromJsonAsync<ErrorResponse>())!.Message);
    }

    [Fact]
    public async Task NoToken_Returns401WithErrorBody()
    {
        using var client = factory.CreateClient();

        var response = await client.SendAsync(Get("/test/me", null));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(JwtAuthenticationExtensions.UnauthorizedMessage,
            (await response.Content.ReadFromJsonAsync<ErrorResponse>())!.Message);
    }

    [Fact]
    public async Task TamperedToken_Returns401()
    {
        using var client = factory.CreateClient();
        var token = await LoginAsync(client, ApiFactory.UserUsername, "Mobile");

        var response = await client.SendAsync(Get("/test/me", token[..^4] + "AAAA"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UserOnAdminEndpoint_Returns403WithErrorBody()
    {
        using var client = factory.CreateClient();
        var token = await LoginAsync(client, ApiFactory.UserUsername, "Mobile");

        var response = await client.SendAsync(Get("/test/admin", token));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(JwtAuthenticationExtensions.ForbiddenMessage,
            (await response.Content.ReadFromJsonAsync<ErrorResponse>())!.Message);
    }

    [Fact]
    public async Task AdminOnAdminEndpoint_IsAllowed()
    {
        using var client = factory.CreateClient();
        var token = await LoginAsync(client, ApiFactory.AdminUsername, "Desktop");

        var response = await client.SendAsync(Get("/test/admin", token));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task UserOnDesktop_Returns403()
    {
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/auth/login",
            new { login = ApiFactory.UserUsername, password = TestUsers.Password, app = "Desktop" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task InvalidEnumAndEmptyBody_ReturnBosnianMessages()
    {
        using var client = factory.CreateClient();

        var invalidEnum = await client.PostAsJsonAsync("/auth/login", new { login = "a", password = "b", app = "Tablet" });
        var emptyBody = await client.PostAsync("/auth/login",
            new StringContent("", System.Text.Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, invalidEnum.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, emptyBody.StatusCode);
        var invalidEnumText = await invalidEnum.Content.ReadAsStringAsync();
        var emptyBodyText = await emptyBody.Content.ReadAsStringAsync();
        Assert.DoesNotContain("JSON", invalidEnumText);
        Assert.DoesNotContain("The ", invalidEnumText);
        Assert.Contains("Tijelo zahtjeva je obavezno.", emptyBodyText);
        Assert.DoesNotContain("The ", emptyBodyText);
        Assert.Contains("\"app\"", invalidEnumText);
    }

    [Fact]
    public async Task MissingApp_ReturnsFieldMessage()
    {
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/auth/login", new { login = "a", password = "b" });

        var body = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Aplikacija je obavezna", body!.Errors!["app"].Single());
    }

    // Token potpisan ispravnim ključem, ali s pogrešnim izdavačem, publikom ili rokom: API ga mora odbiti
    private static string ForgeToken(string issuer, string audience, DateTime expires)
    {
        var jwt = AuthServiceTests.Jwt;
        return new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = issuer,
            Audience = audience,
            IssuedAt = expires.AddMinutes(-60),
            NotBefore = expires.AddMinutes(-60),
            Expires = expires,
            Claims = new Dictionary<string, object>
            {
                [TorvinClaims.UserId] = "1",
                [TorvinClaims.Role] = "Admin",
                [TorvinClaims.TokenId] = Guid.NewGuid().ToString("N"),
            },
            SigningCredentials = new SigningCredentials(JwtTokenService.SigningKey(jwt.Key), SecurityAlgorithms.HmacSha256),
        });
    }

    [Theory]
    [InlineData("drugi-izdavac", "torvin-test", 30)]
    [InlineData("torvin-test", "druga-aplikacija", 30)]
    [InlineData("torvin-test", "torvin-test", -5)]
    public async Task WrongIssuerAudienceOrExpiredToken_Returns401(string issuer, string audience, int expiresInMinutes)
    {
        using var client = factory.CreateClient();
        var token = ForgeToken(issuer, audience, DateTime.UtcNow.AddMinutes(expiresInMinutes));

        var response = await client.SendAsync(Get("/test/admin", token));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ValidForgedTokenControl_IsAccepted()
    {
        // Kontrola za test iznad: isti token s ispravnim podacima prolazi, pa odbijanje dolazi od izdavača, publike i roka
        using var client = factory.CreateClient();
        var token = ForgeToken("torvin-test", "torvin-test", DateTime.UtcNow.AddMinutes(30));

        var response = await client.SendAsync(Get("/test/admin", token));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task NonJsonBody_Returns415WithErrorBody()
    {
        using var client = factory.CreateClient();

        var response = await client.PostAsync("/auth/login", new StringContent("login=a", System.Text.Encoding.UTF8, "text/plain"));

        Assert.Equal(HttpStatusCode.UnsupportedMediaType, response.StatusCode);
        Assert.Contains("JSON", (await response.Content.ReadFromJsonAsync<ErrorResponse>())!.Message);
    }

    [Fact]
    public async Task DoubleLogout_SecondIsRejectedNotServerError()
    {
        using var client = factory.CreateClient();
        var token = await LoginAsync(client, ApiFactory.UserUsername, "Mobile");

        async Task<HttpStatusCode> Logout()
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "/auth/logout");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return (await client.SendAsync(request)).StatusCode;
        }

        Assert.Equal(HttpStatusCode.NoContent, await Logout());
        Assert.Equal(HttpStatusCode.Unauthorized, await Logout());
    }

    [Fact]
    public async Task Register_Returns201_AndIgnoresRoleFromBody()
    {
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/auth/register", new
        {
            firstName = "Haris", lastName = "Begić", username = "haris.api", email = "haris.api@torvin.local",
            password = "Torvin.2026!", confirmPassword = "Torvin.2026!", role = AppRoles.Admin,
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(AuthService.Registered, (await response.Content.ReadFromJsonAsync<MessageResponse>())!.Message);
        using var scope = factory.Services.CreateScope();
        var user = await scope.ServiceProvider.GetRequiredService<TorvinDbContext>().Users
            .Include(u => u.Role).SingleAsync(u => u.Username == "haris.api");
        Assert.Equal(AppRoles.User, user.Role.Name);
        Assert.Contains(factory.Publisher.Published, p => ((EmailRequested)p.Message).To == "haris.api@torvin.local");
    }

    [Fact]
    public async Task VerifyEmail_WithWrongCodeFormat_Returns400WithFormatMessage()
    {
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/auth/verify-email", new { email = "a@torvin.local", code = "12345" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = (await response.Content.ReadFromJsonAsync<ErrorResponse>())!;
        Assert.Equal(["Kod ima tačno 6 cifara."], body.Errors!["code"]);
    }

    [Fact]
    public async Task ForgotPassword_ForUnknownEmail_Returns204()
    {
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/auth/forgot-password", new { email = "nepostojeci@torvin.local" });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }
}
