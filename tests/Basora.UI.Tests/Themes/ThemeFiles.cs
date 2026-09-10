namespace Basora.UI.Tests.Themes;

/// <summary>Locates the theme dictionaries on disk.</summary>
internal static class ThemeFiles
{
    /// <summary>The directory holding the theme dictionaries.</summary>
    public static string Directory { get; } =
        System.IO.Path.Combine(RepositoryRoot(), "src", "Basora.UI", "Themes");

    /// <summary>The full path of one file under <see cref="Directory"/>.</summary>
    public static string Path(string fileName) => System.IO.Path.Combine(Directory, fileName);

    /// <summary>Every XAML file Basora ships that a screen could reference a token from.</summary>
    public static IReadOnlyList<string> AllViewMarkup() =>
    [
        .. System.IO.Directory
            .EnumerateFiles(
                System.IO.Path.Combine(RepositoryRoot(), "src"),
                "*.axaml",
                SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{System.IO.Path.DirectorySeparatorChar}obj{System.IO.Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Where(path => !path.Contains($"{System.IO.Path.DirectorySeparatorChar}bin{System.IO.Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Where(path => System.IO.Path.GetDirectoryName(path) != Directory)
            .Order(StringComparer.Ordinal),
    ];

    private static string RepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);

        while (directory is not null
            && !File.Exists(System.IO.Path.Combine(directory.FullName, "Basora.slnx")))
        {
            directory = directory.Parent;
        }

        Assert.NotNull(directory);
        return directory.FullName;
    }
}
