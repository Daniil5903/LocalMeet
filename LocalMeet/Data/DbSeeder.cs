using LocalMeet.Models.Entities;
using LocalMeet.Models.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace LocalMeet.Data
{
    public static class DbSeeder
    {
        public static async Task SeedAsync(
            IServiceProvider serviceProvider,
            IConfiguration configuration)
        {
            using var scope = serviceProvider.CreateScope();

            var roleManager = scope.ServiceProvider
                .GetRequiredService<RoleManager<IdentityRole>>();

            var userManager = scope.ServiceProvider
                .GetRequiredService<UserManager<User>>();

            var dbContext = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

            await dbContext.Database.MigrateAsync();

            await CreateRoleAsync(
                roleManager,
                AppRole.Admin);

            await CreateRoleAsync(
                roleManager,
                AppRole.User);

            await CreateDefaultAdminAsync(
                userManager,
                configuration);

            await SeedCategoriesAsync(dbContext);
        }

        private static async Task CreateRoleAsync(
            RoleManager<IdentityRole> roleManager,
            string roleName)
        {
            var roleExists =
                await roleManager.RoleExistsAsync(roleName);

            if (!roleExists)
            {
                await roleManager.CreateAsync(
                    new IdentityRole(roleName));
            }
        }

        private static async Task CreateDefaultAdminAsync(
            UserManager<User> userManager,
            IConfiguration configuration)
        {
            var adminEmail =
                configuration["AdminUser:Email"];

            var adminPassword =
                configuration["AdminUser:Password"];

            var adminFirstName =
                configuration["AdminUser:FirstName"];

            var adminLastName =
                configuration["AdminUser:LastName"];

            if (string.IsNullOrWhiteSpace(adminEmail) ||
                string.IsNullOrWhiteSpace(adminPassword))
            {
                return;
            }

            var existingAdmin =
                await userManager.FindByEmailAsync(adminEmail);

            if (existingAdmin != null)
            {
                if (!await userManager.IsInRoleAsync(
                    existingAdmin,
                    AppRole.Admin))
                {
                    await userManager.AddToRoleAsync(
                        existingAdmin,
                        AppRole.Admin);
                }

                return;
            }

            var admin = new User
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true,
                FirstName = adminFirstName ?? "Admin",
                LastName = adminLastName ?? "User",
                RegistrationDate = DateTime.Now,
                LastVisit = DateTime.Now
            };

            var result =
                await userManager.CreateAsync(
                    admin,
                    adminPassword);

            if (!result.Succeeded)
            {
                var errors = string.Join(
                    "; ",
                    result.Errors.Select(error =>
                        error.Description));

                throw new InvalidOperationException(
                    $"Не удалось создать администратора: {errors}");
            }

            await userManager.AddToRoleAsync(
                admin,
                AppRole.Admin);
        }

        private static async Task SeedCategoriesAsync(
            ApplicationDbContext dbContext)
        {
            var categoryNames = new[]
            {
                "Спорт",
                "Образование",
                "Культура",
                "Волонтёрство",
                "Бизнес",
                "Игры",
                "Прогулки",
                "Другое"
            };

            foreach (var categoryName in categoryNames)
            {
                var exists = await dbContext.Categories
                    .AnyAsync(category =>
                        category.Name == categoryName);

                if (!exists)
                {
                    dbContext.Categories.Add(
                        new Category
                        {
                            Name = categoryName
                        });
                }
            }

            await dbContext.SaveChangesAsync();
        }
    }
}