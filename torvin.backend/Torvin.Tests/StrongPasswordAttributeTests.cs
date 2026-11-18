using Torvin.Model.Validation;

namespace Torvin.Tests;

public class StrongPasswordAttributeTests
{
    private static readonly StrongPasswordAttribute Attribute = new();

    [Theory]
    [InlineData("Lozinka.1")]
    [InlineData("Ab1!Ab1!")]
    public void StrongPassword_IsValid(string password) => Assert.True(Attribute.IsValid(password));

    [Theory]
    [InlineData("Ab1!Ab1")] // 7 znakova
    [InlineData("lozinka.1")] // bez velikog slova
    [InlineData("LOZINKA.1")] // bez malog slova
    [InlineData("Lozinka.!")] // bez broja
    [InlineData("Lozinka12")] // bez specijalnog znaka
    public void WeakPassword_IsInvalid(string password) => Assert.False(Attribute.IsValid(password));

    [Fact]
    public void LengthLimits_AreInclusive()
    {
        var max = "Aa1!" + new string('x', StrongPasswordAttribute.MaxLength - 4);

        Assert.True(Attribute.IsValid(max));
        Assert.False(Attribute.IsValid(max + "x"));
    }

    [Fact]
    public void Message_DescribesFormat() =>
        Assert.Equal("Lozinka mora imati 8–100 znakova, veliko i malo slovo, broj i specijalni znak.",
            Attribute.FormatErrorMessage("Password"));
}
