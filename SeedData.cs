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
                    // Ajouter des produits si la table est vide
    if (!context.Products.Any())
    {
        Console.WriteLine("🌱 Ajout des produits de test...");
        
        var products = new List<Product>
        {
            new Product 
            { 
                Name = "iPhone 14", 
                Description = "Dernier smartphone Apple avec écran Super Retina XDR", 
                Price = 999.99m,
                StockQuantity = 50,
                ImageUrl = "https://images.unsplash.com/photo-1592750475338-74b7b21085ab?w=400&h=300&fit=crop",
                Category = "Smartphones",
                CreatedAt = DateTime.UtcNow
            },
            new Product 
            { 
                Name = "Samsung Galaxy S23", 
                Description = "Flagship Android avec appareil photo professionnel", 
                Price = 849.99m,
                StockQuantity = 30,
                ImageUrl = "https://images.unsplash.com/photo-1511707171634-5f897ff02aa9?w=400&h=300&fit=crop",
                Category = "Smartphones",
                CreatedAt = DateTime.UtcNow
            },
            new Product 
            { 
                Name = "MacBook Pro 16\"", 
                Description = "Ordinateur portable professionnel avec puce M2", 
                Price = 2499.99m,
                StockQuantity = 15,
                ImageUrl = "https://images.unsplash.com/photo-1541807084-5c52b6b3adef?w=400&h=300&fit=crop",
                Category = "Laptops",
                CreatedAt = DateTime.UtcNow
            },
            new Product 
            { 
                Name = "AirPods Pro", 
                Description = "Écouteurs sans fil avec réduction de bruit active", 
                Price = 249.99m,
                StockQuantity = 100,
                ImageUrl = "https://images.unsplash.com/photo-1600294037681-c80b4cb5b434?w=400&h=300&fit=crop",
                Category = "Accessories",
                CreatedAt = DateTime.UtcNow
            }
        };

        context.Products.AddRange(products);
        await context.SaveChangesAsync();
        Console.WriteLine($"✅ {products.Count} produits de test ajoutés !");
    }
    else
    {
        Console.WriteLine("✅ Des produits existent déjà dans la base");
    }
            }
        }
    }
}

