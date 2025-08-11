using DotNetEnv;
using Inventory.Api.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Load environment variables from .env file only in development
if (builder.Environment.IsDevelopment())
{
    Env.Load("../.env");
}

// Add services using extension methods
builder.Services.AddControllers();
builder.Services.AddDatabaseServices(builder.Configuration, builder.Environment);
builder.Services.AddIdentityServices();
builder.Services.AddAuthenticationServices();
builder.Services.AddAuthorizationPolicies();
builder.Services.AddSwaggerServices();
builder.Services.AddApplicationServices();

var app = builder.Build();

// Configure pipeline using extension methods
app.ConfigureSwagger();
app.ConfigurePipeline();
await app.SeedDatabaseAsync();

app.Run();

// Make the Program class accessible for testing
public partial class Program { }
