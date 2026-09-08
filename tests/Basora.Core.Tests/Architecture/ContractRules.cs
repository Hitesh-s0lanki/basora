using System.Reflection;
using System.Xml.Linq;

namespace Basora.Core.Tests.Architecture;

/// <summary>
/// The two conventions from docs/12-coding-standards.md section 2 that reflection can
/// check: cancellable async contracts, and inheritance that was actually intended.
/// </summary>
/// <remarks>
/// Both are pure functions over types so they can be pointed at a deliberately bad type
/// in a test as well as at the real assemblies.
/// </remarks>
internal static class ContractRules
{
    /// <summary>
    /// Every public async method on a contract takes a <see cref="CancellationToken"/>
    /// last, and — because an interface must not let a caller quietly opt out of
    /// cancellation — without a default value.
    /// </summary>
    public static IReadOnlyList<string> AsyncMethodsMissingCancellationToken(IEnumerable<Type> interfaces)
    {
        List<string> violations = [];

        foreach (Type type in interfaces.Where(t => t.IsInterface))
        {
            foreach (MethodInfo method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance))
            {
                if (!IsAsyncShaped(method.ReturnType))
                {
                    continue;
                }

                ParameterInfo[] parameters = method.GetParameters();
                ParameterInfo? last = parameters.LastOrDefault();

                if (last?.ParameterType != typeof(CancellationToken))
                {
                    violations.Add(
                        $"{type.FullName}.{method.Name} returns {Describe(method.ReturnType)} but its last "
                        + "parameter is not a CancellationToken.");
                }
                else if (last.HasDefaultValue)
                {
                    violations.Add(
                        $"{type.FullName}.{method.Name} gives its CancellationToken a default value; "
                        + "interfaces must not let a caller opt out of cancellation by omission.");
                }
            }
        }

        return violations;
    }

    /// <summary>
    /// Every public class is sealed, static, or carries a documented reason for being
    /// open — the "designing for inheritance requires a comment saying why" rule.
    /// </summary>
    public static IReadOnlyList<string> UnsealedTypesWithoutJustification(
        IEnumerable<Type> types,
        JustificationDocumentation documentation)
    {
        List<string> violations = [];

        foreach (Type type in types)
        {
            if (!type.IsClass || type.IsSealed || IsCompilerGenerated(type))
            {
                // A static class is abstract and sealed, so it is covered by IsSealed.
                continue;
            }

            if (!documentation.HasJustification(type))
            {
                violations.Add(
                    $"{type.FullName} is not sealed and carries no <remarks> explaining why it is open "
                    + "for inheritance.");
            }
        }

        return violations;
    }

    private static bool IsAsyncShaped(Type returnType)
    {
        if (returnType == typeof(Task) || returnType == typeof(ValueTask))
        {
            return true;
        }

        if (!returnType.IsGenericType)
        {
            return false;
        }

        Type definition = returnType.GetGenericTypeDefinition();
        return definition == typeof(Task<>)
            || definition == typeof(ValueTask<>)
            || definition == typeof(IAsyncEnumerable<>);
    }

    private static bool IsCompilerGenerated(Type type) =>
        type.IsDefined(typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute), inherit: false);

    private static string Describe(Type type) => type.IsGenericType
        ? type.GetGenericTypeDefinition().Name
        : type.Name;
}

/// <summary>
/// The XML documentation an assembly ships, read for the one thing an architecture test
/// can check about a comment: that the author wrote a <c>&lt;remarks&gt;</c> saying why a
/// type is open for inheritance.
/// </summary>
/// <remarks>
/// GenerateDocumentationFile is on for every project, so the compiler puts these next to
/// the assembly and the build copies them into the test output.
/// </remarks>
internal sealed class JustificationDocumentation
{
    private readonly HashSet<string> _documentedMembers;

    private JustificationDocumentation(HashSet<string> documentedMembers) =>
        _documentedMembers = documentedMembers;

    public static JustificationDocumentation ForAssemblies(IEnumerable<string> assemblyNames)
    {
        HashSet<string> documented = new(StringComparer.Ordinal);

        foreach (string assemblyName in assemblyNames)
        {
            string path = Path.ChangeExtension(BasoraAssemblies.PathOf(assemblyName), ".xml");
            if (!File.Exists(path))
            {
                continue;
            }

            foreach (XElement member in XDocument.Load(path).Descendants("member"))
            {
                string? name = member.Attribute("name")?.Value;
                if (name is not null && member.Element("remarks") is not null)
                {
                    documented.Add(name);
                }
            }
        }

        return new JustificationDocumentation(documented);
    }

    public bool HasJustification(Type type) => _documentedMembers.Contains($"T:{type.FullName}");
}
