using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using TFinanceBackend.Data;

var builder = WebApplication.CreateBuilder(args);

// Запускаем Kestrel, адреса берутся из переменных окружения (ASPNETCORE_URLS)
builder.WebHost.UseKestrel();

// DEV CORS (������ ��� ����������)
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

// JWT �� ��������
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

// �������� SQLite �� ConnectionStrings
var connectionString = builder.Configuration.GetConnectionString("Default") ?? "Data Source=Data/users.db";
builder.Services.AddDbContext<TFinanceDbContext>(options => options.UseSqlite(connectionString));

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

// ��������� ��������� �� Nginx (����� ������ https)
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
