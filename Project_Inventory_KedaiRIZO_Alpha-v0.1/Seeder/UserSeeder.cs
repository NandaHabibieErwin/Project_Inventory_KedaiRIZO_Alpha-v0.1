using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace Project_Inventory_KedaiRIZO_Alpha_v0._1.Seeder
{
    public static class UserSeeder
    {
        public static async Task UserSeedAsync(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            string[] RoleName = { "Admin", "Kasir" };
            foreach (var roleName in RoleName)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }
        }
    }
}
