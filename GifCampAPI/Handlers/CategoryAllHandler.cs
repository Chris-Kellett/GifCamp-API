using Microsoft.EntityFrameworkCore;
using GifCampAPI.Data;
using GifCampAPI.Models;

namespace GifCampAPI.Handlers;

public static class CategoryAllHandler
{
    public static async Task<IResult> Handle(CategoriesAllRequest request, GifCampDbContext dbContext, ILogger logger)
    {
        logger.LogInformation("Category-all handler reached. UserId: {UserId}", request.UserId);

        // Validate request
        if (request.UserId <= 0)
        {
            logger.LogWarning("Category-all request validation failed. Invalid UserId: {UserId}", request.UserId);
            return Results.Ok(new CategoriesAllResponse
            {
                Error = true,
                Description = "Valid UserId is required",
                Categories = new List<CategoryItem>()
            });
        }

        logger.LogDebug("Fetching categories for UserId: {UserId}", request.UserId);

        try
        {
            // Verify user exists
            var userExists = await dbContext.Users.AnyAsync(u => u.Id == request.UserId);
            if (!userExists)
            {
                logger.LogWarning("Category-all request failed. User not found. UserId: {UserId}", request.UserId);
                return Results.Ok(new CategoriesAllResponse
                {
                    Error = true,
                    Description = "User not found",
                    Categories = new List<CategoryItem>()
                });
            }

            // Fetch all categories for the user
            var categories = await dbContext.Categories
                .Where(c => c.UserId == request.UserId)
                .OrderBy(c => c.Name)
                .Select(c => new CategoryItem
                {
                    Id = c.Id,
                    Name = c.Name
                })
                .ToListAsync();

            logger.LogInformation("Categories retrieved successfully. UserId: {UserId}, Count: {Count}", 
                request.UserId, categories.Count);

            return Results.Ok(new CategoriesAllResponse
            {
                Error = false,
                Description = "",
                Categories = categories
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching categories. UserId: {UserId}", request.UserId);
            return Results.Ok(new CategoriesAllResponse
            {
                Error = true,
                Description = "An error occurred while fetching categories.",
                Categories = new List<CategoryItem>()
            });
        }
    }
}

