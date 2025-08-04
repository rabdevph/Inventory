using Microsoft.AspNetCore.Identity;
using Inventory.Api.Models;

namespace Inventory.Api.Data;

public class InventoryContextSeedData(
    InventoryContext context,
    UserManager<ApplicationUser> userManager,
    RoleManager<ApplicationRole> roleManager,
    ILogger<InventoryContextSeedData> logger)
{
    private readonly InventoryContext _context = context;
    private readonly UserManager<ApplicationUser> _userManager = userManager;
    private readonly RoleManager<ApplicationRole> _roleManager = roleManager;
    private readonly ILogger<InventoryContextSeedData> _logger = logger;

    public async Task SeedAsync()
    {
        var roles = new[] { "Administrator", "Supply Officer", "Supply Assistant" };

        foreach (var role in roles)
        {
            if (!await _roleManager.RoleExistsAsync(role))
            {
                await _roleManager.CreateAsync(new ApplicationRole { Name = role });
                _logger.LogInformation("SEED.ROLE.CREATED: Created role {Role}", role);
            }
            else
            {
                _logger.LogInformation("SEED.ROLE.EXISTS: Role {Role} already exists", role);
            }
        }

        if (!_userManager.Users.Any())
        {
            var adminUser = new ApplicationUser
            {
                UserName = "admin",
                Email = "admin@example.com",
                Firstname = "System",
                LastName = "Administrator",
                EmailConfirmed = true
            };
            await _userManager.CreateAsync(adminUser, "YourSecurePassword123!");
            await _userManager.AddToRoleAsync(adminUser, "Administrator");
            _logger.LogInformation("SEED.USER.CREATED: Created admin user {UserName} and assigned to role {Role}",
                adminUser.UserName, "Administrator");
        }
        else
        {
            _logger.LogInformation("SEED.USER.SKIPPED: Users already exist, no admin user created.");
        }
    }
}
