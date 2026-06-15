using AutonomusCRM.Application.DatabaseIntelligence;

namespace AutonomusCRM.Infrastructure.DatabaseIntelligence.Discovery;

public sealed class DbSchemaIntrospectorRegistry
{
    private readonly IReadOnlyDictionary<DbEngineType, IDbSchemaIntrospector> _introspectors;

    public DbSchemaIntrospectorRegistry()
    {
        IDbSchemaIntrospector[] all =
        [
            new PostgreSqlSchemaIntrospector(),
            new SqlServerSchemaIntrospector(),
            new MySqlSchemaIntrospector(),
            new MariaDbSchemaIntrospector(),
            new OracleSchemaIntrospector()
        ];
        _introspectors = all.ToDictionary(i => i.EngineType);
    }

    public IDbSchemaIntrospector Resolve(DbEngineType engineType)
    {
        if (!_introspectors.TryGetValue(engineType, out var introspector))
            throw new DbIntelligenceValidationException($"Unsupported database engine: {engineType}");
        return introspector;
    }
}
