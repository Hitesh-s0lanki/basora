using System.Collections.Immutable;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace Basora.Core.Tests.Architecture;

/// <summary>
/// Locates the shipped assemblies and reads what they actually depend on.
/// </summary>
/// <remarks>
/// References are read out of the PE metadata rather than from the project files. That
/// is the difference that matters: a project file states an intention, while the
/// metadata records what the compiler emitted, so a layer violation cannot hide behind a
/// reference that was declared for some other reason. The declared side is checked
/// separately, at build time, by the BasoraValidateLayerRules target in
/// Directory.Build.props.
/// </remarks>
internal static class BasoraAssemblies
{
    /// <summary>Every assembly Basora ships, in layer order.</summary>
    public static ImmutableArray<string> SourceNames { get; } =
    [
        "Basora.Core",
        "Basora.Infrastructure",
        "Basora.Security",
        "Basora.Analytics",
        "Basora.AI",
        "Basora.PostgreSQL",
        "Basora.UI",
        "Basora.App",
    ];

    /// <summary>Assemblies that are part of the platform rather than part of Basora.</summary>
    public static bool IsPlatformAssembly(string name) =>
        name.StartsWith("System", StringComparison.Ordinal)
        || name is "netstandard" or "mscorlib" or "WindowsBase";

    public static string PathOf(string assemblyName) =>
        Path.Combine(AppContext.BaseDirectory, assemblyName + ".dll");

    public static Assembly Load(string assemblyName) => Assembly.LoadFrom(PathOf(assemblyName));

    /// <summary>
    /// Public types declared by an assembly, tolerating types whose dependencies cannot
    /// be resolved in the test host.
    /// </summary>
    public static IReadOnlyList<Type> PublicTypesOf(string assemblyName)
    {
        try
        {
            return Load(assemblyName).GetExportedTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            return [.. ex.Types.OfType<Type>().Where(t => t.IsPublic)];
        }
    }

    /// <summary>Assembly names in the AssemblyRef table — what the IL genuinely touches.</summary>
    public static IReadOnlyList<string> ReferencedAssembliesOf(string assemblyName)
    {
        using FileStream stream = File.OpenRead(PathOf(assemblyName));
        using var pe = new PEReader(stream);
        MetadataReader metadata = pe.GetMetadataReader();

        return
        [
            .. metadata.AssemblyReferences
                .Select(handle => metadata.GetString(metadata.GetAssemblyReference(handle).Name))
                .Distinct(StringComparer.Ordinal)
                .Order(StringComparer.Ordinal),
        ];
    }

    /// <summary>Namespaces in the TypeRef table — every namespace the IL reaches into.</summary>
    public static IReadOnlyList<string> ReferencedNamespacesOf(string assemblyName)
    {
        using FileStream stream = File.OpenRead(PathOf(assemblyName));
        using var pe = new PEReader(stream);
        MetadataReader metadata = pe.GetMetadataReader();

        return
        [
            .. metadata.TypeReferences
                .Select(handle => metadata.GetString(metadata.GetTypeReference(handle).Namespace))
                .Where(ns => !string.IsNullOrEmpty(ns))
                .Distinct(StringComparer.Ordinal)
                .Order(StringComparer.Ordinal),
        ];
    }
}
