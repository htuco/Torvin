using System.Globalization;
using Torvin.Model.Validation;

namespace Torvin.Tests;

public class MoneyTests
{
    [Theory]
    [InlineData("0.01")]
    [InlineData("10.5")]
    [InlineData("10.50")]
    [InlineData("10.500")]
    [InlineData("100000")]
    public void ValidAmounts_Pass(string amount) => Assert.True(Money.IsValidAmount(Parse(amount)));

    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("1.005")]
    [InlineData("100000.01")]
    public void InvalidAmounts_Fail(string amount) => Assert.False(Money.IsValidAmount(Parse(amount)));

    private static decimal Parse(string amount) => decimal.Parse(amount, CultureInfo.InvariantCulture);
}
