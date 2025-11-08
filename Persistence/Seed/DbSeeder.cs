using Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Persistence.Seed
{
    public static class DbSeeder
    {
        public static async Task SeedRolesAndSuperAdminAsync(
            IServiceProvider serviceProvider, IConfiguration config)
        {
            using var scope = serviceProvider.CreateScope();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            // 1. Seed roles
            string[] roles = { UserRole.User, UserRole.Coach, UserRole.Admin, UserRole.SuperAdmin };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                    await roleManager.CreateAsync(new IdentityRole(role));
            }

            // 2. Seed SuperAdmin
            var email = config["SuperAdmin:Email"];
            var password = config["SuperAdmin:Password"];

            var superAdmin = await userManager.FindByEmailAsync(email);
            if (superAdmin == null)
            {
                superAdmin = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true,
                    IsApproved = true,   // ✅ explicitly approved
                    Role = UserRole.SuperAdmin
                };
                var result = await userManager.CreateAsync(superAdmin, password);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(superAdmin, UserRole.SuperAdmin);
                }
            }
            else
            {
                // Ensure existing superadmin has the right role & approval
                if (!await userManager.IsInRoleAsync(superAdmin, UserRole.SuperAdmin))
                    await userManager.AddToRoleAsync(superAdmin, UserRole.SuperAdmin);

                if (!superAdmin.IsApproved)
                {
                    superAdmin.IsApproved = true;
                    await userManager.UpdateAsync(superAdmin);
                }
            }
        }
    }

}
