using GrillMaster.Models;
using Microsoft.AspNetCore.Identity;

namespace GrillMaster.Data
{
    public static class IdentitySeeder
    {
        public static async Task SeedAsync(IServiceProvider services)
        {
            var roleManager =
                services.GetRequiredService<RoleManager<IdentityRole>>();

            var userManager =
                services.GetRequiredService<UserManager<ApplicationUser>>();

            // =========================
            // CREATE ROLES
            // =========================

            const string adminRole = "Admin";
            const string userRole = "User";

            if (!await roleManager.RoleExistsAsync(adminRole))
            {
                await roleManager.CreateAsync(
                    new IdentityRole(adminRole));
            }

            if (!await roleManager.RoleExistsAsync(userRole))
            {
                await roleManager.CreateAsync(
                    new IdentityRole(userRole));
            }

            // =========================
            // ADMIN ACCOUNT
            // =========================

            const string adminEmail = "admin@grillmaster.com";
            const string adminPassword = "Admin@123456";
            const string adminFullName = "GrillMaster Admin";

            var adminUser =
                await userManager.FindByEmailAsync(adminEmail);

            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true,
                    FullName = adminFullName
                };

                var result = await userManager.CreateAsync(
                    adminUser,
                    adminPassword);

                if (!result.Succeeded)
                {
                    var errors = string.Join(
                        ", ",
                        result.Errors.Select(e => e.Description));

                    throw new Exception(
                        $"Failed to create admin user: {errors}");
                }
            }
            else
            {
                // Make sure existing admin has a display name.
                if (string.IsNullOrWhiteSpace(adminUser.FullName))
                {
                    adminUser.FullName = adminFullName;

                    await userManager.UpdateAsync(adminUser);
                }
            }

            // =========================
            // ASSIGN ADMIN ROLE
            // =========================

            if (!await userManager.IsInRoleAsync(
                    adminUser,
                    adminRole))
            {
                await userManager.AddToRoleAsync(
                    adminUser,
                    adminRole);
            }
        }
    }
}