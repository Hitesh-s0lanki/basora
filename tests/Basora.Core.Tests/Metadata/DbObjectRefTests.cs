using Basora.Core.Models.Metadata;

namespace Basora.Core.Tests.Metadata;

public sealed class DbObjectRefTests
{
    [Fact]
    public void QualifiedName_JoinsTheSchemaAndTheName()
    {
        DbObjectRef reference = new(DbObjectKind.Table, "public", "orders");

        Assert.Equal("public.orders", reference.QualifiedName);
    }

    [Fact]
    public void QualifiedName_IsJustTheNameWhenThereIsNoSchema()
    {
        DbObjectRef database = new(DbObjectKind.Database, null, "app_production");

        Assert.Equal("app_production", database.QualifiedName);
    }

    [Fact]
    public void QualifiedName_DoesNotQuote_BecauseItIsForPeopleRatherThanForSql()
    {
        DbObjectRef awkward = new(DbObjectKind.Table, "my schema", "my table");

        Assert.Equal("my schema.my table", awkward.QualifiedName);
    }

    [Fact]
    public void QuotedName_IsTheFormThatIsSafeInSql()
    {
        DbObjectRef awkward = new(DbObjectKind.Table, "my schema", "my\"table");

        Assert.Equal("\"my schema\".\"my\"\"table\"", awkward.QuotedName);
    }

    [Fact]
    public void QuotedName_OmitsTheSchemaWhenThereIsNone()
    {
        DbObjectRef role = new(DbObjectKind.Role, null, "app");

        Assert.Equal("\"app\"", role.QuotedName);
    }

    [Theory]
    [InlineData(DbObjectKind.Table, true)]
    [InlineData(DbObjectKind.PartitionedTable, true)]
    [InlineData(DbObjectKind.ForeignTable, true)]
    [InlineData(DbObjectKind.View, true)]
    [InlineData(DbObjectKind.MaterializedView, true)]
    [InlineData(DbObjectKind.Index, false)]
    [InlineData(DbObjectKind.Function, false)]
    [InlineData(DbObjectKind.Schema, false)]
    [InlineData(DbObjectKind.Sequence, false)]
    public void IsRelation_IsTrueForExactlyTheKindsThatHoldRows(DbObjectKind kind, bool expected)
    {
        DbObjectRef reference = new(kind, "public", "thing");

        Assert.Equal(expected, reference.IsRelation);
    }

    [Fact]
    public void TwoRefsToTheSameObject_AreEqual()
    {
        DbObjectRef first = new(DbObjectKind.Table, "public", "orders", Oid: 16384);
        DbObjectRef second = new(DbObjectKind.Table, "public", "orders", Oid: 16384);

        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void TheOid_IsPartOfIdentity_SoASavedRefAndALiveOneDoNotSilentlyMerge()
    {
        // A ref restored from a workspace has no OID yet. Treating it as equal to a live
        // one would let a stale cache entry answer for an object nobody has resolved.
        DbObjectRef restored = new(DbObjectKind.Table, "public", "orders");
        DbObjectRef resolved = restored with { Oid = 16384 };

        Assert.NotEqual(restored, resolved);
        Assert.Equal(restored.QualifiedName, resolved.QualifiedName);
    }

    [Fact]
    public void NameIsCaseSensitive_BecausePostgresqlTreatsQuotedNamesThatWay()
    {
        DbObjectRef lower = new(DbObjectKind.Table, "public", "orders");
        DbObjectRef upper = new(DbObjectKind.Table, "public", "Orders");

        Assert.NotEqual(lower, upper);
    }
}
