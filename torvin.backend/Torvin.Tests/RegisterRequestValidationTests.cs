using System.ComponentModel.DataAnnotations;
using Torvin.Model.Requests;

namespace Torvin.Tests;

public class RegisterRequestValidationTests
{
    private static List<ValidationResult> Validate(string? phone)
    {
        var request = new RegisterRequest
        {
            FirstName = "Amina", LastName = "Hodžić", Username = "amina", Email = "amina@torvin.local",
            PhoneNumber = phone, Password = "Torvin.2026!", ConfirmPassword = "Torvin.2026!",
        };
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(request, new ValidationContext(request), results, validateAllProperties: true);
        return results;
    }

    [Theory]
    [InlineData(null)]
    [InlineData("+387 61 234 567")]
    [InlineData("061234567")]
    public void ValidPhone_Passes(string? phone) => Assert.Empty(Validate(phone));

    [Theory]
    [InlineData("1     ")] // jedna cifra i razmaci
    [InlineData("12345")] // premalo cifara
    [InlineData("+387  61")] // dupli razmak
    [InlineData("061-234-567")] // crtice
    [InlineData("1234567890123456")] // više od 15 cifara
    public void InvalidPhone_Fails(string phone) =>
        Assert.Contains(Validate(phone), r => r.MemberNames.Contains(nameof(RegisterRequest.PhoneNumber)));
}
