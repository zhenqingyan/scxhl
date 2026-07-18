using henglong.Web.Common;
using henglong.Web.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<IMySqlHelper, MySqlHelper>();
builder.Services.AddScoped<MiniProgramStore>();
builder.Services.AddScoped<IMiniProgramStore>(sp => sp.GetRequiredService<MiniProgramStore>());
builder.Services.AddScoped<IAdminUserStore, AdminUserStore>();
builder.Services.AddScoped<IAdminAuthService, AdminAuthService>();
builder.Services.AddScoped<PasswordHasher<AdminUser>>();
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/Login";
    });
builder.Services.AddControllersWithViews();

// Health checks
builder.Services.AddHealthChecks();

var app = builder.Build();

// Ensure the products table exists on startup
using (var scope = app.Services.CreateScope())
{
    var helper = scope.ServiceProvider.GetRequiredService<IMySqlHelper>() as MySqlHelper;
    helper?.EnsureTableCreated();
    var miniStore = scope.ServiceProvider.GetRequiredService<MiniProgramStore>();
    miniStore.EnsureTablesCreated();
    var adminStore = scope.ServiceProvider.GetRequiredService<IAdminUserStore>();
    adminStore.EnsureTableCreated();
}

var disableHttps = builder.Configuration.GetValue<bool>("DISABLE_HTTPS");

if (!app.Environment.IsDevelopment() && !disableHttps)
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

if (!disableHttps)
{
    app.UseHttpsRedirection();
}
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/healthz");
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
