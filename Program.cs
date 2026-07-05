using henglong.Web.Common;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<IMySqlHelper, MySqlHelper>();
builder.Services.AddControllersWithViews();

// Health checks
builder.Services.AddHealthChecks();

var app = builder.Build();

// Ensure the products table exists on startup
using (var scope = app.Services.CreateScope())
{
    var helper = scope.ServiceProvider.GetRequiredService<IMySqlHelper>() as MySqlHelper;
    helper?.EnsureTableCreated();
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
app.UseAuthorization();

app.MapHealthChecks("/healthz");
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
