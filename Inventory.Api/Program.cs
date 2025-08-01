using DotNetEnv;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using System.Reflection;
using Inventory.Api.Data;
using Inventory.Api.Interfaces;
using Inventory.Api.Services;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// Load environment variables from .env file only in development
if (builder.Environment.IsDevelopment())
{
    Env.Load("../.env");
}

// Add services to the container
builder.Services.AddControllers();

// Configure ApiBehaviorOptions to disable automatic model validation
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.SuppressModelStateInvalidFilter = true;
});

// Configure Swagger with XML documentation
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Inventory Management API",
        Version = "v1",
        Description = "A comprehensive REST API for managing inventory items with CRUD operations, filtering, pagination, and soft deletion capabilities.",
        Contact = new OpenApiContact
        {
            Name = "RAB.devph",
            Email = "rab.devph@gmail.com"
        }
    });

    // Include XML comments - standard approach
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);

    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }

    // Enhanced documentation configuration
    c.DescribeAllParametersInCamelCase();
    c.UseInlineDefinitionsForEnums();
});

// Configure Entity Framework with PostgreSQL
builder.Services.AddDbContext<InventoryContext>(options =>
{
    // Use in-memory database for testing environment
    if (builder.Environment.IsEnvironment("Testing"))
    {
        options.UseInMemoryDatabase("InMemoryDbForTesting");
    }
    else
    {
        var connectionString = Environment.GetEnvironmentVariable("DATABASE_CONNECTION_STRING")
            ?? builder.Configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DATABASE_CONNECTION_STRING not found in environment variables or appsettings.json");

        options.UseNpgsql(connectionString);
    }
});

// Register application services
builder.Services.AddScoped<IItemService, ItemService>();
builder.Services.AddScoped<IEmployeeService, EmployeeService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger(c =>
    {
        c.RouteTemplate = "swagger/{documentName}/swagger.json";
    });
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Inventory API v1");
        options.RoutePrefix = "swagger";
        options.DocumentTitle = "Inventory Management API Documentation";
        options.DefaultModelsExpandDepth(-1); // Hide models section by default
        options.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.None); // Collapse all endpoints by default
        options.EnableDeepLinking();
        options.EnableFilter();
        options.EnableValidator();
    });
}

// Only use HTTPS redirection in production
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

// Map controllers
app.MapControllers();

app.Run();

// Make the Program class accessible for testing
public partial class Program { }
