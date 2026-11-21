using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Torvin.Model.Constants;
using Torvin.Model.Enums;
using Torvin.Model.Responses;
using Torvin.Model.Validation;
using Torvin.Services.Database;
using Torvin.Services.Security;
using Torvin.Services.Services;

namespace Torvin.Tests;

[Collection(ApiCollection.Name)]
public class TransactionsApiTests(ApiFactory factory) : IClassFixture<ApiFactory>, IAsyncLifetime
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

    // Kartica testnog korisnika i novi primalac s karticom u istoj valuti
    private async Task<(int FromCardId, int ToUserId)> PrepareTransferAsync(decimal balance = 100m)
    {
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TorvinDbContext>();
        var me = await context.Users.SingleAsync(u => u.Username == ApiFactory.UserUsername);
        var card = await TestCards.AddAsync(context, me, balance);
        var recipient = await TestUsers.AddAsync(context, $"primalac.{Guid.NewGuid():N}", AppRoles.User);
        await TestCards.AddAsync(context, recipient, 0m);
        return (card.Id, recipient.Id);
    }

    private static async Task<Dictionary<string, string[]>> ErrorsAsync(HttpResponseMessage response)
    {
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("errors").Deserialize<Dictionary<string, string[]>>()!;
    }

    [Fact]
    public async Task Anonymous_GetsUnauthorized()
    {
        var client = await ClientAsync(null, "Mobile");

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/transactions")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/recipients/search?fts=test")).StatusCode);
    }

    [Fact]
    public async Task Admin_IsForbidden()
    {
        var admin = await ClientAsync(ApiFactory.AdminUsername, "Desktop");

        Assert.Equal(HttpStatusCode.Forbidden, (await admin.GetAsync("/transactions")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await admin.GetAsync("/recipients/search?fts=test")).StatusCode);
    }

    [Fact]
    public async Task Create_ReturnsCreatedTransaction_VisibleInHistory()
    {
        var client = await UserAsync();
        var (fromCardId, toUserId) = await PrepareTransferAsync();

        var response = await client.PostAsJsonAsync("/transactions",
            new { fromCardId, toUserId, amount = 12.5m, note = "Pizza" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = (await response.Content.ReadFromJsonAsync<TransactionResponse>(Json))!;
        Assert.Equal(TransactionDirection.Sent, created.Direction);
        Assert.Equal(12.5m, created.Amount);

        var details = await client.GetFromJsonAsync<TransactionResponse>($"/transactions/{created.Id}", Json);
        Assert.Equal(created.ReferenceNumber, details!.ReferenceNumber);
        var page = await client.GetFromJsonAsync<PagedResult<TransactionResponse>>($"/transactions?cardId={fromCardId}", Json);
        Assert.Equal(created.Id, Assert.Single(page!.Items).Id);
    }

    [Fact]
    public async Task Create_WithThreeDecimals_GivesFieldError()
    {
        var client = await UserAsync();
        var (fromCardId, toUserId) = await PrepareTransferAsync();

        var response = await client.PostAsJsonAsync("/transactions", new { fromCardId, toUserId, amount = 1.005m });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal([Money.InvalidAmount], (await ErrorsAsync(response))["amount"]);
    }

    [Fact]
    public async Task Create_WithMoreThanBalance_GivesFieldError()
    {
        var client = await UserAsync();
        var (fromCardId, toUserId) = await PrepareTransferAsync(balance: 5m);

        var response = await client.PostAsJsonAsync("/transactions", new { fromCardId, toUserId, amount = 5.01m });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal([TransactionService.InsufficientFunds("4821")], (await ErrorsAsync(response))["amount"]);
    }

    [Fact]
    public async Task SomeoneElsesTransaction_IsNotFound()
    {
        int foreignId;
        using (var scope = factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<TorvinDbContext>();
            var first = await TestUsers.AddAsync(context, $"prvi.{Guid.NewGuid():N}", AppRoles.User);
            var second = await TestUsers.AddAsync(context, $"drugi.{Guid.NewGuid():N}", AppRoles.User);
            var card = await TestCards.AddAsync(context, first, 50m);
            await TestCards.AddAsync(context, second, 0m);
            var service = new TransactionService(context, new FixedCurrentUser(first.Id), new FakeMessagePublisher(),
                NullLogger<TransactionService>.Instance);
            foreignId = (await service.CreateAsync(new() { FromCardId = card.Id, ToUserId = second.Id, Amount = 1m })).Id;
        }

        var client = await UserAsync();

        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/transactions/{foreignId}")).StatusCode);
    }

    [Fact]
    public async Task Lists_RejectTooLargePageAndTooShortSearch()
    {
        var client = await UserAsync();

        var tooLarge = await client.GetAsync("/transactions?pageSize=101");
        var tooShort = await client.GetAsync("/recipients/search?fts=ab");

        Assert.Equal(HttpStatusCode.BadRequest, tooLarge.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, tooShort.StatusCode);
        Assert.Equal([RecipientService.QueryTooShort], (await ErrorsAsync(tooShort))["fts"]);
    }

    private sealed class FixedCurrentUser(int userId) : ICurrentUserService
    {
        public int UserId { get; } = userId;
        public string Role => AppRoles.User;
        public string TokenId => "test";
        public DateTime TokenExpiresAt => DateTime.UtcNow.AddHours(1);
    }
}
