using Microsoft.EntityFrameworkCore;
using GifCampAPI.Data;
using GifCampAPI.Models;
using GifCampAPI.Services;

namespace GifCampAPI.Handlers;

public static class ImageHandlers
{
    // Shared validation functions
    public static async Task<bool> ValidateUserIdAsync(int userId, GifCampDbContext dbContext)
    {
        return await dbContext.Users.AnyAsync(u => u.Id == userId);
    }

    public static bool ValidateImageUrl(string imageUrl)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
            return false;

        // Basic URL validation
        return Uri.TryCreate(imageUrl, UriKind.Absolute, out var uri) &&
               (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }

    public static async Task<string?> SaveImageBlobAsync(IFormFile file, int userId, ILogger logger, ImageStorageService storageService)
    {
        // Validate file is an image
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".bmp" };
        var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
        
        if (!allowedExtensions.Contains(fileExtension))
        {
            logger.LogWarning("Invalid file extension: {Extension}", fileExtension);
            return null;
        }

        // Validate file size (10MB limit)
        const long maxFileSize = 10 * 1024 * 1024; // 10MB
        if (file.Length > maxFileSize)
        {
            logger.LogWarning("File size exceeds limit. Size: {Size} bytes", file.Length);
            return null;
        }

        // Validate MIME type
        var allowedMimeTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/gif", "image/webp", "image/bmp" };
        if (!allowedMimeTypes.Contains(file.ContentType.ToLowerInvariant()))
        {
            logger.LogWarning("Invalid MIME type: {MimeType}", file.ContentType);
            return null;
        }

