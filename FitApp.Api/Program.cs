using FitApp.Api.Data;
using FitApp.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;

var builder = WebApplication.CreateBuilder(args);

// DI
builder.Services.AddSingleton<AppDb>();
builder.Services.AddSingleton<JwtService>();

// JWT
var jwtTmp = new JwtService(builder.Configuration);
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = jwtTmp.GetValidationParameters();
    });
builder.Services.AddAuthorization();

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

// Создаём/мигрируем БД при старте, до того как примем первый запрос.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDb>();
    await db.InitAsync();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// HTTPS-редирект в dev мешает (нет сертификата на устройствах) — выключаем
// до этапа деплоя, на Render будет внешний https терминатор.
// app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
