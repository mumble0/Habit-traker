var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var welcome = app.Configuration["AppSettings:WelcomeMessage"];
var version = app.Configuration["AppSettings:Version"];

app.Logger.LogInformation("Застосунок запущено. Середовище: {Env}", app.Environment.EnvironmentName);
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.MapGet("/boom", () =>
{
    throw new Exception("Тестова помилка для перевірки Middleware");
});

app.MapGet("/", () =>
{
    app.Logger.LogInformation("Опрацювання запиту до головного ендпоінта");
    return $"{welcome} (версія {version})";
});

app.Run();
