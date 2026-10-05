using Microsoft.AspNetCore.Identity;
using SecureTodo.Models;

namespace SecureTodo.Data
{
    public static class DbSeeder
    {
        public static async Task SeedAsync(IServiceProvider services)
        {
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = services.GetRequiredService<UserManager<AppUser>>();

            // 1) Роли
            foreach (var role in new[] { "Admin", "Author", "User" })
            {
                if (!await roleManager.RoleExistsAsync(role))
                    await roleManager.CreateAsync(new IdentityRole(role));
            }

            // 2) Админ
            await EnsureUserAsync(userManager,
                email: "admin@todo.com",
                password: "Admin123!",
                fullName: "Administrator",
                role: "Admin");

            // 3) Автор
            await EnsureUserAsync(userManager,
                email: "author@todo.com",
                password: "Author123!",
                fullName: "Default Author",
                role: "Author");

            // 4) Обычный пользователь
            await EnsureUserAsync(userManager,
                email: "user@todo.com",
                password: "User123!",
                fullName: "Default User",
                role: "User");
        }

        private static async Task EnsureUserAsync(
            UserManager<AppUser> userManager,
            string email,
            string password,
            string fullName,
            string role)
        {
            var user = await userManager.FindByEmailAsync(email);
            if (user != null) return;

            user = new AppUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FullName = fullName
            };

            var result = await userManager.CreateAsync(user, password);
            if (result.Succeeded)
                await userManager.AddToRoleAsync(user, role);
        }
    }
}