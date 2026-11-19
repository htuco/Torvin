using Microsoft.EntityFrameworkCore;
using Torvin.Model.Constants;
using Torvin.Model.Enums;
using Torvin.Model.Exceptions;
using Torvin.Model.Messages;
using Torvin.Model.Requests;
using Torvin.Services.Database;
using Torvin.Services.Services;
using static Torvin.Tests.AuthFlowTestHelpers;

namespace Torvin.Tests;

public class RegistrationTests
{
    private const string Password = "Torvin.2026!";

    private static RegisterRequest Request(string username = "Amina.H", string email = "Amina@Torvin.Local") => new()
    {
        FirstName = "Amina", LastName = "Hodžić", Username = username, Email = email,
        Password = Password, ConfirmPassword = Password,
    };

    [Fact]
    public async Task Register_CreatesUnconfirmedUser_AndPublishesOnlyAfterSave()
    {
        var options = TestDatabase.CreateOptions();
        await using var context = await NewContextAsync(options);
        var publisher = new FakeMessagePublisher();
        var savedWhenPublished = false;
        publisher.OnPublish = async _ =>
        {
            await using var other = new TorvinDbContext(options);
            savedWhenPublished = await other.Users.AnyAsync(u => u.Username == "amina.h");
        };

        var response = await AuthServiceTests.NewService(context, publisher).RegisterAsync(Request());

        Assert.Equal(AuthService.Registered, response.Message);
        Assert.True(savedWhenPublished);
        var user = await context.Users.Include(u => u.Role).SingleAsync(u => u.Username == "amina.h");
        Assert.Equal("amina@torvin.local", user.Email);
        Assert.Equal(AppRoles.User, user.Role.Name);
        Assert.False(user.EmailConfirmed);
        var (queue, message) = Assert.Single(publisher.Published);
        Assert.Equal(QueueNames.Email, queue);
        Assert.Equal("amina@torvin.local", Assert.IsType<EmailRequested>(message).To);
    }

    [Fact]
    public async Task TakenUsernameAndEmail_ReturnBothFieldErrors()
    {
        await using var context = await NewContextAsync(TestDatabase.CreateOptions());
        var service = AuthServiceTests.NewService(context);
        await service.RegisterAsync(Request());

        var exception = await Assert.ThrowsAsync<ValidationException>(() => service.RegisterAsync(Request()));

        Assert.Equal([AuthService.UsernameTaken], exception.Errors["username"]);
        Assert.Equal([AuthService.EmailTaken], exception.Errors["email"]);
    }

    [Theory]
    [InlineData("Amina.H2026!")] // korisničko ime
    [InlineData("Xamina@1Torvin")] // dio emaila prije @
    public async Task PasswordContainingIdentity_IsRejected(string password)
    {
        await using var context = await NewContextAsync(TestDatabase.CreateOptions());
        var request = Request();
        request.Password = request.ConfirmPassword = password;

        var exception = await Assert.ThrowsAsync<ValidationException>(() =>
            AuthServiceTests.NewService(context).RegisterAsync(request));

        Assert.Equal([AuthService.PasswordContainsIdentity], exception.Errors["password"]);
        Assert.False(await context.Users.AnyAsync());
    }

    [Fact]
    public async Task VerifyEmail_ConfirmsAccount_AndLoginWorks()
    {
        await using var context = await NewContextAsync(TestDatabase.CreateOptions());
        var publisher = new FakeMessagePublisher();
        var service = AuthServiceTests.NewService(context, publisher);
        await service.RegisterAsync(Request());

        await service.VerifyEmailAsync(new VerifyEmailRequest { Email = "amina@torvin.local", Code = LastCode(publisher) });

        var login = await service.LoginAsync(new LoginRequest { Login = "amina.h", Password = Password, App = ClientApp.Mobile });
        Assert.Equal(AppRoles.User, login.Role);
    }

    [Theory]
    [InlineData("amina@torvin.local", false)] // pogrešan kod
    [InlineData("nema@torvin.local", true)] // nepostojeći email
    public async Task VerifyEmail_FailuresShareOneMessage(string email, bool useRealCode)
    {
        await using var context = await NewContextAsync(TestDatabase.CreateOptions());
        var publisher = new FakeMessagePublisher();
        var service = AuthServiceTests.NewService(context, publisher);
        await service.RegisterAsync(Request());
        var code = LastCode(publisher);
        var wrong = code == "000000" ? "111111" : "000000";

        var exception = await Assert.ThrowsAsync<BusinessException>(() =>
            service.VerifyEmailAsync(new VerifyEmailRequest { Email = email, Code = useRealCode ? code : wrong }));

        Assert.Equal(AuthService.InvalidCode, exception.Message);
    }
}
