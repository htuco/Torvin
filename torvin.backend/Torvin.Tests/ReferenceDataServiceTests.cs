using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Torvin.Model.Constants;
using Torvin.Model.Exceptions;
using Torvin.Model.Requests;
using Torvin.Model.SearchObjects;
using Torvin.Services.Database;
using Torvin.Services.Database.Entities;
using Torvin.Services.Services;

namespace Torvin.Tests;

// Referentni podaci iz HasData (3 valute, 6 statusa kartica, 7 kategorija) su u svakoj testnoj bazi
public class ReferenceDataServiceTests
{
    private const int EurId = 2;
    private const int UsdId = 3;
    private const int ActiveCardStatusId = 2;
    private const int FoodCategoryId = 1;

    private readonly ReferenceDataCache _cache = new(new MemoryCache(new MemoryCacheOptions()));

    private static async Task<TorvinDbContext> NewContextAsync()
    {
        var context = new TorvinDbContext(TestDatabase.CreateOptions());
        await context.Database.EnsureCreatedAsync();
        return context;
    }

    private CurrencyService Currencies(TorvinDbContext context) =>
        new(context, _cache, NullLogger<CurrencyService>.Instance);

    private TransactionCategoryService Categories(TorvinDbContext context) =>
        new(context, _cache, NullLogger<TransactionCategoryService>.Instance);

    private CardStatusService CardStatuses(TorvinDbContext context) =>
        new(context, _cache, NullLogger<CardStatusService>.Instance);

    private static Card NewCard(int currencyId) => new()
    {
        UserId = 1, CardTypeId = 1, CardStatusId = ActiveCardStatusId, CurrencyId = currencyId,
        Last4Digits = "4821", ExpiresAt = new DateOnly(2029, 12, 31), RowVersion = [1],
    };

    [Fact]
    public async Task GetPaged_PageSizeAboveMaximum_IsCappedAt100()
    {
        await using var context = await NewContextAsync();
        for (var i = 0; i < 105; i++)
        {
            context.TransactionCategories.Add(new TransactionCategory { Name = $"Kategorija {i}" });
        }

        await context.SaveChangesAsync();

        var result = await Categories(context).GetPagedAsync(new TransactionCategorySearchObject { PageSize = 500 });

        Assert.Equal(BaseSearchObject.MaxPageSize, result.PageSize);
        Assert.Equal(100, result.Items.Count);
        Assert.Equal(112, result.TotalCount);
    }

    [Fact]
    public async Task GetPaged_SecondPage_ReturnsRestInIdOrder()
    {
        await using var context = await NewContextAsync();

        var result = await Categories(context)
            .GetPagedAsync(new TransactionCategorySearchObject { Page = 2, PageSize = 5 });

        Assert.Equal([6, 7], result.Items.Select(category => category.Id));
        Assert.Equal(7, result.TotalCount);
        Assert.Equal(2, result.Page);
    }

    [Fact]
    public async Task GetPaged_FtsAndCode_FilterInDatabase()
    {
        await using var context = await NewContextAsync();
        var service = Currencies(context);

        var byName = await service.GetPagedAsync(new CurrencySearchObject { Fts = "Euro" });
        var byCode = await service.GetPagedAsync(new CurrencySearchObject { Code = CurrencyCodes.USD });

        Assert.Equal(CurrencyCodes.EUR, Assert.Single(byName.Items).Code);
        Assert.Equal(1, byName.TotalCount);
        Assert.Equal(UsdId, Assert.Single(byCode.Items).Id);
    }

    [Fact]
    public async Task Delete_CurrencyUsedByCard_ThrowsWithCount()
    {
        await using var context = await NewContextAsync();
        context.Cards.Add(NewCard(EurId));
        await context.SaveChangesAsync();

        var exception = await Assert.ThrowsAsync<BusinessException>(() => Currencies(context).DeleteAsync(EurId));

        Assert.Equal("Valuta EUR se koristi i ne može se obrisati (kartice: 1).", exception.Message);
        Assert.True(await context.Currencies.AnyAsync(currency => currency.Id == EurId));
    }

    [Fact]
    public async Task Delete_CategoryUsedOnlyBySoftDeletedBudget_StillThrows()
    {
        await using var context = await NewContextAsync();
        var budget = new Budget
        {
            UserId = 1, TransactionCategoryId = FoodCategoryId, CurrencyId = 1, Year = 2026, Month = 11, LimitAmount = 300m,
        };
        context.Budgets.Add(budget);
        await context.SaveChangesAsync();
        context.Budgets.Remove(budget);
        await context.SaveChangesAsync();

        var exception = await Assert.ThrowsAsync<BusinessException>(
            () => Categories(context).DeleteAsync(FoodCategoryId));

        Assert.Equal("Kategorija Hrana se koristi i ne može se obrisati (budžeti: 1).", exception.Message);
    }

