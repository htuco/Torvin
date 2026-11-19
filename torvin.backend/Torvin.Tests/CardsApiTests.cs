using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Torvin.Model.Constants;
using Torvin.Model.Enums;
using Torvin.Model.Responses;
using Torvin.Services.Database;
using Torvin.Services.Database.Entities;
using Torvin.Services.Services;

namespace Torvin.Tests;

[Collection(ApiCollection.Name)]
public class CardsApiTests(ApiFactory factory) : IClassFixture<ApiFactory>, IAsyncLifetime
{
    private const string CardNumber = "5555555555554444";

    // API šalje enume kao tekst ("Mastercard"), pa ih klijent u testu čita isto
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

    private Task<HttpClient> AdminAsync() => ClientAsync(ApiFactory.AdminUsername, "Desktop");

    private Task<HttpClient> UserAsync() => ClientAsync(ApiFactory.UserUsername, "Mobile");

    private static string Expiry() => DateTime.UtcNow.AddYears(3).ToString("MM/yy", CultureInfo.InvariantCulture);

    // Kartica drugog korisnika, upisana direktno u bazu
    private async Task<int> AddForeignCardAsync()
    {
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TorvinDbContext>();
        var other = await TestUsers.AddAsync(context, $"drugi.{Guid.NewGuid():N}", AppRoles.User);
        var card = new Card
        {
            UserId = other.Id, CardTypeId = 1, CurrencyId = 1,
            CardStatusId = await context.CardStatuses.Where(s => s.Code == CardStatusCodes.Active).Select(s => s.Id).SingleAsync(),
            Network = CardNetwork.Visa, Last4Digits = "7777", ExpiresAt = new DateOnly(2030, 12, 31), RowVersion = [1],
        };
        context.Cards.Add(card);
        await context.SaveChangesAsync();
        return card.Id;
    }

    [Fact]
    public async Task Anonymous_GetsUnauthorized()
    {
        var client = await ClientAsync(null, "Mobile");

        var response = await client.GetAsync("/cards");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task EachRole_IsLimitedToItsOwnEndpoints()
    {
        var admin = await AdminAsync();
        var user = await UserAsync();

        var adminOnUserCards = await admin.GetAsync("/cards");
        var userOnAdminCards = await user.GetAsync("/admin/cards");
        var userChangesStatus = await user.PutAsJsonAsync("/admin/cards/1/status", new { cardStatusId = 3, reason = "Pokušaj korisnika." });

        Assert.Equal(HttpStatusCode.Forbidden, adminOnUserCards.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, userOnAdminCards.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, userChangesStatus.StatusCode);
    }

    [Fact]
    public async Task Create_Returns201WithoutFullNumber_AndCardIsFirstInList()
    {
        var client = await UserAsync();

        var create = await client.PostAsJsonAsync("/cards",
            new { cardTypeId = 1, currencyId = 1, name = "Online", cardNumber = CardNumber, expiry = Expiry() });
        var body = await create.Content.ReadAsStringAsync();
        var list = await client.GetFromJsonAsync<PagedResult<CardResponse>>("/cards", Json);

        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        Assert.DoesNotContain(CardNumber, body);
        Assert.DoesNotContain("cardNumber", body);
        Assert.Contains("\"network\":\"Mastercard\"", body);
        Assert.Equal("4444", list!.Items[0].Last4Digits);
        Assert.Equal(CardStatusCodes.Pending, list.Items[0].CardStatusCode);
    }

    [Theory]
    [InlineData("4111111111111112", "Broj kartice nije ispravan. Unesite 16 cifara bez razmaka.")]
    [InlineData("6011111111111117", "Podržane su samo Visa i Mastercard kartice.")]
    public async Task Create_WithInvalidNumber_GetsFieldError(string number, string message)
    {
        var client = await UserAsync();

        var response = await client.PostAsJsonAsync("/cards",
            new { cardTypeId = 1, currencyId = 1, cardNumber = number, expiry = Expiry() });
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal([message], error!.Errors!["cardNumber"]);
    }

    [Fact]
    public async Task ForeignCard_IsNotFound()
    {
        var client = await UserAsync();
        var foreignId = await AddForeignCardAsync();

        var get = await client.GetAsync($"/cards/{foreignId}");
        var block = await client.PostAsync($"/cards/{foreignId}/block", null);
        var error = await block.Content.ReadFromJsonAsync<ErrorResponse>();

        Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, block.StatusCode);
        Assert.Equal(CardService.NotFound, error!.Message);
    }

    [Fact]
    public async Task PageSizeOver100_IsRejected()
    {
        var client = await AdminAsync();

        var response = await client.GetAsync("/admin/cards?pageSize=101");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Admin_ChangesStatusWithReason_AndWithoutReasonGetsFieldError()
    {
        var client = await AdminAsync();
        var cardId = await AddForeignCardAsync();

        var withoutReason = await client.PutAsJsonAsync($"/admin/cards/{cardId}/status", new { cardStatusId = 3 });
        var error = await withoutReason.Content.ReadFromJsonAsync<ErrorResponse>();
        var blocked = await client.PutAsJsonAsync($"/admin/cards/{cardId}/status",
            new { cardStatusId = 3, reason = "Sumnjiva aktivnost." });

        Assert.Equal(HttpStatusCode.BadRequest, withoutReason.StatusCode);
        Assert.Equal(["Razlog je obavezan."], error!.Errors!["reason"]);
        Assert.Equal(HttpStatusCode.OK, blocked.StatusCode);
        Assert.Equal(CardStatusCodes.Blocked, (await blocked.Content.ReadFromJsonAsync<CardResponse>(Json))!.CardStatusCode);
    }
}
