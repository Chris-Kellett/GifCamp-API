using Microsoft.EntityFrameworkCore;
using GifCampAPI.Data;
using GifCampAPI.Models;

namespace GifCampAPI.Handlers;

public static class LoginHandler
{
    public static async Task<IResult> Handle(LoginRequest request, GifCampDbContext dbContext, ILogger logger)
    {
        logger.LogInformation("Login handler reached. Email: {Email}, Method: {Method}, Name: {Name}", 
            request.Email, request.Method, request.Name);

        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Method))
        {
            logger.LogWarning("Login request validation failed. Email or Method is missing. Email: {Email}, Method: {Method}", 
                request.Email ?? "null", request.Method ?? "null");
            return Results.Ok(new LoginResponse
            {
                Error = true,
                Description = "Email and Method are required",
                User = null
            });
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
            return Results.Ok(new LoginResponse
            {
                Error = true,
                Description = "An error occurred while processing your login request.",
                User = null
            });
        }

        logger.LogInformation("Login handler completed successfully. Returning user data. UserId: {UserId}", existingUser.Id);
        return Results.Ok(new LoginResponse
        {
            Error = false,
            Description = "",
            User = existingUser
        });
    }
}

