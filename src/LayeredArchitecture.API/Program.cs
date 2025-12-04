using LayeredArchitecture.API.Extensions;
using LayeredArchitecture.API.Middleware;
using LayeredArchitecture.API.Services.Interceptors;
using LayeredArchitecture.Application;
using LayeredArchitecture.Application.Common.Behaviors;
using LayeredArchitecture.BLL;
using LayeredArchitecture.Common.Configuration;
using LayeredArchitecture.DAL.Data;
using LayeredArchitecture.Infrastructure;
using LayeredArchitecture.Infrastructure.Persistence;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Instrumentation.GrpcNetClient;
using OpenTelemetry.Instrumentation.Process;
using OpenTelemetry.Instrumentation.Runtime;
using OpenTelemetry.Instrumentation.StackExchangeRedis;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Add ServiceDefaults for centralized logging and health checks
builder.AddDefaultHealthChecks();
builder.AddStructuredLogging();

// Configure Serilog (already done in AddStructuredLogging, but keeping for compatibility)
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

builder.Host.UseSerilog();

// Add service discovery
builder.Services.AddServiceDiscovery();

// Configure MongoDB serialization
BsonSerializer.RegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));
BsonSerializer.RegisterSerializer(new DateTimeSerializer(DateTimeKind.Utc));

// Configure MongoDB
builder.Services.Configure<MongoDbSettings>(builder.Configuration.GetSection("MongoDbSettings"));
builder.Services.AddSingleton<IMongoClient>(serviceProvider =>
{
    var settings = serviceProvider.GetRequiredService<IOptions<MongoDbSettings>>().Value;
    return new MongoClient(settings.ConnectionString);
});

builder.Services.AddScoped<IMongoDatabase>(serviceProvider =>
{
    var settings = serviceProvider.GetRequiredService<IOptions<MongoDbSettings>>().Value;
    var client = serviceProvider.GetRequiredService<IMongoClient>();
    return client.GetDatabase(settings.DatabaseName);
});

// Add MediatR with all handlers and behaviors
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(Application.AssemblyReference).Assembly);
    cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
});

// Add FluentValidation validators
builder.Services.AddValidatorsFromAssembly(typeof(Application.AssemblyReference).Assembly);

// Add AutoMapper profiles
builder.Services.AddAutoMapper(typeof(Application.AssemblyReference).Assembly);

// Add health checks for MongoDB and Redis
builder.Services.AddHealthChecks()
    .AddMongoDb(
        mongodbConnectionString: builder.Configuration.GetSection("MongoDbSettings:ConnectionString").Value ?? "mongodb://localhost:27017",
        mongoDatabaseName: builder.Configuration.GetSection("MongoDbSettings:DatabaseName").Value ?? "LayeredArchitectureDB",
        name: "mongodb",
        failureStatus: HealthStatus.Unhealthy,
        tags: new[] { "mongodb", "database" })
    .AddRedis("redis", tags: new[] { "redis", "cache" })
    .AddRabbitMQ("rabbitmq", tags: new[] { "rabbitmq", "messaging" });

// Add Entity Framework Core (keeping for compatibility during migration)
builder.Services.AddDbContext<BudMasterDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add services to the container.
builder.Services.Configure<ConnectionStrings>(builder.Configuration.GetSection("ConnectionStrings"));

// Register Infrastructure services (MongoDB)
builder.Services.AddInfrastructure(builder.Configuration);

// Register custom services
builder.Services.AddApplicationServices();
builder.Services.AddDatabaseServices();
builder.Services.AddBusinessServices();

// Register caching services
builder.Services.AddScoped<Services.Caching.ICacheService, Services.Caching.CacheService>();
builder.Services.AddScoped<Services.Caching.IDistributedCacheService, Services.Caching.DistributedCacheService>();
builder.Services.AddHostedService<Services.Caching.CacheWarmupService>();
builder.Services.AddSingleton<Services.Caching.ICacheWarmupService, Services.Caching.CacheWarmupService>();

// Add controllers
builder.Services.AddControllers();

// Add gRPC services
builder.Services.AddGrpc(options =>
{
    options.Interceptors.Add<TelemetryGrpcInterceptor>();
});

