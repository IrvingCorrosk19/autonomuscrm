using AutonomusCRM.Application.DatabaseIntelligence;
using AutonomusCRM.Infrastructure.DatabaseIntelligence.Connectors;

namespace AutonomusCRM.Infrastructure.DatabaseIntelligence;

public sealed class DbConnectorFactory : IDbConnectorFactory
{
    private readonly IReadOnlyDictionary<DbEngineType, IDbConnector> _connectors;

    public DbConnectorFactory()
    {
        IDbConnector[] all =
        [
            new PostgreSqlConnector(),
            new SqlServerConnector(),
            new MySqlConnectorImpl(),
            new MariaDbConnector(),
            new OracleConnector()
        ];
        _connectors = all.ToDictionary(c => c.EngineType);
    }

    public IDbConnector Create(DbEngineType engineType)
    {
        if (!_connectors.TryGetValue(engineType, out var connector))
            throw new DbIntelligenceValidationException($"Unsupported database engine: {engineType}");
        return connector;
    }
}
