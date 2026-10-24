using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Torvin.API.Infrastructure;
using Torvin.Model.Exceptions;
using Torvin.Model.Responses;

namespace Torvin.Tests;

public class InvalidModelStateResponseTests
{
    private static ErrorResponse CreateFor(Action<ModelStateDictionary> fill)
    {
        var context = new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor());
        fill(context.ModelState);

        var result = Assert.IsType<BadRequestObjectResult>(InvalidModelStateResponse.Create(context));
        return Assert.IsType<ErrorResponse>(result.Value);
    }

    [Fact]
    public void ModelStateError_ReturnsErrorResponseWithCamelCaseKeys()
    {
        var response = CreateFor(state =>
        {
            state.AddModelError("Email", "Email nije u ispravnom formatu.");
            state.AddModelError("Address.City", "Grad je obavezan.");
        });

        Assert.Equal(ValidationException.DefaultMessage, response.Message);
        Assert.Equal(["Email nije u ispravnom formatu."], response.Errors!["email"]);
        Assert.Equal(["Grad je obavezan."], response.Errors["address.city"]);
    }

    [Fact]
    public void ParsingError_DoesNotLeakTechnicalMessage()
    {
        var response = CreateFor(state =>
            state.TryAddModelException("Amount", new InvalidOperationException("tehnički detalj")));

        Assert.Equal(["Vrijednost nije ispravna."], response.Errors!["amount"]);
    }
}
