namespace Basora.Core.Models.Metadata;

/// <summary>What a foreign key does when the row it references is updated or deleted.</summary>
public enum ReferentialAction
{
    /// <summary>Raise an error, checked at the end of the statement.</summary>
    NoAction,

    /// <summary>Raise an error immediately, without waiting for the end of the statement.</summary>
    Restrict,

    /// <summary>Apply the same change to the referencing rows.</summary>
    Cascade,

    /// <summary>Set the referencing columns to null.</summary>
    SetNull,

    /// <summary>Set the referencing columns to their defaults.</summary>
    SetDefault,
}
