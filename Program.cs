using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using LOSTBOOKS.Data;
using LOSTBOOKS.Services;
using QuestPDF.Infrastructure;

var builder = WebApplication.CreateBuilder(args);


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

    string recoveryToken = new string(
        Enumerable.Range(0, 16)
            .Select(_ => chars[random.Next(chars.Length)])
            .ToArray());

    admin.EmergencyRecoveryToken = recoveryToken;
    admin.EmergencyRecoveryTokenExpiry = DateTime.Now.AddHours(24);

    recoveryContext.SaveChanges();

    Console.WriteLine("=====================================================");
    Console.WriteLine(" EMERGENCY RECOVERY TOKEN GENERATED");
    Console.WriteLine("=====================================================");
    Console.WriteLine($" Account:         {admin.Username} ({admin.FullName})");
    Console.WriteLine($" Recovery token:  {recoveryToken}");
    Console.WriteLine(" Valid for 24 hours. The account holder must enter this");
    Console.WriteLine(" token themselves to set their own new password.");
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

builder.Services.AddScoped<LOSTBOOKS.Services.ICurrentUserService,
    LOSTBOOKS.Services.CurrentUserService>();
builder.Services.AddScoped<LOSTBOOKS.Services.IActivityLogger,
    LOSTBOOKS.Services.ActivityLogger>();
builder.Services.AddScoped<LOSTBOOKS.Services.IEmailSender,
    LOSTBOOKS.Services.EmailSender > ();

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