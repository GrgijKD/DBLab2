using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using LibraryInfrastructure;
using LibraryDomain.Model;
using LibraryInfrastructure.Areas.Identity.Data;
using Hangfire;
using Hangfire.SqlServer;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<LibraryContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddHangfire(config =>
    config.SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
          .UseSimpleAssemblyNameTypeSerializer()
          .UseRecommendedSerializerSettings()
          .UseSqlServerStorage(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddHangfireServer();

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
})
    .AddEntityFrameworkStores<LibraryContext>()
    .AddDefaultTokenProviders()
    .AddDefaultUI();

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
    var dbContext = services.GetRequiredService<LibraryContext>();

    string[] roles = { "Admin", "Librarian" };

    try
    {
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Помилка створення ролей: {ex.Message}");
    }

    string adminEmail = "admin@example.com";
    string adminPassword = "Admin123!";

    try
    {
        var admin = await userManager.FindByEmailAsync(adminEmail);
        if (admin == null)
        {
            admin = new ApplicationUser { UserName = adminEmail, Email = adminEmail };
            var result = await userManager.CreateAsync(admin, adminPassword);

            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(admin, "Admin");
            }
            else
            {
                Console.WriteLine("Помилка створення адміністратора: " + string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }
        else if (!await userManager.IsInRoleAsync(admin, "Admin"))
        {
            await userManager.AddToRoleAsync(admin, "Admin");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Помилка створення адміністратора: {ex.Message}");
    }

    try
    {
        var users = userManager.Users.ToList();
        var existingClientIds = new HashSet<string>(dbContext.Clients.Select(c => c.Id));

        foreach (var user in users)
        {
            if (!existingClientIds.Contains(user.Id))
            {
                dbContext.Clients.Add(new Client
                {
                    Id = user.Id,
                    FullName = user.UserName ?? "Невідомий користувач"
                });
            }
        }

        await dbContext.SaveChangesAsync();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Помилка оновлення клієнтів: {ex.Message}");
    }
}

app.UseHangfireDashboard();

RecurringJob.AddOrUpdate<BookReservationsController>(
    "expire-reservations",
    x => x.ExpireReservations(),
    "0 0 * * *",
    new RecurringJobOptions()
    {
        TimeZone = TimeZoneInfo.FindSystemTimeZoneById("FLE Standard Time")
    }
);

RecurringJob.AddOrUpdate<BookReservationsController>(
    "check-overdue-reservations",
    x => x.CheckOverdue(),
    "0 0 * * *",
    new RecurringJobOptions()
    {
        TimeZone = TimeZoneInfo.FindSystemTimeZoneById("FLE Standard Time")
    }
);

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Books}/{action=Index}/{id?}");

app.MapRazorPages();

app.Run();