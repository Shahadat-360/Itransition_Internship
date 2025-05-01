using FormsApp.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Logging;
using System;

namespace FormsApp.Data
{
    public static class DbInitializer
    {
        public static async Task InitializeAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<ApplicationDbContext>>();
            
            try
            {
                // Apply migrations
                await context.Database.MigrateAsync();
                
                // Reset TopicIds after removing the predefined topics
                await DataMigrations.TopicIdResetUtility.ResetTopicIdsAsync(serviceProvider);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while migrating the database");
                // Allow the app to continue even if migration fails
                // The OnConfiguring method in ApplicationDbContext should suppress the warnings
            }
            
            // Create roles
            await EnsureRoleAsync(roleManager, "Admin", logger);


            // With this:
            var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json")
            .Build();

            // With this:
            string adminEmail = config["AdminCredentials:Email"];
            string adminPassword = config["AdminCredentials:Password"];

            var admin = await userManager.FindByEmailAsync(adminEmail);
            if (admin == null)
            {
                logger.LogInformation("Admin user '{AdminEmail}' not found, creating new user.", adminEmail);
                admin = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true
                };
                
                var createUserResult = await userManager.CreateAsync(admin, adminPassword);
                if (createUserResult.Succeeded)
                {
                    logger.LogInformation("Admin user '{AdminEmail}' created successfully. Assigning 'Admin' role.", adminEmail);
                    var addToRoleResult = await userManager.AddToRoleAsync(admin, "Admin");
                    if (addToRoleResult.Succeeded)
                    {
                        logger.LogInformation("Successfully assigned 'Admin' role to user '{AdminEmail}'.", adminEmail);
                    }
                    else
                    {
                        logger.LogError("Failed to assign 'Admin' role to user '{AdminEmail}'. Errors: {Errors}", adminEmail, string.Join(", ", addToRoleResult.Errors.Select(e => e.Description)));
                    }
                }
                else
                {
                    logger.LogError("Failed to create admin user '{AdminEmail}'. Errors: {Errors}", adminEmail, string.Join(", ", createUserResult.Errors.Select(e => e.Description)));
                }
            }
            else
            {
                logger.LogInformation("Existing admin user '{AdminEmail}' already exists.", adminEmail);
                // Optional: Check if the existing user has the Admin role
                if (!await userManager.IsInRoleAsync(admin, "Admin"))
                {
                    logger.LogWarning("Existing admin user '{AdminEmail}' does NOT have the 'Admin' role. Attempting to assign.", adminEmail);
                    var addToRoleResult = await userManager.AddToRoleAsync(admin, "Admin");
                    if (addToRoleResult.Succeeded)
                    {
                        logger.LogInformation("Successfully assigned 'Admin' role to existing user '{AdminEmail}'.", adminEmail);
                    }
                    else
                    {
                        logger.LogError("Failed to assign 'Admin' role to existing user '{AdminEmail}'. Errors: {Errors}", adminEmail, string.Join(", ", addToRoleResult.Errors.Select(e => e.Description)));
                    }
                }
                else
                {
                    logger.LogInformation("Existing admin user '{AdminEmail}' already has the 'Admin' role.", adminEmail);
                }
            }
            
            // Removed sample tags initialization
            
            await context.SaveChangesAsync();
        }
        
        private static async Task EnsureRoleAsync(RoleManager<IdentityRole> roleManager, string roleName, ILogger logger)
        {
            var roleExists = await roleManager.RoleExistsAsync(roleName);
            logger.LogInformation("Checking for role '{RoleName}'. Exists: {Exists}", roleName, roleExists);

            if (!roleExists)
            {
                logger.LogInformation("Role '{RoleName}' not found. Creating role.", roleName);
                var createRoleResult = await roleManager.CreateAsync(new IdentityRole(roleName));
                if (createRoleResult.Succeeded)
                {
                    logger.LogInformation("Role '{RoleName}' created successfully.", roleName);
                }
                else
                {
                    logger.LogError("Failed to create role '{RoleName}'. Errors: {Errors}", roleName, string.Join(", ", createRoleResult.Errors.Select(e => e.Description)));
                }
            }
        }
    }
} 