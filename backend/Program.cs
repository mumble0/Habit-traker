using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using backend.Data;
using backend.DTOs;
using backend.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;


var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(connectionString));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
        };
    });
builder.Services.AddAuthorization();


builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Твої слейні звички ",
        Version = "v1",
        Description = "Тут ти записуєшь свої слейні звички, щоб не забути виканати всі"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Введіть JWT-токен, отриманий з ендпоінта /auth/login."
    });

    options.AddSecurityRequirement(doc => new OpenApiSecurityRequirement
{
    { new OpenApiSecuritySchemeReference("Bearer", doc), new List<string>() }
});

});


var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.UseSwagger();
app.UseSwaggerUI();

app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "MVP Back-End API v1");
    options.DocumentTitle = "MVP Back-End API";
});

app.MapPost("/auth/login", async (LoginDto dto, AppDbContext db, IConfiguration config) =>
{
    var user = await db.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
    if (user is null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
        return Results.Unauthorized();

    var token = CreateToken(user, config);
    return Results.Ok(new { access_token = token, token_type = "Bearer" });
})
    .WithTags("Auth");
app.MapGet("/", () => "MVP Back-End SQLite!");

app.MapGet("/users/{id}", async (int id, AppDbContext db) =>
    await db.Users.FindAsync(id) is User user
        ? Results.Ok(user)
        : Results.NotFound());

app.MapPost("/users", async (User user, AppDbContext db) =>
{
    db.Users.Add(user);
    await db.SaveChangesAsync();
    return Results.Created($"/users/{user.Id}", user);
});

app.MapPut("/users/{id}", async (int id, User input, AppDbContext db) =>
{
    var user = await db.Users.FindAsync(id);
    if (user is null) return Results.NotFound();

    user.Name = input.Name;
    user.Email = input.Email;
    await db.SaveChangesAsync();
    return Results.NoContent();
});

app.MapDelete("/users/{id}", async (int id, AppDbContext db) =>
{
    var user = await db.Users.FindAsync(id);
    if (user is null) return Results.NotFound();

    db.Users.Remove(user);
    await db.SaveChangesAsync();
    return Results.NoContent();
});

app.MapGet("/users", async (AppDbContext db) =>
    await db.Users.ToListAsync())
    .WithTags("Users");

app.MapGet("/auth/me", (ClaimsPrincipal principal) =>
    Results.Ok(new
    {
        Id = principal.FindFirstValue(ClaimTypes.NameIdentifier),
        Email = principal.FindFirstValue(ClaimTypes.Email),
        Role = principal.FindFirstValue(ClaimTypes.Role)
    }))
    .RequireAuthorization()
    .WithTags("Auth");


app.MapPost("/auth/register", async (RegisterDto dto, AppDbContext db) =>
{
    if (await db.Users.AnyAsync(u => u.Email == dto.Email))
        return Results.Conflict("Користувач з таким email вже існує.");

    var user = new User
    {
        Name = dto.Name,
        Email = dto.Email,
        PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
        Role = "user"
    };
    db.Users.Add(user);
    await db.SaveChangesAsync();
    return Results.Created($"/users/{user.Id}",
        new { user.Id, user.Name, user.Email, user.Role });
})
    .WithTags("Auth");


// NENNENENNENENENNENENNENENENE

app.MapGet("/habits", async (ClaimsPrincipal principal, AppDbContext db) =>
{
    var userId = int.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier)!);
    var habits = await db.Habits.Where(h => h.UserId == userId).ToListAsync();
    return Results.Ok(habits);
})
    .RequireAuthorization()
    .WithTags("Habits");

app.MapGet("/habits/{id}", async (int id, ClaimsPrincipal principal, AppDbContext db) =>
{
    var userId = int.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier)!);
    var habit = await db.Habits.FindAsync(id);
    if (habit is null || habit.UserId != userId) return Results.NotFound();
    return Results.Ok(habit);
})
    .RequireAuthorization()
    .WithTags("Habits");

app.MapPost("/habits", async (HabitDto dto, ClaimsPrincipal principal, AppDbContext db) =>
{
    var userId = int.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier)!);
    var habit = new Habit
    {
        NameHabit = dto.NameHabit,
        Description = dto.Description,
        StartDate = dto.StartDate,
        IsCompleted = false,
        UserId = userId
    };
    db.Habits.Add(habit);
    await db.SaveChangesAsync();
    return Results.Created($"/habits/{habit.Id}", habit);
})
    .RequireAuthorization()
    .WithTags("Habits");

app.MapPut("/habits/{id}", async (int id, HabitDto dto, ClaimsPrincipal principal, AppDbContext db) =>
{
    var userId = int.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier)!);
    var habit = await db.Habits.FindAsync(id);
    if (habit is null || habit.UserId != userId) return Results.NotFound();

    habit.NameHabit = dto.NameHabit;
    habit.Description = dto.Description;
    habit.StartDate = dto.StartDate;
    await db.SaveChangesAsync();
    return Results.NoContent();
})
    .RequireAuthorization()
    .WithTags("Habits");

app.MapPatch("/habits/{id}/complete", async (int id, ClaimsPrincipal principal, AppDbContext db) =>
{
    var userId = int.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier)!);
    var habit = await db.Habits.FindAsync(id);
    if (habit is null || habit.UserId != userId) return Results.NotFound();

    habit.IsCompleted = !habit.IsCompleted;
    await db.SaveChangesAsync();
    return Results.Ok(habit);
})
    .RequireAuthorization()
    .WithTags("Habits");

app.MapDelete("/habits/{id}", async (int id, ClaimsPrincipal principal, AppDbContext db) =>
{
    var userId = int.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier)!);
    var habit = await db.Habits.FindAsync(id);
    if (habit is null || habit.UserId != userId) return Results.NotFound();

    db.Habits.Remove(habit);
    await db.SaveChangesAsync();
    return Results.NoContent();
})
    .RequireAuthorization()
    .WithTags("Habits");
// NENENENNENENNENE




app.Run();


static string CreateToken(User user, IConfiguration config)
{
    var claims = new[]
    {
        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
        new Claim(ClaimTypes.Email, user.Email),
        new Claim(ClaimTypes.Role, user.Role)
    };
    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]!));
    var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
    var token = new JwtSecurityToken(
        issuer: config["Jwt:Issuer"],
        audience: config["Jwt:Audience"],
        claims: claims,
        expires: DateTime.UtcNow.AddHours(2),
        signingCredentials: creds);
    return new JwtSecurityTokenHandler().WriteToken(token);
}


record RegisterDto(string Name, string Email, string Password);
record LoginDto(string Email, string Password);
