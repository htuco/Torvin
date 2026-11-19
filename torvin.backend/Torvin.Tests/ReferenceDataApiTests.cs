using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Torvin.Model.Enums;
using Torvin.Model.Responses;
using Torvin.Services.Database;
using Torvin.Services.Database.Entities;

namespace Torvin.Tests;

[Collection(ApiCollection.Name)]
public class ReferenceDataApiTests(ApiFactory factory) : IClassFixture<ApiFactory>, IAsyncLifetime
{
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

    [Fact]
    public async Task User_CanListButNotChange()
    {
        var client = await UserAsync();

        var list = await client.GetFromJsonAsync<PagedResult<CurrencyResponse>>("/currencies");
        var create = await client.PostAsJsonAsync("/currencies", new { code = "JPY", name = "Jen", symbol = "¥" });

        // Drugi testovi u klasi dijele bazu i dodaju valute, pa se ne provjerava tačan broj
        Assert.Contains(list!.Items, currency => currency.Code == "EUR");
        Assert.Equal(HttpStatusCode.Forbidden, create.StatusCode);
    }

    [Fact]
    public async Task Anonymous_GetsUnauthorized()
    {
        var client = await ClientAsync(null, "Mobile");

        var response = await client.GetAsync("/card-types");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Admin_CreatesUpdatesAndDeletesCategory()
    {
        var client = await AdminAsync();

        var create = await client.PostAsJsonAsync("/transaction-categories", new { name = "Putovanja" });
        var created = await create.Content.ReadFromJsonAsync<TransactionCategoryResponse>();
        var update = await client.PutAsJsonAsync($"/transaction-categories/{created!.Id}",
            new { name = "Putovanja", description = "Avion, hotel i smještaj." });
        var delete = await client.DeleteAsync($"/transaction-categories/{created.Id}");
        var afterDelete = await client.GetAsync($"/transaction-categories/{created.Id}");

        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        Assert.Equal($"/transaction-categories/{created.Id}", create.Headers.Location!.AbsolutePath);
        Assert.Equal("Avion, hotel i smještaj.",
            (await update.Content.ReadFromJsonAsync<TransactionCategoryResponse>())!.Description);
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, afterDelete.StatusCode);
    }

    [Fact]
    public async Task Admin_DeletingCurrencyInUse_GetsBadRequestWithReason()
    {
        var client = await AdminAsync();
        var create = await client.PostAsJsonAsync("/currencies", new { code = "GBP", name = "Britanska funta", symbol = "£" });
        var currency = (await create.Content.ReadFromJsonAsync<CurrencyResponse>())!;
        using (var scope = factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<TorvinDbContext>();
            context.Cards.Add(new Card
            {
                UserId = 1, CardTypeId = 1, CardStatusId = 2, CurrencyId = currency.Id,
                Last4Digits = "4821", Network = CardNetwork.Visa, ExpiresAt = new DateOnly(2029, 12, 31), RowVersion = [1],
            });
            await context.SaveChangesAsync();
        }

        var response = await client.DeleteAsync($"/currencies/{currency.Id}");
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("Valuta GBP se koristi i ne može se obrisati (kartice: 1).", error!.Message);
    }

    [Fact]
    public async Task PageSizeAboveMaximum_GetsBadRequest()
    {
        var client = await UserAsync();

        var response = await client.GetAsync("/currencies?pageSize=101");
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(["Veličina stranice mora biti između 1 i 100."], error!.Errors!["pageSize"]);
    }

    [Fact]
    public async Task InvalidCurrencyCode_GetsFieldError()
    {
        var client = await AdminAsync();

        var response = await client.PostAsJsonAsync("/currencies", new { code = "eu", name = "Euro", symbol = "€" });
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(["Kod valute su tri velika slova po ISO 4217 (npr. EUR)."], error!.Errors!["code"]);
    }

    [Fact]
    public async Task Roles_AreReadOnly()
    {
        var client = await AdminAsync();

        var list = await client.GetFromJsonAsync<PagedResult<RoleResponse>>("/roles");
        var create = await client.PostAsJsonAsync("/roles", new { name = "Auditor" });

        Assert.Equal(2, list!.TotalCount);
        Assert.Equal(HttpStatusCode.MethodNotAllowed, create.StatusCode);
    }
}
