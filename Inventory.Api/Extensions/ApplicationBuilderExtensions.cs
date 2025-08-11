using Inventory.Api.Data;

namespace Inventory.Api.Extensions;

// Extension methods for configuring the application pipeline.
// Organizes middleware configuration to keep Program.cs clean and maintainable.
public static class ApplicationBuilderExtensions
{
    // Configures Swagger UI for API documentation in development environment.
    // Sets up Swagger endpoint, UI customization, and developer-friendly options.
    // Only enabled in development to avoid exposing API documentation in production.
    public static WebApplication ConfigureSwagger(this WebApplication app)
    {
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

        return app;
    }

    // Configures the HTTP request pipeline with proper middleware ordering.
    // Sets up HTTPS redirection (production only), authentication, authorization, and routing.
    // Middleware order is critical: Authentication must come before Authorization.
    public static WebApplication ConfigurePipeline(this WebApplication app)
    {
        // Only use HTTPS redirection in production
        if (!app.Environment.IsDevelopment())
        {
            app.UseHttpsRedirection();
        }

        // IMPORTANT: Middleware order matters!
        app.UseAuthentication(); // Must come before UseAuthorization
        app.UseAuthorization();  // Must come before MapControllers

        // Map controllers
        app.MapControllers();

        return app;
    }

    // Seeds the database with initial data including default roles and admin user.
    // Creates a service scope to access scoped services during application startup.
    // Ensures the database has required data for the application to function properly.
    public static async Task<WebApplication> SeedDatabaseAsync(this WebApplication app)
    {
        using (var scope = app.Services.CreateScope())
        {
            var seeder = scope.ServiceProvider.GetRequiredService<InventoryContextSeedData>();
            await seeder.SeedAsync();
        }

        return app;
    }
}
