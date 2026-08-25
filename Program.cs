using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using LOSTBOOKS.Data;
using QuestPDF.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

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
builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession();

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