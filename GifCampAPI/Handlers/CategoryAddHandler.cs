using Microsoft.EntityFrameworkCore;
using GifCampAPI.Data;
using GifCampAPI.Models;

namespace GifCampAPI.Handlers;

public static class CategoryAddHandler
{
    public static async Task<IResult> Handle(CategoryAddRequest request, GifCampDbContext dbContext, ILogger logger)
    {
        logger.LogInformation("Category-add handler reached. UserId: {UserId}, Name: {Name}", 
            request.UserId, request.Name);

        // Validate request
        if (request.UserId <= 0)
        {
            logger.LogWarning("Category-add request validation failed. Invalid UserId: {UserId}", request.UserId);
            return Results.Ok(new CategoryAddResponse
            {
                Error = true,
                Description = "Valid UserId is required",
                CategoryId = null
            });
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            logger.LogWarning("Category-add request validation failed. Name is missing. UserId: {UserId}", request.UserId);
            return Results.Ok(new CategoryAddResponse
            {
                Error = true,
                Description = "Name is required",
                CategoryId = null
            });
        }

        // Verify user exists
        logger.LogDebug("Verifying user exists. UserId: {UserId}", request.UserId);
        var userExists = await dbContext.Users.AnyAsync(u => u.Id == request.UserId);
        if (!userExists)
        {
            logger.LogWarning("Category-add request failed. User not found. UserId: {UserId}", request.UserId);
            return Results.Ok(new CategoryAddResponse
            {
                Error = true,
                Description = "User not found",
                CategoryId = null
            });
        }

        logger.LogDebug("Creating new category. UserId: {UserId}, Name: {Name}", request.UserId, request.Name);

        try
        {
            var now = DateTime.UtcNow;
            var newCategory = new Category
            {
                UserId = request.UserId,
                Name = request.Name.Trim(),
                CreatedAt = now
            };

            dbContext.Categories.Add(newCategory);
            logger.LogDebug("Category added to context. Saving changes to database...");
            
            await dbContext.SaveChangesAsync();
            
            logger.LogInformation("Category created successfully. CategoryId: {CategoryId}, UserId: {UserId}, Name: {Name}", 
                newCategory.Id, newCategory.UserId, newCategory.Name);

            return Results.Ok(new CategoryAddResponse
            {
                Error = false,
                Description = "",
                CategoryId = newCategory.Id
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating category. UserId: {UserId}, Name: {Name}", 
                request.UserId, request.Name);
            return Results.Ok(new CategoryAddResponse
            {
                Error = true,
                Description = "An error occurred while creating the category.",
                CategoryId = null
            });
        }
    }
}

