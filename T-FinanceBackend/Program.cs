using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.RateLimiting;
using TFinanceBackend.Data;
using TFinanceBackend.Services;

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

// JWT - приоритет переменным окружения, затем конфигурации
var jwtSection = builder.Configuration.GetSection("Jwt");
var issuer = Environment.GetEnvironmentVariable("JWT_ISSUER") 
    ?? jwtSection["Issuer"] 
    ?? throw new InvalidOperationException("JWT Issuer не настроен. Установите JWT_ISSUER в переменных окружения или appsettings.json");
var audience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") 
    ?? jwtSection["Audience"] 
    ?? throw new InvalidOperationException("JWT Audience не настроен. Установите JWT_AUDIENCE в переменных окружения или appsettings.json");
var key = Environment.GetEnvironmentVariable("JWT_KEY") 
    ?? jwtSection["Key"] 
    ?? throw new InvalidOperationException("JWT Key не настроен. Установите JWT_KEY в переменных окружения или appsettings.json");
var expiresInHours = int.TryParse(Environment.GetEnvironmentVariable("JWT_EXPIRES_IN_HOURS"), out var hours) 
    ? hours 
    : jwtSection.GetValue<int>("ExpiresInHours", 1);

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

// Data Protection Certificate - приоритет переменным окружения
var certPath = Environment.GetEnvironmentVariable("DATA_PROTECTION_CERT_PATH") 
    ?? builder.Configuration["DataProtection:CertificatePath"];
    
if (!string.IsNullOrWhiteSpace(certPath))
{
    // Поддержка путей внутри контейнера
    var fullCertPath = Path.IsPathRooted(certPath) 
        ? certPath 
        : Path.Combine(builder.Environment.ContentRootPath, certPath);
    
    if (File.Exists(fullCertPath))
    {
        try
        {
            var certPassword = Environment.GetEnvironmentVariable("DATA_PROTECTION_CERT_PASSWORD") 
                ?? builder.Configuration["DataProtection:CertificatePassword"];
            var rawData = File.ReadAllBytes(fullCertPath);
            X509Certificate2 certificate = string.IsNullOrEmpty(certPassword)
                ? X509CertificateLoader.LoadPkcs12(rawData, ReadOnlySpan<char>.Empty)
                : X509CertificateLoader.LoadPkcs12(rawData, certPassword.AsSpan());

            dataProtection.ProtectKeysWithCertificate(certificate);
            Console.WriteLine($"[DataProtection] Certificate loaded successfully from '{fullCertPath}'");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DataProtection] WARNING: Failed to load certificate from '{fullCertPath}'. Keys will be stored unencrypted. Error: {ex.Message}");
        }
    }
    else
    {
        Console.WriteLine($"[DataProtection] WARNING: Certificate file not found at '{fullCertPath}'. Keys will be stored unencrypted.");
    }
}
else
{
    Console.WriteLine("[DataProtection] INFO: No certificate configured. Keys will be stored unencrypted. Set DATA_PROTECTION_CERT_PATH to enable encryption.");
}

// Rate Limiting для защиты от брутфорса
builder.Services.AddRateLimiter(options =>
{
    // Ограничение для логина - 5 попыток в минуту
    options.AddFixedWindowLimiter("login", opt =>
    {
        opt.Window = TimeSpan.FromMinutes(1);
        opt.PermitLimit = 5;
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = 2;
    });

    // Ограничение для регистрации - 3 попытки в минуту
    options.AddFixedWindowLimiter("register", opt =>
    {
        opt.Window = TimeSpan.FromMinutes(1);
        opt.PermitLimit = 3;
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = 1;
    });

    // Общее ограничение для API - 100 запросов в минуту
    options.AddFixedWindowLimiter("api", opt =>
    {
        opt.Window = TimeSpan.FromMinutes(1);
        opt.PermitLimit = 100;
    });

    // Глобальное ограничение - 1000 запросов в минуту на IP
    options.GlobalLimiter = PartitionedRateLimiter.Create<Microsoft.AspNetCore.Http.HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: partition => new FixedWindowRateLimiterOptions
            {
                Window = TimeSpan.FromMinutes(1),
                PermitLimit = 1000
            }));
});

// Регистрация YooKassaService
builder.Services.AddHttpClient<YooKassaService>();
builder.Services.AddScoped<YooKassaService>();

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

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
