using Microsoft.AspNetCore.Identity;
using webapi.Models.ModelsImpl;

namespace webapi.Data;

public static class DatabaseSeeder
{
    public static async Task SeedAdminUserAsync(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole<Guid>> roleManager,
        ILogger logger,
        IConfiguration configuration)
    {
        const string adminEmail = "s.raess@me.com";
        const string adminRoleName = "Admin";

        try
        {
            // Ensure Admin role exists
            if (!await roleManager.RoleExistsAsync(adminRoleName))
            {
                logger.LogInformation("Creating Admin role...");
                var roleResult = await roleManager.CreateAsync(new IdentityRole<Guid>(adminRoleName));
                if (roleResult.Succeeded)
                {
                    logger.LogInformation("Admin role created successfully");
                }
                else
                {
                    logger.LogError("Failed to create Admin role: {Errors}", string.Join(", ", roleResult.Errors.Select(e => e.Description)));
                    return;
                }
            }

            // Check if admin user exists
            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            if (adminUser == null)
            {
                logger.LogInformation("Creating admin user: {Email}", adminEmail);

                // Get password from environment variable or use default
                var adminPassword = configuration["ADMIN_PASSWORD"] ?? Environment.GetEnvironmentVariable("ADMIN_PASSWORD") ?? "password";

                adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FirstName = "Admin",
                    LastName = "User",
                    EmailConfirmed = true
                };

                var createResult = await userManager.CreateAsync(adminUser, adminPassword);
                if (createResult.Succeeded)
                {
                    logger.LogInformation("Admin user created successfully");
                }
                else
                {
                    logger.LogError("Failed to create admin user: {Errors}", string.Join(", ", createResult.Errors.Select(e => e.Description)));
                    return;
                }
            }

            // Ensure admin user has Admin role
            if (!await userManager.IsInRoleAsync(adminUser, adminRoleName))
            {
                logger.LogInformation("Assigning Admin role to user: {Email}", adminEmail);
                var addRoleResult = await userManager.AddToRoleAsync(adminUser, adminRoleName);
                if (addRoleResult.Succeeded)
                {
                    logger.LogInformation("Admin role assigned successfully");
                }
                else
                {
                    logger.LogError("Failed to assign Admin role: {Errors}", string.Join(", ", addRoleResult.Errors.Select(e => e.Description)));
                }
            }
            else
            {
                logger.LogInformation("Admin user already has Admin role");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while seeding admin user");
        }
    }
}