    [Fact]
    public async Task Delete_UnusedCurrency_RemovesIt()
    {
        await using var context = await NewContextAsync();

        await Currencies(context).DeleteAsync(UsdId);

        Assert.False(await context.Currencies.AnyAsync(currency => currency.Id == UsdId));
    }

    [Fact]
    public async Task Delete_SystemCardStatus_Throws()
    {
        await using var context = await NewContextAsync();

        var exception = await Assert.ThrowsAsync<BusinessException>(
            () => CardStatuses(context).DeleteAsync(ActiveCardStatusId));

        Assert.Equal("Status kartice Active je sistemski i ne može se obrisati.", exception.Message);
    }

    [Fact]
    public async Task Delete_NewUnusedCardStatus_RemovesIt()
    {
        await using var context = await NewContextAsync();
        var service = CardStatuses(context);
        var created = await service.InsertAsync(new CardStatusInsertRequest { Code = "Expired", Name = "Istekla" });

        await service.DeleteAsync(created.Id);

        Assert.False(await context.CardStatuses.AnyAsync(status => status.Code == "Expired"));
    }

    [Fact]
    public async Task Insert_DuplicateCurrencyCode_Throws()
    {
        await using var context = await NewContextAsync();

        var exception = await Assert.ThrowsAsync<BusinessException>(() => Currencies(context).InsertAsync(
            new CurrencyInsertRequest { Code = CurrencyCodes.EUR, Name = "Euro", Symbol = "€" }));

        Assert.Equal("Valuta s kodom EUR već postoji.", exception.Message);
    }

    [Fact]
    public async Task InsertAndUpdate_DuplicateCategoryName_ThrowsButOwnNameIsAllowed()
    {
        await using var context = await NewContextAsync();
        var service = Categories(context);

        var exception = await Assert.ThrowsAsync<BusinessException>(
            () => service.InsertAsync(new TransactionCategoryRequest { Name = "Hrana" }));
        var updated = await service.UpdateAsync(FoodCategoryId,
            new TransactionCategoryRequest { Name = "Hrana", Description = "Novi opis." });

        Assert.Equal("Kategorija s nazivom Hrana već postoji.", exception.Message);
        Assert.Equal("Novi opis.", updated.Description);
    }

    [Fact]
    public async Task Update_CardStatus_ChangesNameButKeepsCode()
    {
        await using var context = await NewContextAsync();

        var updated = await CardStatuses(context).UpdateAsync(ActiveCardStatusId,
            new CardStatusUpdateRequest { Name = "Aktivirana" });

        Assert.Equal("Aktivirana", updated.Name);
        Assert.Equal(CardStatusCodes.Active, updated.Code);
        Assert.Null(updated.Description);
    }

    [Fact]
    public async Task GetPaged_IsCachedUntilServiceChangesData()
    {
        await using var context = await NewContextAsync();
        var service = Currencies(context);
        var search = new CurrencySearchObject();
        await service.GetPagedAsync(search);

        await service.InsertAsync(new CurrencyInsertRequest { Code = "CHF", Name = "Švicarski franak", Symbol = "Fr" });
        var afterInsert = await service.GetPagedAsync(search);

        // Upis mimo servisa ne poništava keš, pa lista ostaje ista: dokaz da se čita iz keša
        context.Currencies.Add(new Currency { Code = "GBP", Name = "Britanska funta", Symbol = "£" });
        await context.SaveChangesAsync();
        var cached = await service.GetPagedAsync(search);

        Assert.Equal(4, afterInsert.TotalCount);
        Assert.Equal(4, cached.TotalCount);
    }

    [Fact]
    public async Task GetPaged_WithSearch_IsNotCached()
    {
        await using var context = await NewContextAsync();
        var service = Currencies(context);
        var search = new CurrencySearchObject { Fts = "funta" };
        await service.GetPagedAsync(search);

        context.Currencies.Add(new Currency { Code = "GBP", Name = "Britanska funta", Symbol = "£" });
        await context.SaveChangesAsync();
        var result = await service.GetPagedAsync(search);

        Assert.Equal("GBP", Assert.Single(result.Items).Code);
    }

    [Fact]
    public async Task MissingId_ThrowsNotFound()
    {
        await using var context = await NewContextAsync();
        var service = Currencies(context);

        await Assert.ThrowsAsync<NotFoundException>(() => service.GetByIdAsync(999));
        await Assert.ThrowsAsync<NotFoundException>(() =>
            service.UpdateAsync(999, new CurrencyUpdateRequest { Name = "X", Symbol = "X" }));
        await Assert.ThrowsAsync<NotFoundException>(() => service.DeleteAsync(999));
    }
}
