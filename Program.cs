using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using LOSTBOOKS.Data;
using QuestPDF.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// =====================================================
// EMERGENCY ADMIN RECOVERY (console-only, not web-reachable)
// Run with: dotnet run -- emergency-reset-admin
// =====================================================
if (args.Contains("emergency-reset-admin"))
{
    var optionsBuilder = new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<LOSTBOOKS.Data.LOSTBOOKSContext>();
    optionsBuilder.UseSqlServer(builder.Configuration.GetConnectionString("LOSTBOOKSContext"));

    using var recoveryContext = new LOSTBOOKS.Data.LOSTBOOKSContext(optionsBuilder.Options);

    var admin = recoveryContext.Users
        .Where(u => u.Role == "Manager" && u.Status == "Active")
        .OrderBy(u => u.UserID)
        .FirstOrDefault();

    if (admin == null)
    {
        Console.WriteLine("No active Manager account found. Cannot perform emergency recovery.");
        return;
    }

    const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789";
    var random = new Random();

    string tempPassword = new string(
        Enumerable.Range(0, 12)
            .Select(_ => chars[random.Next(chars.Length)])
            .ToArray());

    var hasher = new Microsoft.AspNetCore.Identity.PasswordHasher<LOSTBOOKS.Models.User>();

    admin.PasswordHash = hasher.HashPassword(admin, tempPassword);
    admin.MustChangePassword = true;

    recoveryContext.SaveChanges();

    Console.WriteLine("=====================================================");
    Console.WriteLine(" EMERGENCY ADMIN RECOVERY COMPLETE");
    Console.WriteLine("=====================================================");
    Console.WriteLine($" Account:            {admin.Username} ({admin.FullName})");
    Console.WriteLine($" Temporary password: {tempPassword}");
    Console.WriteLine(" This account must set a new password on next login.");
    Console.WriteLine("=====================================================");

    return;
}

// QuestPDF License
QuestPDF.Settings.License = LicenseType.Community;

builder.Services.AddDbContext<LOSTBOOKSContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("LOSTBOOKSContext")
        ?? throw new InvalidOperationException(
            "Connection string 'LOSTBOOKSContext' not found."
        ),
        sqlOptions => sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorNumbersToAdd: null
        )
    ));

// Add services to the container.
builder.Services.AddHttpContextAccessor();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession();

builder.Services.AddScoped<LOSTBOOKS.Filters.RequireLoginFilter>();

builder.Services.AddControllersWithViews(options =>
{
    options.Filters.AddService<LOSTBOOKS.Filters.RequireLoginFilter>();
});

builder.Services.AddScoped<
    LOSTBOOKS.Services.ICurrentUserService,
    LOSTBOOKS.Services.CurrentUserService>();

builder.Services.AddScoped<
    LOSTBOOKS.Services.IActivityLogger,
    LOSTBOOKS.Services.ActivityLogger>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");

    // The default HSTS value is 30 days.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();