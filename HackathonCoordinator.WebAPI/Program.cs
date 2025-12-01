using HackathonCoordinator.WebAPI.Data;
using HackathonCoordinator.WebAPI.Hubs;
using HackathonCoordinator.WebAPI.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Добавление сервисов
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Настройка Swagger с поддержкой JWT
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "HackathonCoordinator API",
        Version = "v1"
    });

    // Конфигурация аутентификации JWT в Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Введите JWT токен в формате: Bearer {token}"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

// Настройка HTTP клиента для GitHub API
builder.Services.AddHttpClient("GitHub", client =>
{
    client.BaseAddress = new Uri("https://api.github.com/");
    client.DefaultRequestHeaders.Add("User-Agent", "HackathonCoordinator/1.0");
    client.DefaultRequestHeaders.Add("Accept", "application/vnd.github.v3+json");
    client.Timeout = TimeSpan.FromSeconds(30);
});

// Регистрация контекста базы данных
builder.Services.AddDbContext<HackathonCoordinatorContext>();

// Настройка SignalR
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = true;
});

// Регистрация зависимостей
builder.Services.AddScoped<IGitHubService, GitHubService>();
builder.Services.AddScoped<NotificationHelperService>();

// Конфигурация JWT аутентификации
var jwt = builder.Configuration.GetSection("Jwt");

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
        ValidIssuer = jwt["Issuer"],
        ValidAudience = jwt["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"]))
    };

    // Обработчики событий JWT
    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            Console.WriteLine($"Ошибка аутентификации JWT: {context.Exception.Message}");
            return Task.CompletedTask;
        },
        OnChallenge = context =>
        {
            Console.WriteLine($"JWT Challenge: {context.ErrorDescription}");
            return Task.CompletedTask;
        },
        OnTokenValidated = context =>
        {
            Console.WriteLine("JWT токен успешно проверен!");
            return Task.CompletedTask;
        },
        OnMessageReceived = context =>
        {
            // Извлечение токена из query string для SignalR соединений
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;

            if (!string.IsNullOrEmpty(accessToken) &&
                (path.StartsWithSegments("/chathub") || path.StartsWithSegments("/notificationhub")))
            {
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();
builder.Services.AddSingleton<IEncryptionService, AesEncryptionService>();

// Настройка CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Фоновые сервисы
builder.Services.AddHostedService<DeadlineNotificationService>();

var app = builder.Build();

// Конфигурация конвейера запросов
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();

// Регистрация SignalR хабов
app.MapHub<ChatHub>("/chathub");
app.MapHub<NotificationHub>("/notificationhub");

app.MapControllers();
app.Run();
