using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http.Features;
using FurManual.Data;
using WebMarkupMin.AspNetCoreLatest;

var builder = WebApplication.CreateBuilder(args);


// 1. Подключение к БД (PostgreSQL)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

// 2. Настройка Аутентификации для фуррминов :3 (Cookie)
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Login";
        options.ExpireTimeSpan = TimeSpan.FromDays(30);
        options.SlidingExpiration = true;

        options.Cookie.HttpOnly = true;

        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;

        options.Cookie.SameSite = SameSiteMode.Strict;

        options.Cookie.Name = ".FurManual.Auth";
    });

// 3. Увеличение лимита загрузки файлов
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 60L * 1024 * 1024;
});

builder.Services.AddRazorPages();


builder.Services.AddWebMarkupMin(options =>
{
    options.AllowMinificationInDevelopmentEnvironment = true;
    options.AllowCompressionInDevelopmentEnvironment = true;
})
.AddHtmlMinification(options =>
{
    options.MinificationSettings.RemoveHtmlComments = true;

    options.MinificationSettings.WhitespaceMinificationMode = WebMarkupMin.Core.WhitespaceMinificationMode.Safe;

    options.MinificationSettings.RemoveHttpProtocolFromAttributes = true;
    options.MinificationSettings.RemoveHttpsProtocolFromAttributes = true;
})
.AddHttpCompression();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseWebMarkupMin();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

app.Run();