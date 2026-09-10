using System.Text.RegularExpressions;

namespace Basora.Core.Tests.Results;

/// <summary>
/// The SQLSTATE table in docs/05-postgresql-data-layer.md section 10, read from the
/// document itself.
/// </summary>
/// <remarks>
/// Copying the table into the test would only prove the copy agrees with the catalogue.
/// Reading the document means adding a row to the spec fails the build until the
/// catalogue covers it, which is the property the acceptance criterion is actually after.
/// </remarks>
internal static partial class SqlStateSpecTable
{
    private const string SpecPath = "docs/05-postgresql-data-layer.md";
    private const string SectionHeading = "## 10. Error mapping";

    /// <summary>Every SQLSTATE the spec table lists, in document order.</summary>
    public static IReadOnlyList<string> SqlStates { get; } = Read();

    private static List<string> Read()
    {
        string path = Path.Combine(RepositoryRoot(), SpecPath);
        List<string> states = [];
        bool inSection = false;

        foreach (string line in File.ReadLines(path))
        {
            if (line.StartsWith("## ", StringComparison.Ordinal))
            {
                if (inSection)
                {
                    break;
                }

                inSection = line.StartsWith(SectionHeading, StringComparison.Ordinal);
                continue;
            }

            if (inSection && RowPattern().Match(line) is { Success: true } row)
            {
                states.Add(row.Groups["state"].Value);
            }
        }

        Assert.NotEmpty(states);
        return states;
    }

    private static string RepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);

        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Basora.slnx")))
        {
            directory = directory.Parent;
        }

        Assert.NotNull(directory);
        return directory.FullName;
    }

    /// <summary>A table row whose first cell is a SQLSTATE in backticks.</summary>
    [GeneratedRegex(@"^\|\s*`(?<state>[0-9A-Z]{5})`\s*\|", RegexOptions.CultureInvariant)]
    private static partial Regex RowPattern();
}
