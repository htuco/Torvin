using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Torvin.API.Controllers;

namespace Torvin.Tests;

public class ControllerAuthorizationTests
{
    // Pravilo iz uputa §5: svaki controller je zaštićen, a anonimne su samo pojedine akcije auth toka
    [Fact]
    public void EveryApiController_HasAuthorizeOnClass()
    {
        var controllers = typeof(AuthController).Assembly.GetTypes()
            .Where(type => type.IsSubclassOf(typeof(ControllerBase)) && !type.IsAbstract)
            .ToList();

        Assert.NotEmpty(controllers);
        Assert.All(controllers, controller =>
            Assert.True(controller.GetCustomAttribute<AuthorizeAttribute>() is not null,
                $"{controller.Name} nema [Authorize] na klasi."));
    }

    [Fact]
    public void AnonymousActions_AreOnlyInAuthController()
    {
        var anonymous = typeof(AuthController).Assembly.GetTypes()
            .Where(type => type.IsSubclassOf(typeof(ControllerBase)))
            .SelectMany(type => type.GetMethods())
            .Where(method => method.GetCustomAttribute<AllowAnonymousAttribute>() is not null)
            .ToList();

        Assert.All(anonymous, method => Assert.Equal(typeof(AuthController), method.DeclaringType));
    }
}
