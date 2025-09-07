using FileShare.Services;
using Microsoft.AspNetCore.Http.Features;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

// Configure the maximum allowed file upload size for forms
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 250 * 1024 * 1024; // 250MB
});

// Configure Kestrel for large uploads
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 250 * 1024 * 1024; // 250 MB
});

// Add in-memory caching service (used to store temporary data in memory)
builder.Services.AddMemoryCache();

// Register background services
builder.Services.AddHostedService<FileCleanupService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

// Custom route for file sharing downloads
// Example URL: https://example.com/share/filename
app.MapControllerRoute(
    name: "share",
    pattern: "share/{fileName}",
    defaults: new { controller = "Home", action = "Share" }
);

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
