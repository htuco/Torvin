namespace Torvin.Services.Configuration;

public static class DotEnvLoader
{
    private const string RootMarkerFolder = "torvin.backend";
    private const string DevelopmentEnvironment = "Development";

    // Samo za lokalni razvoj: u Dockeru varijable daje compose
    public static void LoadIfDevelopment()
    {
        var environment = Environment.GetEnvironmentVariable(EnvVars.AspNetCoreEnvironment)
            ?? Environment.GetEnvironmentVariable(EnvVars.DotnetEnvironment);
        if (environment != DevelopmentEnvironment)
        {
            return;
        }

        var root = FindRoot(Directory.GetCurrentDirectory());
        if (root is not null)
        {
            Load(Path.Combine(root, ".env"));
        }
    }

    // Varijabla koja već postoji u okruženju ima prednost nad .env
    public static void Load(string path)
    {
        if (!File.Exists(path))
        {
            return;
        }

        foreach (var line in File.ReadLines(path))
        {
            var trimmed = line.Trim();
            if (trimmed.Length == 0 || trimmed.StartsWith('#'))
            {
                continue;
            }

            var separator = trimmed.IndexOf('=');
            if (separator <= 0)
            {
                continue;
            }

            var name = trimmed[..separator].Trim();
            var value = Unquote(trimmed[(separator + 1)..].Trim());
            if (Environment.GetEnvironmentVariable(name) is null)
            {
                Environment.SetEnvironmentVariable(name, value);
            }
        }
    }

    private static string? FindRoot(string startDirectory)
    {
        for (var directory = new DirectoryInfo(startDirectory); directory is not null; directory = directory.Parent)
        {
            if (Directory.Exists(Path.Combine(directory.FullName, RootMarkerFolder)))
            {
                return directory.FullName;
            }
        }

        return null;
    }

    private static string Unquote(string value) =>
        value.Length >= 2 && (value[0] == '"' || value[0] == '\'') && value[^1] == value[0]
            ? value[1..^1]
            : value;
}
