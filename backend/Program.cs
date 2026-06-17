using backend.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(connectionString));

var app = builder.Build();

var welcome = app.Configuration["AppSettings:WelcomeMessage"];
var version = app.Configuration["AppSettings:Version"];

app.Logger.LogInformation("Застосунок запущено. Середовище: {Env}", app.Environment.EnvironmentName);
app.UseMiddleware<ExceptionHandlingMiddleware>();

app.MapGet("/", () =>
{
    app.Logger.LogInformation("Опрацювання запиту до головного ендпоінта");
    return $"{welcome} (версія {version}). SQLite підключено.";
});

app.MapGet("/users", async (AppDbContext db) =>
    await db.Users.ToListAsync());

app.MapGet("/boom", () =>
{
    throw new Exception("Тестова помилка для перевірки Middleware");
});

app.Run();