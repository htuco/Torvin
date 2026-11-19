using System.ComponentModel.DataAnnotations;
using Torvin.Model.Enums;
using Torvin.Model.Validation;

namespace Torvin.Tests;

public class CardNumberTests
{
    [Theory]
    [InlineData("4111111111111111", CardNetwork.Visa)]
    [InlineData("4012888888881881", CardNetwork.Visa)]
    [InlineData("5555555555554444", CardNetwork.Mastercard)]
    [InlineData("2223003122003222", CardNetwork.Mastercard)]
    public void ValidNumber_PassesLuhn_AndHasNetwork(string number, CardNetwork network)
    {
        Assert.True(CardNumber.IsValidLuhn(number));
        Assert.Equal(network, CardNumber.DetectNetwork(number));
    }

    [Theory]
    [InlineData("4111111111111112")]
    [InlineData("5555555555554440")]
    [InlineData("41111111111111a1")]
    public void WrongCheckDigitOrLetter_FailsLuhn(string number) => Assert.False(CardNumber.IsValidLuhn(number));

    // Amex (37), Discover (6011) i Mastercard rubovi izvan raspona (56, 2220, 2721)
    [Theory]
    [InlineData("378282246310005")]
    [InlineData("6011111111111117")]
    [InlineData("5610591081018250")]
    [InlineData("2220990000000000")]
    [InlineData("2721000000000000")]
    public void OtherNetwork_IsNotDetected(string number) => Assert.Null(CardNumber.DetectNetwork(number));

    [Theory]
    [InlineData("4111111111111111", null)]
    [InlineData("", null)]
    [InlineData("4111 1111 1111 1111", CardNumberAttribute.InvalidNumber)]
    [InlineData("4111111111111112", CardNumberAttribute.InvalidNumber)]
    [InlineData("6011111111111117", CardNumberAttribute.UnsupportedNetwork)]
    public void Attribute_ReturnsMessageForField(string number, string? message)
    {
        var context = new ValidationContext(new object()) { MemberName = "CardNumber" };

        var result = new CardNumberAttribute().GetValidationResult(number, context);

        Assert.Equal(message, result?.ErrorMessage);
    }
}
