using DotNetEnv;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using System.Reflection;
using Inventory.Api.Data;
using Inventory.Api.Interfaces;
using Inventory.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Inventory.Api.Models;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace Inventory.Api.Extensions;

// Extension methods for configuring services in the DI container.
// Organizes service registration by functionality to keep Program.cs clean and maintainable.
public static class ServiceCollectionExtensions
{
    // Configures Entity Framework DbContext with PostgreSQL or In-Memory database.
    // Uses PostgreSQL for production/development and In-Memory for testing.
    // Connection string is read from environment variables or appsettings.json.
    public static IServiceCollection AddDatabaseServices(this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment environment)
    {
        services.AddDbContext<InventoryContext>(options =>
        {
            // Use in-memory database for testing environment
            if (environment.IsEnvironment("Testing"))
            {
                options.UseInMemoryDatabase("InMemoryDbForTesting");
            }
            else
            {
                var connectionString = Environment.GetEnvironmentVariable("DATABASE_CONNECTION_STRING")
                    ?? configuration.GetConnectionString("DefaultConnection")
                    ?? throw new InvalidOperationException("DATABASE_CONNECTION_STRING not found in environment variables or appsettings.json");

                options.UseNpgsql(connectionString);
            }
        });

        return services;
    }

    // Configures ASP.NET Core Identity with custom User and Role entities.
    // Sets up password requirements, lockout policies, and user settings.
    // Integrates with Entity Framework for data persistence.
    public static IServiceCollection AddIdentityServices(this IServiceCollection services)
    {
        services.AddIdentity<User, Role>(options =>
        {
            // Password settings (adjust as needed)
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequiredLength = 6;

            // Lockout settings
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(30);
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.AllowedForNewUsers = true;

            // User settings
            options.User.RequireUniqueEmail = true;
        })
        .AddEntityFrameworkStores<InventoryContext>()
        .AddDefaultTokenProviders();

        return services;
    }

    // Configures cookie-based authentication for the API.
    // Sets up secure cookie options and handles API-specific authentication flows.
    // Returns 401/403 status codes instead of redirects for API endpoints.
    public static IServiceCollection AddAuthenticationServices(this IServiceCollection services)
    {
        services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.LoginPath = "/api/auth/login";
                options.LogoutPath = "/api/auth/logout";
                options.ExpireTimeSpan = TimeSpan.FromHours(2);
                options.SlidingExpiration = true;
                options.Cookie.HttpOnly = true;
                options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                options.Cookie.SameSite = SameSiteMode.Lax;

                // Return 401/403 for API calls instead of redirects
                options.Events.OnRedirectToLogin = context =>
                {
                    if (context.Request.Path.StartsWithSegments("/api"))
                    {
                        context.Response.StatusCode = 401;
                        return Task.CompletedTask;
                    }
                    context.Response.Redirect(context.RedirectUri);
                    return Task.CompletedTask;
                };

                options.Events.OnRedirectToAccessDenied = context =>
                {
                    if (context.Request.Path.StartsWithSegments("/api"))
                    {
                        context.Response.StatusCode = 403;
                        return Task.CompletedTask;
                    }
                    context.Response.Redirect(context.RedirectUri);
                    return Task.CompletedTask;
                };
            });

        return services;
    }

    // Configures authorization policies for role-based and feature-based access control.
    // Defines hierarchical role permissions: SuperAdmin > Administrator > Supply Officer > Supply Assistant.
    // Uses modern AddAuthorizationBuilder() for fluent policy configuration.
    public static IServiceCollection AddAuthorizationPolicies(this IServiceCollection services)
    {
        services.AddAuthorizationBuilder()
            .AddPolicy("RequireSuperAdminRole", policy =>
                policy.RequireRole("SuperAdmin"))
            .AddPolicy("RequireAdministratorRole", policy =>
                policy.RequireRole("Administrator", "SuperAdmin"))
            .AddPolicy("RequireSupplyOfficerRole", policy =>
                policy.RequireRole("Supply Officer", "Administrator", "SuperAdmin"))
            .AddPolicy("RequireSupplyAssistantRole", policy =>
                policy.RequireRole("Supply Assistant", "Supply Officer", "Administrator", "SuperAdmin"))
            .AddPolicy("CanManageUsers", policy =>
                policy.RequireRole("SuperAdmin"))
            .AddPolicy("CanManageEmployees", policy =>
                policy.RequireRole("Administrator", "SuperAdmin"))
            .AddPolicy("CanManageInventory", policy =>
                policy.RequireRole("Supply Officer", "Administrator", "SuperAdmin"))
            .AddPolicy("CanViewInventory", policy =>
                policy.RequireRole("Supply Assistant", "Supply Officer", "Administrator", "SuperAdmin"))
            .AddPolicy("CanCreateTransactions", policy =>
                policy.RequireRole("Supply Assistant", "Supply Officer", "Administrator", "SuperAdmin"))
            .AddPolicy("CanViewReports", policy =>
                policy.RequireRole("Supply Officer", "Administrator", "SuperAdmin"))
            .AddPolicy("CanDeleteItems", policy =>
                policy.RequireRole("Administrator", "SuperAdmin"))
            .AddPolicy("CanManageRoles", policy =>
                policy.RequireRole("SuperAdmin"))
            .AddPolicy("CanManageSystem", policy =>
                policy.RequireRole("SuperAdmin"));

        return services;
    }

    // Configures Swagger/OpenAPI documentation with authentication support.
    // Includes XML comments, security definitions for cookie authentication,
    // and enhanced UI options for better developer experience.
    public static IServiceCollection AddSwaggerServices(this IServiceCollection services)
    {
        services.AddSwaggerGen(c =>
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

            // Add security definition for cookie authentication
            c.AddSecurityDefinition("Cookie", new OpenApiSecurityScheme
            {
                Name = "Cookie Authentication",
                Type = SecuritySchemeType.ApiKey,
                In = ParameterLocation.Cookie,
                Description = "Cookie-based Authentication"
            });

            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Cookie"
                        }
                    },
                    Array.Empty<string>()
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

        return services;
    }

    // Registers all application-specific services and configurations.
    // Includes business logic services, data seeding, and API behavior options.
    // Disables automatic model validation to allow custom validation handling.
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Configure ApiBehaviorOptions to disable automatic model validation
        services.Configure<ApiBehaviorOptions>(options =>
        {
            options.SuppressModelStateInvalidFilter = true;
        });

        // Register application services
        services.AddScoped<IItemService, ItemService>();
        services.AddScoped<IEmployeeService, EmployeeService>();
        services.AddScoped<IInventoryTransactionService, InventoryTransactionService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddScoped<InventoryContextSeedData>();

        return services;
    }
}
