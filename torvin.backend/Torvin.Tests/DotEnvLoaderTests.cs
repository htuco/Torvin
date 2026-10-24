using Torvin.Services.Configuration;

namespace Torvin.Tests;

public class DotEnvLoaderTests : IDisposable
{
    private const string NewVariable = "TORVIN_TEST_NEW";
    private const string ExistingVariable = "TORVIN_TEST_EXISTING";
    private const string QuotedVariable = "TORVIN_TEST_QUOTED";

    private readonly string _path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.env");

    public void Dispose()
    {
        File.Delete(_path);
        Environment.SetEnvironmentVariable(NewVariable, null);
        Environment.SetEnvironmentVariable(ExistingVariable, null);
        Environment.SetEnvironmentVariable(QuotedVariable, null);
    }

    [Fact]
    public void Load_SetsVariablesAndSkipsCommentsAndBlankLines()
    {
        File.WriteAllText(_path, $"# komentar\n\n{NewVariable}=vrijednost\n{QuotedVariable}=\"s razmakom\"\n");

        DotEnvLoader.Load(_path);

        Assert.Equal("vrijednost", Environment.GetEnvironmentVariable(NewVariable));
        Assert.Equal("s razmakom", Environment.GetEnvironmentVariable(QuotedVariable));
    }

    [Fact]
    public void Load_DoesNotOverrideExistingVariable()
    {
        Environment.SetEnvironmentVariable(ExistingVariable, "iz-okruzenja");
        File.WriteAllText(_path, $"{ExistingVariable}=iz-fajla\n");

        DotEnvLoader.Load(_path);

        Assert.Equal("iz-okruzenja", Environment.GetEnvironmentVariable(ExistingVariable));
    }

    [Fact]
    public void Load_MissingFile_DoesNothing()
    {
        DotEnvLoader.Load(_path);

        Assert.Null(Environment.GetEnvironmentVariable(NewVariable));
    }
}
