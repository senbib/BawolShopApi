using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using BawolShopApi.Models;

namespace BawolShopApi.Data
{
    public static class SeedData
    {
        public static async Task Initialize(IServiceProvider serviceProvider)
        {
            using (var context = new AppDbContext(
                serviceProvider.GetRequiredService<DbContextOptions<AppDbContext>>()))
            {
                var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

                // ✅ CRÉER LES RÔLES
                string[] roleNames = { "Admin", "Client" };

                foreach (var roleName in roleNames)
                {
                    var roleExist = await roleManager.RoleExistsAsync(roleName);
                    if (!roleExist)
                    {
                        await roleManager.CreateAsync(new IdentityRole(roleName));
                    }
                }

                // ✅ CRÉER UN UTILISATEUR ADMIN PAR DÉFAUT
                var adminUser = await userManager.FindByNameAsync("771234567");
                if (adminUser == null)
                {
                    var admin = new ApplicationUser
                    {
                        UserName = "771234567",
                        PhoneNumber = "771234567",
                        CountryCode = "+221",
                        FirstName = "Admin",
                        LastName = "BawolShop",
                        Email = "admin@bawolshop.com",
                        Role = "Admin"
                    };

                    var result = await userManager.CreateAsync(admin, "admin123");
                    if (result.Succeeded)
                    {
                        await userManager.AddToRoleAsync(admin, "Admin");
                    }
                }
            }
        }
    }
}
