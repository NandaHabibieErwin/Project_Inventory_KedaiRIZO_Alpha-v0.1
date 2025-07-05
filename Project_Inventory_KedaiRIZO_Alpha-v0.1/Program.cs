using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Project_Inventory_KedaiRIZO_Alpha_v0._1.Data;
using Project_Inventory_KedaiRIZO_Alpha_v0._1.Seeder;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();
builder.Services.AddRazorPages();
builder.Services.AddIdentity<IdentityUser, IdentityRole>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddRoles<IdentityRole>()
.AddDefaultUI()
.AddDefaultTokenProviders();
builder.Services.AddControllersWithViews();

var app = builder.Build();
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    await UserSeeder.UserSeedAsync(services);
}
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var roles = new[] { "Admin", "Kasir" };

    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
            await roleManager.CreateAsync(new IdentityRole(role));
    }
}

using (var scope = app.Services.CreateScope())
{
    var UserManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();


    string emailAdmin = "admin@rizo.com";
    string passwordAdmin = "Blackjack420!";

    string emailKasir = "kasir@rizo.com";
    string passwordKasir = "Blackjacks420!";

    var adminUser = await UserManager.FindByEmailAsync(emailAdmin);
    if (adminUser == null)
    {
        var Admin = new IdentityUser { UserName = emailAdmin, Email = emailAdmin, EmailConfirmed = true };
        await UserManager.CreateAsync(Admin, passwordAdmin);
        await UserManager.AddToRoleAsync(Admin, "Admin");
    }

    var kasirUser = await UserManager.FindByEmailAsync(emailKasir);
    if (kasirUser == null)
    {
        var Kasir = new IdentityUser { UserName = emailKasir, Email = emailKasir, EmailConfirmed = true };
        await UserManager.CreateAsync(Kasir, passwordKasir);
        await UserManager.AddToRoleAsync(Kasir, "Kasir");
    }


}




app.Run();