        // Use storage service to save the image
        return await storageService.SaveImageAsync(file, userId, fileExtension);
    }

    // Images-add-link handler
    public static async Task<IResult> HandleAddLink(ImageAddLinkRequest request, GifCampDbContext dbContext, ILogger logger)
    {
        logger.LogInformation("Images-add-link handler reached. UserId: {UserId}, CategoryId: {CategoryId}, ImageUrl: {ImageUrl}", 
            request.UserId, request.CategoryId, request.ImageUrl);

        // Validate userId (cannot be 0)
        if (request.UserId <= 0)
        {
            logger.LogWarning("Images-add-link validation failed. Invalid UserId: {UserId}", request.UserId);
            return Results.Ok(new ImageAddResponse
            {
                Error = true,
                Description = "Valid UserId is required",
                ImageId = null
            });
        }

        // Validate categoryId (can be 0)
        if (request.CategoryId < 0)
        {
            logger.LogWarning("Images-add-link validation failed. Invalid CategoryId: {CategoryId}", request.CategoryId);
            return Results.Ok(new ImageAddResponse
            {
                Error = true,
                Description = "CategoryId must be 0 or greater",
                ImageId = null
            });
        }

        // Validate ImageUrl
        if (!ValidateImageUrl(request.ImageUrl))
        {
            logger.LogWarning("Images-add-link validation failed. Invalid ImageUrl: {ImageUrl}", request.ImageUrl);
            return Results.Ok(new ImageAddResponse
            {
                Error = true,
                Description = "Valid ImageUrl is required",
                ImageId = null
            });
        }

        // Validate userId exists
        if (!await ValidateUserIdAsync(request.UserId, dbContext))
        {
            logger.LogWarning("Images-add-link validation failed. User not found. UserId: {UserId}", request.UserId);
            return Results.Ok(new ImageAddResponse
            {
                Error = true,
                Description = "User not found",
                ImageId = null
            });
        }

        try
        {
            var newImage = new Image
            {
                UserId = request.UserId,
                CategoryId = request.CategoryId,
                ImageUrl = request.ImageUrl.Trim(),
                StorageUrl = null
            };

            dbContext.Images.Add(newImage);
            await dbContext.SaveChangesAsync();

            logger.LogInformation("Image link added successfully. ImageId: {ImageId}, UserId: {UserId}, CategoryId: {CategoryId}", 
                newImage.Id, newImage.UserId, newImage.CategoryId);

            return Results.Ok(new ImageAddResponse
            {
                Error = false,
                Description = "",
                ImageId = newImage.Id
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error adding image link. UserId: {UserId}, CategoryId: {CategoryId}", 
                request.UserId, request.CategoryId);
            return Results.Ok(new ImageAddResponse
            {
                Error = true,
                Description = "An error occurred while adding the image.",
                ImageId = null
            });
        }
    }

    // Images-add-blob handler
    public static async Task<IResult> HandleAddBlob(HttpRequest httpRequest, GifCampDbContext dbContext, ILogger logger, ImageStorageService storageService)
    {
        logger.LogInformation("Images-add-blob handler reached");

        // Parse form data
        if (!httpRequest.HasFormContentType)
        {
            logger.LogWarning("Images-add-blob validation failed. Request is not multipart/form-data");
            return Results.Ok(new ImageAddResponse
            {
                Error = true,
                Description = "Request must be multipart/form-data",
                ImageId = null
            });
        }

        var form = await httpRequest.ReadFormAsync();
        
        if (!int.TryParse(form["userId"].ToString(), out var userId))
        {
            logger.LogWarning("Images-add-blob validation failed. Invalid UserId");
            return Results.Ok(new ImageAddResponse
            {
                Error = true,
                Description = "Valid UserId is required",
                ImageId = null
            });
        }

        if (!int.TryParse(form["categoryId"].ToString(), out var categoryId))
        {
            logger.LogWarning("Images-add-blob validation failed. Invalid CategoryId");
            return Results.Ok(new ImageAddResponse
            {
                Error = true,
                Description = "Valid CategoryId is required",
                ImageId = null
            });
        }

        var file = form.Files.GetFile("image");
        if (file == null || file.Length == 0)
        {
            logger.LogWarning("Images-add-blob validation failed. No image file provided");
            return Results.Ok(new ImageAddResponse
            {
                Error = true,
                Description = "Image file is required",
                ImageId = null
            });
        }

        logger.LogInformation("Images-add-blob processing. UserId: {UserId}, CategoryId: {CategoryId}, FileName: {FileName}", 
            userId, categoryId, file.FileName);

        // Validate userId (cannot be 0)
        if (userId <= 0)
        {
            logger.LogWarning("Images-add-blob validation failed. Invalid UserId: {UserId}", userId);
            return Results.Ok(new ImageAddResponse
            {
                Error = true,
                Description = "Valid UserId is required",
                ImageId = null
            });
        }

        // Validate categoryId (can be 0)
        if (categoryId < 0)
        {
            logger.LogWarning("Images-add-blob validation failed. Invalid CategoryId: {CategoryId}", categoryId);
            return Results.Ok(new ImageAddResponse
            {
                Error = true,
                Description = "CategoryId must be 0 or greater",
                ImageId = null
            });
        }

        // Validate userId exists
        if (!await ValidateUserIdAsync(userId, dbContext))
        {
            logger.LogWarning("Images-add-blob validation failed. User not found. UserId: {UserId}", userId);
            return Results.Ok(new ImageAddResponse
            {
                Error = true,
                Description = "User not found",
                ImageId = null
            });
        }

        // Validate and save image blob
        var storageUrl = await SaveImageBlobAsync(file, userId, logger, storageService);
        if (storageUrl == null)
        {
            logger.LogWarning("Images-add-blob validation failed. Invalid image file");
            return Results.Ok(new ImageAddResponse
            {
                Error = true,
                Description = "Invalid image file. Must be a valid image format and under 10MB",
                ImageId = null
            });
        }

        try
        {
            var newImage = new Image
            {
                UserId = userId,
                CategoryId = categoryId,
                ImageUrl = null,
                StorageUrl = storageUrl
            };

            dbContext.Images.Add(newImage);
            await dbContext.SaveChangesAsync();

            logger.LogInformation("Image blob added successfully. ImageId: {ImageId}, UserId: {UserId}, CategoryId: {CategoryId}, StorageUrl: {StorageUrl}", 
                newImage.Id, newImage.UserId, newImage.CategoryId, newImage.StorageUrl);

            return Results.Ok(new ImageAddResponse
            {
                Error = false,
                Description = "",
                ImageId = newImage.Id
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error adding image blob. UserId: {UserId}, CategoryId: {CategoryId}", 
                userId, categoryId);
            return Results.Ok(new ImageAddResponse
            {
                Error = true,
                Description = "An error occurred while adding the image.",
                ImageId = null
            });
        }
    }

    // Images-get handler
    public static async Task<IResult> HandleGet(ImageGetRequest request, GifCampDbContext dbContext, ILogger logger, ImageStorageService storageService)
    {
        logger.LogInformation("Images-get handler reached. UserId: {UserId}, CategoryId: {CategoryId}", 
            request.UserId, request.CategoryId);

        // Validate userId
        if (request.UserId <= 0)
        {
            logger.LogWarning("Images-get validation failed. Invalid UserId: {UserId}", request.UserId);
            return Results.Ok(new ImageGetResponse
            {
                Error = true,
                Description = "Valid UserId is required",
                Images = new List<ImageItem>()
            });
        }

        // Validate categoryId (-1, 0, or positive)
        if (request.CategoryId < -1)
        {
            logger.LogWarning("Images-get validation failed. Invalid CategoryId: {CategoryId}", request.CategoryId);
            return Results.Ok(new ImageGetResponse
            {
                Error = true,
                Description = "CategoryId must be -1, 0, or a positive integer",
                Images = new List<ImageItem>()
            });
        }

        // Validate userId exists
        if (!await ValidateUserIdAsync(request.UserId, dbContext))
        {
            logger.LogWarning("Images-get validation failed. User not found. UserId: {UserId}", request.UserId);
            return Results.Ok(new ImageGetResponse
            {
                Error = true,
                Description = "User not found",
                Images = new List<ImageItem>()
            });
        }

        try
        {
            var query = dbContext.Images.Where(i => i.UserId == request.UserId);

            // Filter by categoryId
            if (request.CategoryId == -1)
            {
                // Return all images for the user
                logger.LogDebug("Fetching all images for UserId: {UserId}", request.UserId);
            }
            else if (request.CategoryId == 0)
            {
                // Return images with CategoryId = 0 (no category)
                query = query.Where(i => i.CategoryId == 0);
                logger.LogDebug("Fetching images with CategoryId = 0 for UserId: {UserId}", request.UserId);
            }
            else
            {
                // Return images with specific CategoryId
                query = query.Where(i => i.CategoryId == request.CategoryId);
                logger.LogDebug("Fetching images with CategoryId = {CategoryId} for UserId: {UserId}", 
                    request.CategoryId, request.UserId);
            }

            var images = await query.ToListAsync();
            
            var imageItems = images.Select(i => new ImageItem
            {
                Id = i.Id,
                Url = !string.IsNullOrWhiteSpace(i.StorageUrl) 
                    ? storageService.GetPublicUrl(i.StorageUrl) 
                    : (i.ImageUrl ?? string.Empty)
            }).ToList();

            logger.LogInformation("Images retrieved successfully. UserId: {UserId}, CategoryId: {CategoryId}, Count: {Count}", 
                request.UserId, request.CategoryId, imageItems.Count);

            return Results.Ok(new ImageGetResponse
            {
                Error = false,
                Description = "",
                Images = imageItems
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching images. UserId: {UserId}, CategoryId: {CategoryId}", 
                request.UserId, request.CategoryId);
            return Results.Ok(new ImageGetResponse
            {
                Error = true,
                Description = "An error occurred while fetching images.",
                Images = new List<ImageItem>()
            });
        }
    }
}

