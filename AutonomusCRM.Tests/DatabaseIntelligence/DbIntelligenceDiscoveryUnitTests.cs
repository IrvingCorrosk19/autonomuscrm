using AutonomusCRM.Application.DatabaseIntelligence;
using AutonomusCRM.Infrastructure.DatabaseIntelligence.Discovery;

namespace AutonomusCRM.Tests.DatabaseIntelligence;

[Trait("Category", "DatabaseIntelligence")]
public class DbIntelligenceDiscoveryUnitTests
{
    [Fact]
    public void SqlGuard_RejectsNonSelect()
    {
        Assert.Throws<InvalidOperationException>(() =>
            DbDiscoverySqlGuard.EnsureReadOnlyMetadataQuery("DELETE FROM users"));
    }

    [Fact]
    public void SqlGuard_RejectsDropInSelect()
    {
        Assert.Throws<InvalidOperationException>(() =>
            DbDiscoverySqlGuard.EnsureReadOnlyMetadataQuery("SELECT 1; DROP TABLE users"));
    }

    [Fact]
    public void SqlGuard_AllowsSelectMetadata()
    {
        DbDiscoverySqlGuard.EnsureReadOnlyMetadataQuery("SELECT table_name FROM information_schema.tables");
    }

    [Fact]
    public void NamingHeuristic_LinksCustomerId_ToCustomersTable()
    {
        var result = new PhysicalSchemaDiscoveryResult
        {
            Tables =
            [
                new PhysicalTableInfo { SchemaName = "public", ObjectName = "orders", ObjectType = DbCatalogObjectTypes.Table },
                new PhysicalTableInfo { SchemaName = "public", ObjectName = "customers", ObjectType = DbCatalogObjectTypes.Table }
            ],
            Columns =
            [
                new PhysicalColumnInfo { SchemaName = "public", ObjectName = "orders", ColumnName = "customer_id", DataType = "uuid", Ordinal = 1 },
                new PhysicalColumnInfo { SchemaName = "public", ObjectName = "customers", ColumnName = "id", DataType = "uuid", IsPrimaryKey = true, Ordinal = 1 }
            ]
        };

        DbRelationshipHeuristics.ApplyNamingHeuristics(result);

        var rel = Assert.Single(result.Relationships);
        Assert.Equal(DbRelationshipSource.NamingHeuristic, rel.Source);
        Assert.InRange(rel.ConfidencePercent, 60, 85);
        Assert.Equal("customers", rel.ToTable);
    }

    [Fact]
    public void SqlServerFixture_MapsTablesColumnsAndRelationships()
    {
        var fixture = SchemaIntrospectionFixtures.SqlServer();
        var mapped = SqlServerSchemaIntrospector.ApplyFixture(fixture);
        Assert.NotEmpty(mapped.Tables);
        Assert.NotEmpty(mapped.Columns);
        Assert.Contains(mapped.Relationships, r => r.Source == DbRelationshipSource.ExplicitForeignKey);
    }

    [Fact]
    public void MySqlFixture_MapsTablesAndIndexes()
    {
        var fixture = SchemaIntrospectionFixtures.MySql();
        var mapped = MySqlSchemaIntrospector.ApplyFixture(fixture);
        Assert.NotEmpty(mapped.Tables);
        Assert.True(mapped.Columns.Any(c => c.IsIndexed));
    }

    [Fact]
    public void OracleFixture_MapsPrimaryKeys()
    {
        var fixture = SchemaIntrospectionFixtures.Oracle();
        var mapped = OracleSchemaIntrospector.ApplyFixture(fixture);
        Assert.Contains(mapped.Columns, c => c.IsPrimaryKey);
    }

    [Fact]
    public void IntrospectorRegistry_ResolvesAllEngines()
    {
        var registry = new DbSchemaIntrospectorRegistry();
        foreach (DbEngineType engine in Enum.GetValues<DbEngineType>())
            Assert.NotNull(registry.Resolve(engine));
    }
}

internal static class SchemaIntrospectionFixtures
{
    public static PhysicalSchemaDiscoveryResult SqlServer() => new()
    {
        Schemas = [new PhysicalSchemaInfo { SchemaName = "dbo" }],
        Tables =
        [
            new PhysicalTableInfo { SchemaName = "dbo", ObjectName = "Customers", ObjectType = DbCatalogObjectTypes.Table, EstimatedRowCount = 100 },
            new PhysicalTableInfo { SchemaName = "dbo", ObjectName = "Orders", ObjectType = DbCatalogObjectTypes.Table, EstimatedRowCount = 250 }
        ],
        Columns =
        [
            new PhysicalColumnInfo { SchemaName = "dbo", ObjectName = "Customers", ColumnName = "Id", DataType = "int", IsPrimaryKey = true, Ordinal = 1 },
            new PhysicalColumnInfo { SchemaName = "dbo", ObjectName = "Orders", ColumnName = "CustomerId", DataType = "int", IsForeignKey = true, Ordinal = 1 },
            new PhysicalColumnInfo { SchemaName = "dbo", ObjectName = "Customers", ColumnName = "Id", DataType = "int", IsPrimaryKey = true, Ordinal = 1 }
        ],
        Relationships =
        [
            new PhysicalRelationshipInfo
            {
                FromSchema = "dbo", FromTable = "Orders", FromColumn = "CustomerId",
                ToSchema = "dbo", ToTable = "Customers", ToColumn = "Id",
                Source = DbRelationshipSource.ExplicitForeignKey, ConfidencePercent = 100
            }
        ],
        Indexes =
        [
            new PhysicalIndexInfo { SchemaName = "dbo", ObjectName = "Orders", IndexName = "IX_Orders_CustomerId", IsUnique = false, ColumnNames = ["CustomerId"] }
        ]
    };

    public static PhysicalSchemaDiscoveryResult MySql() => new()
    {
        Tables =
        [
            new PhysicalTableInfo { SchemaName = "shop", ObjectName = "products", ObjectType = DbCatalogObjectTypes.Table, EstimatedRowCount = 50 }
        ],
        Columns =
        [
            new PhysicalColumnInfo { SchemaName = "shop", ObjectName = "products", ColumnName = "sku", DataType = "varchar", IsPrimaryKey = true, IsIndexed = true, Ordinal = 1 }
        ],
        Indexes =
        [
            new PhysicalIndexInfo { SchemaName = "shop", ObjectName = "products", IndexName = "PRIMARY", IsUnique = true, ColumnNames = ["sku"] }
        ]
    };

    public static PhysicalSchemaDiscoveryResult Oracle() => new()
    {
        Tables =
        [
            new PhysicalTableInfo { SchemaName = "APP", ObjectName = "INVOICES", ObjectType = DbCatalogObjectTypes.Table, EstimatedRowCount = 1000 }
        ],
        Columns =
        [
            new PhysicalColumnInfo { SchemaName = "APP", ObjectName = "INVOICES", ColumnName = "INVOICE_ID", DataType = "NUMBER", IsPrimaryKey = true, Ordinal = 1 }
        ],
        Constraints =
        [
            new PhysicalConstraintInfo { SchemaName = "APP", ObjectName = "INVOICES", ConstraintName = "PK_INVOICES", ConstraintType = "P", ColumnNames = ["INVOICE_ID"] }
        ]
    };
}
