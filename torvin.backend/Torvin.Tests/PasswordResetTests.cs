using Torvin.Model.Constants;
using Torvin.Model.Enums;
using Torvin.Model.Exceptions;
using Torvin.Model.Requests;
using Torvin.Services.Services;
using static Torvin.Tests.AuthFlowTestHelpers;

namespace Torvin.Tests;

public class PasswordResetTests
{
    private const string NewPassword = "Torvin.2027!";

    private static ResetPasswordRequest Reset(string code, string password = NewPassword) => new()
    {
        Email = "amina@torvin.local", Code = code, NewPassword = password, ConfirmPassword = password,
    };

    private static LoginRequest Login(string password) =>
        new() { Login = "amina", Password = password, App = ClientApp.Mobile };

    [Theory]
    [InlineData("nema@torvin.local", true)] // nepostojeći email
    [InlineData("amina@torvin.local", false)] // blokiran račun
    public async Task Forgot_ForUnknownOrBlockedAccount_SendsNothingAndDoesNotThrow(string email, bool isActive)
    {
        await using var context = await NewContextAsync(TestDatabase.CreateOptions());
        await TestUsers.AddAsync(context, "amina", AppRoles.User, isActive: isActive);
        var publisher = new FakeMessagePublisher();

        await AuthServiceTests.NewService(context, publisher).ForgotPasswordAsync(new ForgotPasswordRequest { Email = email });

        Assert.Empty(publisher.Published);
    }

    [Fact]
    public async Task Forgot_TwiceWithinPause_SendsOneEmail()
    {
        await using var context = await NewContextAsync(TestDatabase.CreateOptions());
        await TestUsers.AddAsync(context, "amina", AppRoles.User);
        var publisher = new FakeMessagePublisher();
        var service = AuthServiceTests.NewService(context, publisher);

        await service.ForgotPasswordAsync(new ForgotPasswordRequest { Email = "amina@torvin.local" });
        await service.ForgotPasswordAsync(new ForgotPasswordRequest { Email = "AMINA@torvin.local" });

        Assert.Single(publisher.Published);
    }

    [Fact]
    public async Task Forgot_WhenPublishFails_RespondsLikeForUnknownEmail()
    {
        await using var context = await NewContextAsync(TestDatabase.CreateOptions());
        await TestUsers.AddAsync(context, "amina", AppRoles.User);
        var publisher = new FakeMessagePublisher { OnPublish = _ => throw new InvalidOperationException("RabbitMQ nije dostupan.") };

        await AuthServiceTests.NewService(context, publisher).ForgotPasswordAsync(new ForgotPasswordRequest { Email = "amina@torvin.local" });
    }

    [Fact]
    public async Task Reset_WithRecentPassword_IsRejected_AndCodeStaysValid()
    {
        await using var context = await NewContextAsync(TestDatabase.CreateOptions());
        await TestUsers.AddAsync(context, "amina", AppRoles.User);
        var publisher = new FakeMessagePublisher();
        var service = AuthServiceTests.NewService(context, publisher);
        await service.ForgotPasswordAsync(new ForgotPasswordRequest { Email = "amina@torvin.local" });
        var code = LastCode(publisher);

        var exception = await Assert.ThrowsAsync<ValidationException>(() => service.ResetPasswordAsync(Reset(code, TestUsers.Password)));
        Assert.Equal([AuthService.PasswordRecentlyUsed], exception.Errors["newPassword"]);

        await service.ResetPasswordAsync(Reset(code));
    }

    [Fact]
    public async Task Reset_ChangesPassword_UnlocksAndConfirmsAccount_AndCodeIsSingleUse()
    {
        await using var context = await NewContextAsync(TestDatabase.CreateOptions());
        var user = await TestUsers.AddAsync(context, "amina", AppRoles.User, emailConfirmed: false);
        user.FailedLoginCount = 3;
        user.LockoutEndAt = DateTime.UtcNow.AddMinutes(10);
        await context.SaveChangesAsync();
        var publisher = new FakeMessagePublisher();
        var service = AuthServiceTests.NewService(context, publisher);
        await service.ForgotPasswordAsync(new ForgotPasswordRequest { Email = "amina@torvin.local" });
        var code = LastCode(publisher);

        await service.ResetPasswordAsync(Reset(code));

        Assert.Equal(AppRoles.User, (await service.LoginAsync(Login(NewPassword))).Role);
        await Assert.ThrowsAsync<BusinessException>(() => service.LoginAsync(Login(TestUsers.Password)));
        var again = await Assert.ThrowsAsync<BusinessException>(() => service.ResetPasswordAsync(Reset(code, "Torvin.2028!")));
        Assert.Equal(AuthService.InvalidCode, again.Message);
    }
}
