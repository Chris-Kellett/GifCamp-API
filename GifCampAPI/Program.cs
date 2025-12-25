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
})
.WithName("Login")
.WithOpenApi();

// Category-add endpoint
app.MapPost("/category-add", async (CategoryAddRequest request, GifCampDbContext dbContext, ILogger<Program> logger) =>
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
})
.WithName("CategoryAdd")
.WithOpenApi();

// Category-all endpoint
app.MapPost("/category-all", async (CategoriesAllRequest request, GifCampDbContext dbContext, ILogger<Program> logger) =>
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
})
.WithName("CategoryAll")
.WithOpenApi();

// Category-delete endpoint
app.MapPost("/category-delete", async (CategoryDeleteRequest request, GifCampDbContext dbContext, ILogger<Program> logger) =>
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
})
.WithName("CategoryDelete")
.WithOpenApi();

app.Run();