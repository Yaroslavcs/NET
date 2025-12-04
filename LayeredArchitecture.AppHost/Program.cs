var builder = DistributedApplication.CreateBuilder(args);

// Add SQL Server container
var sqlServer = builder.AddSqlServer("sqlserver");
var db = sqlServer.AddDatabase("LayeredArchitectureDB");
var identityDb = sqlServer.AddDatabase("IdentityDb"); // Separate database for Identity

// Add Redis container for caching with persistent volume
var redis = builder.AddRedis("redis")
    .WithDataVolume("redis-data", "/data");

// Add RabbitMQ container with persistent volume and management UI
var rabbitMq = builder.AddRabbitMQ("rabbitmq")
    .WithDataVolume("rabbitmq-data", "/var/lib/rabbitmq")
    .WithManagementPlugin(); // Enables web UI on port 15672

// Add the API project with references to SQL Server, Redis, and RabbitMQ
var apiService = builder.AddProject<Projects.LayeredArchitecture_API>("layeredarchitecture-api")
    .WithReference(db)
    .WithReference(identityDb) // Add Identity database reference
    .WithReference(redis)
    .WithReference(rabbitMq)
    .WithReplicas(1);

builder.Build().Run();