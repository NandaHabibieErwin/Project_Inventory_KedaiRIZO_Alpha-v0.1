using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Project_Inventory_KedaiRIZO_Alpha_v0._1.Models;

namespace Project_Inventory_KedaiRIZO_Alpha_v0._1.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
        public DbSet<Project_Inventory_KedaiRIZO_Alpha_v0._1.Models.Kategori> Kategori { get; set; } = default!;
        public DbSet<Project_Inventory_KedaiRIZO_Alpha_v0._1.Models.Product> Product { get; set; } = default!;
    }
}
