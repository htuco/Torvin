using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Torvin.Model.Constants;
using Torvin.Model.Responses;
using Torvin.Services.Database;
using Torvin.Services.Services;

namespace Torvin.Tests;

// Omiljeni primaoci i QR kod za primanje kroz HTTP: statusi, role i put od koda do prikaza primaoca
[Collection(ApiCollection.Name)]
public class FavoriteRecipientsApiTests(ApiFactory factory) : IClassFixture<ApiFactory>, IAsyncLifetime
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };

    public Task InitializeAsync() => factory.SeedUsersAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    private async Task<HttpClient> ClientAsync(string? username, string app)
    {
        var client = factory.CreateClient();
        if (username is null)
        {
            return client;
        }

        var login = await client.PostAsJsonAsync("/auth/login", new { login = username, password = TestUsers.Password, app });
        login.EnsureSuccessStatusCode();
        var token = (await login.Content.ReadFromJsonAsync<LoginResponse>())!.Token;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private Task<HttpClient> UserAsync() => ClientAsync(ApiFactory.UserUsername, "Mobile");

    // Novi korisnik s aktivnom karticom (može primiti novac i prijaviti se u mobilnu aplikaciju)
    private async Task<(string Username, int UserId, int CardId)> AddUserWithCardAsync()
    {
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TorvinDbContext>();
        var user = await TestUsers.AddAsync(context, $"omiljeni.{Guid.NewGuid():N}"[..30], AppRoles.User);
        var card = await TestCards.AddAsync(context, user, 0m, last4: "9876");
        return (user.Username, user.Id, card.Id);
    }

    private async Task<int> AddOwnCardAsync()
    {
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TorvinDbContext>();
        var me = await context.Users.SingleAsync(u => u.Username == ApiFactory.UserUsername);
        return (await TestCards.AddAsync(context, me, 0m, last4: "5555")).Id;
    }

    private static async Task<string> FirstErrorAsync(HttpResponseMessage response, string field)
    {
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("errors").GetProperty(field)[0].GetString()!;
    }

    [Fact]
    public async Task Anonymous_GetsUnauthorized()
    {
        var client = await ClientAsync(null, "Mobile");

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/favorite-recipients")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/cards/1/receive-code")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await client.PostAsJsonAsync("/recipients/resolve-code", new { code = "x" })).StatusCode);
    }

    [Fact]
    public async Task Admin_IsForbidden()
    {
        var admin = await ClientAsync(ApiFactory.AdminUsername, "Desktop");

        Assert.Equal(HttpStatusCode.Forbidden, (await admin.GetAsync("/favorite-recipients")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await admin.GetAsync("/cards/1/receive-code")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await admin.PostAsJsonAsync("/recipients/resolve-code", new { code = "x" })).StatusCode);
    }

    [Fact]
    public async Task Favorite_CreateUpdateDelete_ReturnsExpectedStatuses()
    {
        var client = await UserAsync();
        var recipient = await AddUserWithCardAsync();

        var created = await client.PostAsJsonAsync("/favorite-recipients", new { recipientUserId = recipient.UserId, nickname = "Kolega" });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var favorite = (await created.Content.ReadFromJsonAsync<FavoriteRecipientResponse>(Json))!;

        var duplicate = await client.PostAsJsonAsync("/favorite-recipients", new { recipientUserId = recipient.UserId });
        Assert.Equal(HttpStatusCode.BadRequest, duplicate.StatusCode);
        Assert.Equal(FavoriteRecipientService.AlreadyFavorite,
            (await duplicate.Content.ReadFromJsonAsync<ErrorResponse>())!.Message);

        var updated = await client.PutAsJsonAsync($"/favorite-recipients/{favorite.Id}", new { nickname = "Šef" });
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
        Assert.Equal("Šef", (await updated.Content.ReadFromJsonAsync<FavoriteRecipientResponse>(Json))!.Nickname);

        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/favorite-recipients/{favorite.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.DeleteAsync($"/favorite-recipients/{favorite.Id}")).StatusCode);
    }

    [Fact]
    public async Task Favorite_NicknameTooLong_IsRejected()
    {
        var client = await UserAsync();
        var recipient = await AddUserWithCardAsync();

        var response = await client.PostAsJsonAsync("/favorite-recipients",
            new { recipientUserId = recipient.UserId, nickname = new string('a', 51) });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("Nadimak može imati najviše 50 znakova.", await FirstErrorAsync(response, "nickname"));
    }

    [Fact]
    public async Task ReceiveCode_ScannedByOtherUser_ResolvesToMaskedCard()
    {
        var owner = await UserAsync();
        var cardId = await AddOwnCardAsync();
        var sender = await AddUserWithCardAsync();
        var senderClient = await ClientAsync(sender.Username, "Mobile");

        var codeResponse = await owner.GetAsync($"/cards/{cardId}/receive-code?amount=25.50");
        Assert.Equal(HttpStatusCode.OK, codeResponse.StatusCode);
        var code = (await codeResponse.Content.ReadFromJsonAsync<ReceiveCodeResponse>(Json))!;

        var resolved = await senderClient.PostAsJsonAsync("/recipients/resolve-code", new { code = code.Code });

        Assert.Equal(HttpStatusCode.OK, resolved.StatusCode);
        var recipient = (await resolved.Content.ReadFromJsonAsync<ReceiveCodeRecipientResponse>(Json))!;
        Assert.Equal(ApiFactory.UserUsername, recipient.Username);
        Assert.Equal("5555", recipient.Last4Digits);
        Assert.Equal(25.50m, recipient.Amount);
    }

    [Fact]
    public async Task ReceiveCode_ForeignCard_IsNotFound_AndTamperedCodeIsRejected()
    {
        var client = await UserAsync();
        var foreign = await AddUserWithCardAsync();

        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/cards/{foreign.CardId}/receive-code")).StatusCode);

        var tampered = await client.PostAsJsonAsync("/recipients/resolve-code",
            new { code = ReceiveCodeSignerTests.NewSigner("0123456789abcdef0123456789abcdef")
                .Create(new(foreign.CardId, null, DateTime.UtcNow.AddMinutes(5))) });
        Assert.Equal(HttpStatusCode.BadRequest, tampered.StatusCode);
        Assert.Equal(ReceiveCodeService.CodeInvalid, await FirstErrorAsync(tampered, "code"));
    }

    [Fact]
    public async Task ReceiveCode_AmountNotANumber_IsBadRequest()
    {
        var client = await UserAsync();
        var cardId = await AddOwnCardAsync();

        var response = await client.GetAsync($"/cards/{cardId}/receive-code?amount=abc");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
