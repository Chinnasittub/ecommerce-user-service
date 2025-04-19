using Microsoft.EntityFrameworkCore;
using UserService.Data;
using UserService.Models;

var builder = WebApplication.CreateBuilder(args);

// 🟡 กำหนดให้ Kestrel ฟังที่พอร์ต 80
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.ListenAnyIP(80);
});

// 🔗 เชื่อม PostgreSQL
builder.Services.AddDbContext<UserDbContext>(opt => opt.UseNpgsql(builder.Configuration.GetConnectionString("PostgresConnection")));

var app = builder.Build();

app.MapGet("/", () => "User Service is running!");


// 🛠 Apply EF Core Migration (Auto)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<UserDbContext>();
    db.Database.Migrate();
}

// ✅ CRUD Minimal API

// GET /users
app.MapGet("/users", async (UserDbContext db) =>
    await db.Users.ToListAsync());

// GET single user by id
app.MapGet("/users/{id}", async (int id, UserDbContext db) =>
    await db.Users.FirstOrDefaultAsync(u => u.Id == id) is User user ? Results.Ok(user) : Results.NotFound());

// Create a new user
app.MapPost("/users", async (User user, UserDbContext db) =>
{
    // Check if the username already exists
    if (await db.Users.AnyAsync(u => u.Username == user.Username))
        return Results.Conflict("Username already exists.");

    db.Users.Add(user);
    await db.SaveChangesAsync();
    return Results.Created($"/users/{user.Id}", user);
});

// Update a user
app.MapPut("/users/{username}", async (string username, User inputUser, UserDbContext db) =>
{
    var user = await db.Users.FirstOrDefaultAsync(u => u.Username == username);
    if (user is null) return Results.NotFound();

    // Prevent changing username
    if (inputUser.Username != username)
        return Results.BadRequest("Cannot change username.");

    user.Email = inputUser.Email;

    await db.SaveChangesAsync();
    return Results.NoContent();
});

app.MapDelete("/users/{id}", async (int id, UserDbContext db) =>
{
    // Check if the user exists
    var user = await db.Users.FindAsync(id);
    if (user is null) return Results.NotFound("User not found.");

    // Call OrderService to check if the user has orders
    using var client = new HttpClient();
    var response = await client.GetAsync($"http://order-service/api/Order/user/{id}");

    if (response.IsSuccessStatusCode)
    {
        var orders = await response.Content.ReadAsStringAsync();
        if (!string.IsNullOrEmpty(orders) && orders != "[]")
        {
            return Results.BadRequest($"User with ID {id} has existing orders and cannot be deleted.");
        }
    }
    else
    {
        return Results.Problem("Failed to validate user's orders.", statusCode: 500);
    }

    // Delete the user
    db.Users.Remove(user);
    await db.SaveChangesAsync();
    return Results.Ok(new { Message = "User deleted successfully.", User = user });
});

app.Run();
