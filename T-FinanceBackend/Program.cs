using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using TFinanceBackend.Data;

var builder = WebApplication.CreateBuilder(args);

// Запускаем Kestrel, адреса берутся из переменных окружения (ASPNETCORE_URLS)
builder.WebHost.UseKestrel();

// DEV CORS (  )
builder.Services.AddCors(options =>
{
    options.AddPolicy("DevCors", cors =>
    {
        cors.WithOrigins("http://192.168.0.106:5173", "http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// JWT 
var jwtSection = builder.Configuration.GetSection("Jwt");
var issuer = jwtSection["Issuer"];
var audience = jwtSection["Audience"];
var key = jwtSection["Key"];

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = issuer,
        ValidAudience = audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key!))
    };
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            if (context.Request.Cookies.TryGetValue("token", out var token))
                context.Token = token;
            return Task.CompletedTask;
        }
    };
});

//  SQLite ConnectionStrings
var connectionString = builder.Configuration.GetConnectionString("Default") ?? "Data Source=Data/users.db";
builder.Services.AddDbContext<TFinanceDbContext>(options => options.UseSqlite(connectionString));

var dataProtectionKeysPath = Path.Combine(builder.Environment.ContentRootPath, "Data", "DataProtection");
Directory.CreateDirectory(dataProtectionKeysPath);
var dataProtection = builder.Services.AddDataProtection()
    .SetApplicationName("TFinanceBackend")
    .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeysPath));

var certPath = builder.Configuration["DataProtection:CertificatePath"] ?? Environment.GetEnvironmentVariable("DATA_PROTECTION_CERT_PATH");
if (!string.IsNullOrWhiteSpace(certPath) && File.Exists(certPath))
{
    try
    {
        var certPassword = builder.Configuration["DataProtection:CertificatePassword"] ?? Environment.GetEnvironmentVariable("DATA_PROTECTION_CERT_PASSWORD");
        var rawData = File.ReadAllBytes(certPath);
        X509Certificate2 certificate = string.IsNullOrEmpty(certPassword)
            ? X509CertificateLoader.LoadPkcs12(rawData, ReadOnlySpan<char>.Empty)
            : X509CertificateLoader.LoadPkcs12(rawData, certPassword.AsSpan());

        dataProtection.ProtectKeysWithCertificate(certificate);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[DataProtection] Failed to load certificate from '{certPath}'. Keys will be stored unencrypted. {ex}");
    }
}

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

// Автоматическое создание базы данных при первом запуске
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<TFinanceDbContext>();
        // Создаём базу данных и таблицы, если их нет
        context.Database.EnsureCreated();
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Произошла ошибка при создании базы данных");
    }
}

//   Nginx (  https)
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseCors("DevCors");
}
// В продакшене SSL завершается на внешнем прокси (Nginx), поэтому без редиректа на HTTPS внутри контейнера.

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
