using LocalMeet.Data;
using LocalMeet.Hubs;
using LocalMeet.Models.Entities;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using MySqlConnector;
using System.Globalization;

var invariantCulture = CultureInfo.InvariantCulture;

CultureInfo.DefaultThreadCurrentCulture = invariantCulture;
CultureInfo.DefaultThreadCurrentUICulture = invariantCulture;

var builder = WebApplication.CreateBuilder(args);

var railwayPort = Environment.GetEnvironmentVariable("PORT");

if (!string.IsNullOrWhiteSpace(railwayPort))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{railwayPort}");
}

var connectionString = ResolveConnectionString(builder.Configuration);
var serverVersionText =
    builder.Configuration["Database:ServerVersion"] ?? "8.0.0";

if (!Version.TryParse(serverVersionText, out var serverVersion))
{
    throw new InvalidOperationException(
        "Параметр Database:ServerVersion должен содержать корректную версию MySQL.");
}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseMySql(
        connectionString,
        new MySqlServerVersion(serverVersion),
        mysqlOptions => mysqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorNumbersToAdd: null));
});

builder.Services
    .AddIdentity<User, IdentityRole>(options =>
    {
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = true;
        options.Password.RequiredLength = 6;

        options.User.RequireUniqueEmail = true;

        options.SignIn.RequireConfirmedEmail = false;
        options.SignIn.RequireConfirmedAccount = false;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
});

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor |
        ForwardedHeaders.XForwardedProto;

    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

var dataPath = builder.Configuration["Storage:DataPath"];
string? avatarStoragePath = null;

if (!string.IsNullOrWhiteSpace(dataPath))
{
    var resolvedDataPath = Path.IsPathRooted(dataPath)
        ? dataPath
        : Path.GetFullPath(
            dataPath,
            builder.Environment.ContentRootPath);

    avatarStoragePath = Path.Combine(
        resolvedDataPath,
        "avatars");

    var dataProtectionKeysPath = Path.Combine(
        resolvedDataPath,
        "keys");

    Directory.CreateDirectory(avatarStoragePath);
    Directory.CreateDirectory(dataProtectionKeysPath);

    builder.Services
        .AddDataProtection()
        .PersistKeysToFileSystem(
            new DirectoryInfo(dataProtectionKeysPath))
        .SetApplicationName("LocalMeet");
}

builder.Services.AddControllersWithViews();
builder.Services.AddSignalR();
builder.Services.AddHealthChecks();

var app = builder.Build();

await DbSeeder.SeedAsync(
    app.Services,
    app.Configuration);

app.UseForwardedHeaders();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error/500");
    app.UseHsts();
}

app.UseWhen(
    context => !context.Request.Path.StartsWithSegments("/health"),
    branch => branch.UseHttpsRedirection());

app.UseStaticFiles();

if (avatarStoragePath != null)
{
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider =
            new PhysicalFileProvider(avatarStoragePath),

        RequestPath = "/uploads/avatars"
    });
}

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");

app.MapHub<EventChatHub>("/hubs/eventChat");

app.MapControllerRoute(
    name: "areas",
    pattern:
        "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern:
        "{controller=Home}/{action=Index}/{id?}");

app.Run();

static string ResolveConnectionString(
    IConfiguration configuration)
{
    var railwayHost = configuration["MYSQLHOST"];

    if (!string.IsNullOrWhiteSpace(railwayHost))
    {
        var connectionStringBuilder =
            new MySqlConnectionStringBuilder
            {
                Server = railwayHost,

                Database = GetRequiredConfiguration(
                    configuration,
                    "MYSQLDATABASE"),

                UserID = GetRequiredConfiguration(
                    configuration,
                    "MYSQLUSER"),

                Password = GetRequiredConfiguration(
                    configuration,
                    "MYSQLPASSWORD"),

                Port = uint.TryParse(
                    configuration["MYSQLPORT"],
                    out var port)
                        ? port
                        : 3306
            };

        return connectionStringBuilder.ConnectionString;
    }

    var configuredConnectionString =
        configuration.GetConnectionString(
            "DefaultConnection");

    if (string.IsNullOrWhiteSpace(
        configuredConnectionString))
    {
        throw new InvalidOperationException(
            "Не настроено подключение к MySQL. " +
            "Укажите ConnectionStrings:DefaultConnection " +
            "или переменные MYSQLHOST, MYSQLPORT, " +
            "MYSQLDATABASE, MYSQLUSER и MYSQLPASSWORD.");
    }

    return configuredConnectionString;
}

static string GetRequiredConfiguration(
    IConfiguration configuration,
    string key)
{
    var value = configuration[key];

    if (string.IsNullOrWhiteSpace(value))
    {
        throw new InvalidOperationException(
            $"Не задана обязательная переменная окружения {key}.");
    }

    return value;
}