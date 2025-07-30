using DotNetEnv;
using Microsoft.EntityFrameworkCore;
using Inventory.Api.Data;

var builder = WebApplication.CreateBuilder(args);

// Load environment variables from .env file only in development
if (builder.Environment.IsDevelopment())
{
    Env.Load("../.env");
}

// Add services to the container
builder.Services.AddOpenApi();

// Configure Entity Framework with PostgreSQL
builder.Services.AddDbContext<InventoryContext>(options =>
{
    var connectionString = Environment.GetEnvironmentVariable("DATABASE_CONNECTION_STRING")
        ?? builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("DATABASE_CONNECTION_STRING not found in environment variables or appsettings.json");

    options.UseNpgsql(connectionString);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "Inventory API v1");
    });
}

app.UseHttpsRedirection();

app.Run();
