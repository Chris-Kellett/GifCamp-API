using Microsoft.EntityFrameworkCore;
using GifCampAPI.Data;
using GifCampAPI.Models;

namespace GifCampAPI.Handlers;

public static class CategoryDeleteHandler
{
    public static async Task<IResult> Handle(CategoryDeleteRequest request, GifCampDbContext dbContext, ILogger logger)
    {
        logger.LogInformation("Category-delete handler reached. UserId: {UserId}, CategoryId: {CategoryId}", 
            request.UserId, request.CategoryId);

        // Validate request
        if (request.UserId <= 0)
        {
            logger.LogWarning("Category-delete request validation failed. Invalid UserId: {UserId}", request.UserId);
            return Results.Ok(new CategoryDeleteResponse
            {
                Error = true,
                Description = "Valid UserId is required"
            });
        }

        if (request.CategoryId <= 0)
        {
            logger.LogWarning("Category-delete request validation failed. Invalid CategoryId: {CategoryId}", request.CategoryId);
            return Results.Ok(new CategoryDeleteResponse
            {
                Error = true,
                Description = "Valid CategoryId is required"
            });
        }

        logger.LogDebug("Verifying user exists. UserId: {UserId}", request.UserId);

        try
        {
            // Verify user exists
            var userExists = await dbContext.Users.AnyAsync(u => u.Id == request.UserId);
            if (!userExists)
            {
                logger.LogWarning("Category-delete request failed. User not found. UserId: {UserId}", request.UserId);
                return Results.Ok(new CategoryDeleteResponse
                {
                    Error = true,
                    Description = "User not found"
                });
            }

            logger.LogDebug("Verifying category exists and is owned by user. CategoryId: {CategoryId}, UserId: {UserId}", 
                request.CategoryId, request.UserId);

            // Verify category exists and is owned by the user
            var category = await dbContext.Categories
                .FirstOrDefaultAsync(c => c.Id == request.CategoryId && c.UserId == request.UserId);

            if (category == null)
            {
                logger.LogWarning("Category-delete request failed. Category not found or not owned by user. CategoryId: {CategoryId}, UserId: {UserId}", 
                    request.CategoryId, request.UserId);
                return Results.Ok(new CategoryDeleteResponse
                {
                    Error = true,
                    Description = "Category not found or you do not have permission to delete it"
                });
            }

            logger.LogInformation("Category found and verified. Deleting category. CategoryId: {CategoryId}, UserId: {UserId}, Name: {Name}", 
                category.Id, category.UserId, category.Name);

            // Delete the category
            dbContext.Categories.Remove(category);
            await dbContext.SaveChangesAsync();

            logger.LogInformation("Category deleted successfully. CategoryId: {CategoryId}, UserId: {UserId}", 
                request.CategoryId, request.UserId);

            return Results.Ok(new CategoryDeleteResponse
            {
                Error = false,
                Description = ""
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting category. CategoryId: {CategoryId}, UserId: {UserId}", 
                request.CategoryId, request.UserId);
            return Results.Ok(new CategoryDeleteResponse
            {
                Error = true,
                Description = "An error occurred while deleting the category."
            });
        }
    }
}