// Configure HTTP/2 support for gRPC
builder.Services.Configure<KestrelServerOptions>(options =>
{
    options.AllowSynchronousIO = true;
    options.Limits.MaxRequestBodySize = 32 * 1024 * 1024; // 32MB
});

// Add in-memory caching with custom options
builder.Services.AddMemoryCache(options =>
{
    options.SizeLimit = 1000; // Maximum number of cache entries
    options.CompactionPercentage = 0.25; // Compact 25% of entries when size limit is reached
    options.ExpirationScanFrequency = TimeSpan.FromMinutes(5); // Scan for expired items every 5 minutes
});

// Add Redis distributed caching with Aspire integration
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("redis");
    options.InstanceName = "LayeredArchitecture";
});

// Add RabbitMQ client with Aspire integration
builder.Services.AddRabbitMQClient("rabbitmq", configureConnectionFactory: factory =>
{
    factory.ClientProvidedName = "LayeredArchitecture.API";
    factory.AutomaticRecoveryEnabled = true;
    factory.NetworkRecoveryInterval = TimeSpan.FromSeconds(10);
    factory.RequestedHeartbeat = TimeSpan.FromSeconds(60);
});

// Configure OpenTelemetry for gRPC observability
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing =>
    {
        tracing.AddAspNetCoreInstrumentation()
               .AddGrpcClientInstrumentation()
               .AddGrpcCoreInstrumentation()
               .AddHttpClientInstrumentation()
               .AddRedisInstrumentation()
               .AddEntityFrameworkCoreInstrumentation()
               .AddSource("LayeredArchitecture.API") // Custom activity source
               .SetResourceBuilder(ResourceBuilder.CreateDefault()
                   .AddService("LayeredArchitecture.API", "1.0.0"))
               .AddConsoleExporter() // For development
               .AddOtlpExporter(options =>
               {
                   options.Endpoint = new Uri(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"] ?? "http://localhost:4317");
               });
    })
    .WithMetrics(metrics =>
    {
        metrics.AddAspNetCoreInstrumentation()
               .AddHttpClientInstrumentation()
               .AddRuntimeInstrumentation()
               .AddProcessInstrumentation()
               .AddMeter("LayeredArchitecture.API") // Custom metrics
               .AddView("cache-hit-ratio", new ExplicitBucketHistogramConfiguration
               {
                   Boundaries = new double[] { 0.0, 0.1, 0.2, 0.3, 0.4, 0.5, 0.6, 0.7, 0.8, 0.9, 1.0 }
               })
               .AddConsoleExporter() // For development
               .AddOtlpExporter(options =>
               {
                   options.Endpoint = new Uri(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"] ?? "http://localhost:4317");
               });
    })
    .WithLogging(logging =>
    {
        logging.AddConsoleExporter() // For development
               .AddOtlpExporter(options =>
               {
                   options.Endpoint = new Uri(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"] ?? "http://localhost:4317");
               });
    });

// Configure ProblemDetails
builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = ctx =>
    {
        ctx.ProblemDetails.Extensions["traceId"] = ctx.HttpContext.TraceIdentifier;
        ctx.ProblemDetails.Extensions["timestamp"] = DateTime.UtcNow;
        
        if (ctx.HttpContext.RequestServices.GetService<IWebHostEnvironment>()?.IsDevelopment() == true)
        {
            ctx.ProblemDetails.Extensions["environment"] = "Development";
        }
    };
});

// Configure API versioning and documentation
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Layered Architecture API V1");
        c.RoutePrefix = string.Empty;
    });
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

// Map gRPC services
app.MapGrpcService<Services.GrpcServices.ProductGrpcService>();
app.MapGrpcService<Services.GrpcServices.CustomerGrpcService>();
app.MapGrpcService<Services.GrpcServices.OrderGrpcService>();
app.MapGrpcService<Services.AggregatorService>();

// Global error handling
app.UseMiddleware<GlobalExceptionHandlingMiddleware>();

// Map default endpoints (health checks)
app.MapDefaultEndpoints();

app.Run();