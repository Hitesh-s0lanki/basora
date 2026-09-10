using Basora.Core.Models.Metadata;
using Basora.Core.Results;

namespace Basora.Core.Interfaces.Metadata;

/// <summary>
/// Turns an intended schema change into the statements that would make it.
/// </summary>
/// <remarks>
/// Pure and synchronous. It touches no server, which is what lets the structure editor
/// show the exact script before anything runs and what makes golden-file tests the
/// primary gate on it.
/// <para>
/// Generating a script is never the same as running one. Everything here is output for a
/// person to read, and the safety engine annotates it with lock levels and rewrite
/// verdicts before Apply is offered.
/// </para>
/// </remarks>
public interface IDdlGenerator
{
    /// <summary>Generates <c>CREATE TABLE</c> and everything that belongs with it.</summary>
    /// <param name="table">The table to create, described as it should end up.</param>
    Result<IReadOnlyList<DdlStatement>> CreateTable(TableDescriptor table);

    /// <summary>
    /// Generates the statements that turn <paramref name="current"/> into
    /// <paramref name="desired"/>.
    /// </summary>
    /// <remarks>
    /// Renames are passed in rather than inferred. A renamed column and a
    /// dropped-then-added column look identical in a diff, and guessing wrong destroys
    /// the data in it, so the editor says which is which.
    /// </remarks>
    /// <param name="current">The table as it is now.</param>
    /// <param name="desired">The table as it should be.</param>
    /// <param name="renamedColumns">Old name to new name, for every column being renamed.</param>
    Result<IReadOnlyList<DdlStatement>> AlterTable(
        TableDescriptor current,
        TableDescriptor desired,
        IReadOnlyDictionary<string, string> renamedColumns);

    /// <summary>Generates <c>CREATE INDEX</c>.</summary>
    /// <param name="table">The relation to index.</param>
    /// <param name="index">The index to create.</param>
    /// <param name="concurrently">
    /// Whether to build without taking a write lock. Slower and cannot run inside a
    /// transaction, and it is the right default on anything in production.
    /// </param>
    Result<IReadOnlyList<DdlStatement>> CreateIndex(
        DbObjectRef table,
        IndexDescriptor index,
        bool concurrently);

    /// <summary>Generates <c>ALTER TABLE ... ADD CONSTRAINT</c>.</summary>
    /// <param name="table">The relation to constrain.</param>
    /// <param name="constraint">The constraint to add.</param>
    /// <param name="notValid">
    /// Whether to skip checking the rows already there. Adding <c>NOT VALID</c> and
    /// validating separately avoids a long lock on a large table.
    /// </param>
    Result<IReadOnlyList<DdlStatement>> AddConstraint(
        DbObjectRef table,
        ConstraintDescriptor constraint,
        bool notValid);

    /// <summary>Generates a <c>DROP</c> for any object.</summary>
    /// <param name="target">What to drop.</param>
    /// <param name="cascade">
    /// Whether to drop everything depending on it. Rarely what someone means, and the
    /// confirmation dialog lists what would go.
    /// </param>
    Result<IReadOnlyList<DdlStatement>> Drop(DbObjectRef target, bool cascade);
}
