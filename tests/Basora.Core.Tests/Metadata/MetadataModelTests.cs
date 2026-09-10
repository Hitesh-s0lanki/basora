using Basora.Core.Models.Metadata;

namespace Basora.Core.Tests.Metadata;

public sealed class MetadataModelTests
{
    private static PgType Text => new()
    {
        Name = "text",
        DisplayName = "text",
        Category = PgTypeCategory.Text,
    };

    private static ColumnDescriptor Column(string name, int position = 1) => new()
    {
        Name = name,
        OrdinalPosition = position,
        Type = Text,
    };

    [Fact]
    public void FindColumn_MatchesExactly_BecausePostgresqlNamesAreCaseSensitive()
    {
        TableDescriptor table = new()
        {
            Ref = new DbObjectRef(DbObjectKind.Table, "public", "orders"),
            Columns = [Column("id"), Column("Total", 2)],
        };

        Assert.NotNull(table.FindColumn("Total"));
        Assert.Null(table.FindColumn("total"));
        Assert.Null(table.FindColumn("missing"));
    }

    [Fact]
    public void HasPrimaryKey_FollowsTheKeyColumnsRatherThanTheConstraintList()
    {
        DbObjectRef reference = new(DbObjectKind.Table, "public", "orders");

        TableDescriptor keyed = new() { Ref = reference, Columns = [Column("id")], PrimaryKeyColumns = ["id"] };
        TableDescriptor keyless = new() { Ref = reference, Columns = [Column("id")] };

        Assert.True(keyed.HasPrimaryKey);
        Assert.False(keyless.HasPrimaryKey);
    }

    [Fact]
    public void TableDescriptor_DefaultsEveryOptionalCollectionToEmpty()
    {
        TableDescriptor table = new()
        {
            Ref = new DbObjectRef(DbObjectKind.View, "public", "order_summary"),
            Columns = [],
        };

        // A view has no indexes or triggers. Those come back empty rather than null, so
        // the structure view can render one code path for every relation kind.
        Assert.Empty(table.Indexes);
        Assert.Empty(table.Constraints);
        Assert.Empty(table.OutgoingForeignKeys);
        Assert.Empty(table.IncomingForeignKeys);
        Assert.Empty(table.Triggers);
        Assert.Empty(table.PrimaryKeyColumns);
        Assert.Null(table.Statistics);
    }

    [Theory]
    [InlineData(IdentityKind.None, null, true)]
    [InlineData(IdentityKind.ByDefault, null, true)]
    [InlineData(IdentityKind.Serial, null, true)]
    [InlineData(IdentityKind.Always, null, false)]
    [InlineData(IdentityKind.None, "price * quantity", false)]
    public void IsWritable_ExcludesTheColumnsTheServerComputes(
        IdentityKind identity,
        string? generated,
        bool expected)
    {
        ColumnDescriptor column = Column("value") with
        {
            Identity = identity,
            GeneratedExpression = generated,
        };

        Assert.Equal(expected, column.IsWritable);
    }

    [Fact]
    public void IsSelfReference_IsTrueWhenAForeignKeyPointsAtItsOwnTable()
    {
        DbObjectRef employees = new(DbObjectKind.Table, "public", "employees");
        DbObjectRef departments = new(DbObjectKind.Table, "public", "departments");

        ForeignKeyDescriptor manager = new()
        {
            Name = "employees_manager_id_fkey",
            SourceTable = employees,
            SourceColumns = ["manager_id"],
            TargetTable = employees,
            TargetColumns = ["id"],
        };

        Assert.True(manager.IsSelfReference);
        Assert.False((manager with { TargetTable = departments }).IsSelfReference);
    }

    [Fact]
    public void HasEverBeenAnalyzed_AcceptsEitherAManualOrAnAutomaticAnalyze()
    {
        TableStatistics never = new();
        TableStatistics manual = new() { LastAnalyze = DateTimeOffset.UnixEpoch };
        TableStatistics automatic = new() { LastAutoAnalyze = DateTimeOffset.UnixEpoch };

        Assert.False(never.HasEverBeenAnalyzed);
        Assert.True(manual.HasEverBeenAnalyzed);
        Assert.True(automatic.HasEverBeenAnalyzed);
    }

    [Fact]
    public void EveryStatistic_IsAbsentByDefaultRatherThanZero()
    {
        // A role that cannot read pg_stat_user_tables must not look like a table with no
        // rows and no scans, or the index analyzer will advise dropping working indexes.
        TableStatistics unavailable = new();

        Assert.Null(unavailable.EstimatedRows);
        Assert.Null(unavailable.LiveTuples);
        Assert.Null(unavailable.DeadTuples);
        Assert.Null(unavailable.TotalSizeBytes);
        Assert.Null(unavailable.SequentialScans);
        Assert.Null(unavailable.IndexScans);
        Assert.Null(unavailable.StatisticsResetAt);
    }

    [Fact]
    public void MetadataAspect_All_CoversEveryDeclaredAspect()
    {
        MetadataAspect covered = Enum.GetValues<MetadataAspect>()
            .Where(aspect => aspect is not MetadataAspect.None and not MetadataAspect.All)
            .Aggregate(MetadataAspect.None, (combined, aspect) => combined | aspect);

        Assert.Equal(MetadataAspect.All, covered);
    }

    [Fact]
    public void MetadataAspect_MembersArePowersOfTwo_SoTheyCombineWithoutOverlapping()
    {
        int[] overlapping =
        [
            .. Enum.GetValues<MetadataAspect>()
                .Where(aspect => aspect is not MetadataAspect.None and not MetadataAspect.All)
                .Select(aspect => (int)aspect)
                .Where(value => (value & (value - 1)) != 0),
        ];

        Assert.Empty(overlapping);
    }

    [Fact]
    public void MetadataCacheKey_IsKeyedBySessionSoEntriesCannotCrossSessions()
    {
        DbObjectRef orders = new(DbObjectKind.Table, "public", "orders");
        Guid session = Guid.NewGuid();

        MetadataCacheKey key = new(session, orders, MetadataAspect.Columns);

        Assert.Equal(key, new MetadataCacheKey(session, orders, MetadataAspect.Columns));
        Assert.NotEqual(key, new MetadataCacheKey(Guid.NewGuid(), orders, MetadataAspect.Columns));
        Assert.NotEqual(key, new MetadataCacheKey(session, orders, MetadataAspect.Indexes));
    }

    [Fact]
    public void ObjectSearchOptions_DefaultsToABoundedSearchOfVisibleObjects()
    {
        ObjectSearchOptions options = new();

        Assert.Equal(50, options.Limit);
        Assert.False(options.IncludeSystemObjects);
        Assert.Empty(options.Kinds);
        Assert.Empty(options.Schemas);
    }

    [Fact]
    public void ObjectNode_DefaultsItsBadgesToEmpty()
    {
        ObjectNode node = new()
        {
            Ref = new DbObjectRef(DbObjectKind.Table, "public", "orders"),
            DisplayName = "orders",
        };

        Assert.Empty(node.Badges);
        Assert.Null(node.EstimatedRows);
    }
}
