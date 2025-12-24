using Microsoft.EntityFrameworkCore;
using GifCampAPI.Data;
using GifCampAPI.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add DbContext
// Connection string is read from:
// 1. Environment variable: ConnectionStrings__DefaultConnection (recommended for production)
// 2. appsettings.Production.json
// 3. appsettings.Development.json (for local development)
// 4. appsettings.json
builder.Services.AddDbContext<GifCampDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    options.UseNpgsql(connectionString);
    // Enable retry on failure for transient database errors
    options.EnableServiceProviderCaching();
    options.EnableSensitiveDataLogging(builder.Environment.IsDevelopment());
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Login endpoint
app.MapPost("/login", async (LoginRequest request, GifCampDbContext dbContext, ILogger<Program> logger) =>
{
    logger.LogInformation("Login handler reached. Email: {Email}, Method: {Method}, Name: {Name}", 
        request.Email, request.Method, request.Name);

    if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Method))
    {
        logger.LogWarning("Login request validation failed. Email or Method is missing. Email: {Email}, Method: {Method}", 
            request.Email ?? "null", request.Method ?? "null");
        return Results.BadRequest(new { message = "Email and Method are required" });
    }

    logger.LogDebug("Searching for existing user with Email: {Email} and Method: {Method}", 
        request.Email, request.Method);

    // Try to find existing user by Email and Method
    var existingUser = await dbContext.Users
        .FirstOrDefaultAsync(u => u.Email == request.Email && u.Method == request.Method);

    var now = DateTime.UtcNow;

    if (existingUser != null)
    {
        logger.LogInformation("Existing user found. UserId: {UserId}, Email: {Email}, Method: {Method}. Updating user information.", 
            existingUser.Id, existingUser.Email, existingUser.Method);
        
        // Update existing user
        existingUser.Name = request.Name;
        existingUser.Picture = request.Picture;
        existingUser.LastLogin = now;
        
        logger.LogDebug("User updated. Name: {Name}, LastLogin: {LastLogin}", 
            existingUser.Name, existingUser.LastLogin);
    }
    else
    {
        logger.LogInformation("No existing user found. Creating new user. Email: {Email}, Method: {Method}", 
            request.Email, request.Method);
        
        // Create new user
        var newUser = new User
        {
            Name = request.Name,
            Email = request.Email,
            Picture = request.Picture,
            Method = request.Method,
            FirstLogin = now,
            LastLogin = now
        };
        dbContext.Users.Add(newUser);
        existingUser = newUser;
        
        logger.LogDebug("New user created (not yet saved). Name: {Name}, Email: {Email}, Method: {Method}", 
            newUser.Name, newUser.Email, newUser.Method);
    }

    try
    {
        logger.LogDebug("Saving changes to database...");
        await dbContext.SaveChangesAsync();
        logger.LogInformation("Database changes saved successfully. UserId: {UserId}, Email: {Email}, Method: {Method}", 
            existingUser.Id, existingUser.Email, existingUser.Method);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error saving user to database. Email: {Email}, Method: {Method}", 
            request.Email, request.Method);
        return Results.Problem("An error occurred while processing your login request.");
    }

    logger.LogInformation("Login handler completed successfully. Returning user data. UserId: {UserId}", existingUser.Id);
    return Results.Ok(existingUser);
})
.WithName("Login")
.WithOpenApi();

app.Run();