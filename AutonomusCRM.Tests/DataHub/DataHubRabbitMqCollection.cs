using AutonomusCRM.Tests.Integration;
using Xunit;

namespace AutonomusCRM.Tests.DataHub;

[CollectionDefinition("DataHubRabbitMqSerial", DisableParallelization = true)]
public sealed class DataHubRabbitMqCollection : ICollectionFixture<PostgresTestFixture>;
