using LayeredArchitecture.API.Extensions;
using LayeredArchitecture.API.Middleware;
using LayeredArchitecture.BLL;
using LayeredArchitecture.Common.Configuration;
using LayeredArchitecture.DAL.Data;
using LayeredArchitecture.Infrastructure;
using LayeredArchitecture.Application;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

builder.Host.UseSerilog();

// Add service discovery
builder.Services.AddServiceDiscovery();

// Add Redis caching and health checks
builder.Services.AddHealthChecks().AddRedis("redis");

// Add Entity Framework Core (keeping for reference, can be removed if not needed)
builder.Services.AddDbContext<BudMasterDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add services to the container.
builder.Services.Configure<ConnectionStrings>(builder.Configuration.GetSection("ConnectionStrings"));

// Register Clean Architecture layers
builder.Services.AddApplication(); // MediatR and validation
builder.Services.AddInfrastructure(builder.Configuration); // MongoDB repositories

// Register legacy services (keeping for backward compatibility during migration)
builder.Services.AddApplicationServices();
builder.Services.AddDatabaseServices();
builder.Services.AddBusinessServices();

// Add controllers
builder.Services.AddControllers();

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

// Global error handling
app.UseMiddleware<GlobalExceptionHandlingMiddleware>();

app.Run();