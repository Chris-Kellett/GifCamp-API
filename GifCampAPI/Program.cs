using Microsoft.EntityFrameworkCore;
using GifCampAPI.Data;
using GifCampAPI.Models;
using GifCampAPI.Handlers;
using GifCampAPI.Services;
using DotNetEnv;
using Microsoft.Extensions.FileProviders;

// Load .env file if it exists
try
{
    Env.Load();
}
catch (FileNotFoundException)
{
    // .env file is optional, continue without it
}

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure storage service
var storageConfig = new StorageConfiguration
{
    StorageProvider = Environment.GetEnvironmentVariable("STORAGE_PROVIDER") 
        ?? builder.Configuration["Storage:Provider"] 
        ?? (builder.Environment.IsDevelopment() ? "local" : "local"),
    
    BaseUrl = Environment.GetEnvironmentVariable("BASE_URL")
        ?? builder.Configuration["Storage:BaseUrl"],
    
    DigitalOceanSpacesEndpoint = Environment.GetEnvironmentVariable("DO_SPACES_ENDPOINT")
        ?? builder.Configuration["Storage:DigitalOcean:Endpoint"],
    
    DigitalOceanSpacesAccessKey = Environment.GetEnvironmentVariable("DO_SPACES_ACCESS_KEY")
        ?? builder.Configuration["Storage:DigitalOcean:AccessKey"],
    
    DigitalOceanSpacesSecretKey = Environment.GetEnvironmentVariable("DO_SPACES_SECRET_KEY")
        ?? builder.Configuration["Storage:DigitalOcean:SecretKey"],
    
    DigitalOceanSpacesBucket = Environment.GetEnvironmentVariable("DO_SPACES_BUCKET")
        ?? builder.Configuration["Storage:DigitalOcean:Bucket"],
    
    DigitalOceanSpacesRegion = Environment.GetEnvironmentVariable("DO_SPACES_REGION")
        ?? builder.Configuration["Storage:DigitalOcean:Region"],
    
    DigitalOceanSpacesCdnUrl = Environment.GetEnvironmentVariable("DO_SPACES_CDN_URL")
        ?? builder.Configuration["Storage:DigitalOcean:CdnUrl"]
};

builder.Services.AddSingleton(storageConfig);
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ImageStorageService>();

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

// Enable static files from Content directory
var contentPath = Path.Combine(app.Environment.ContentRootPath, "Content");
Directory.CreateDirectory(contentPath); // Ensure directory exists

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(contentPath),
    RequestPath = "/Content"
});

// Login endpoint
app.MapPost("/login", async (LoginRequest request, GifCampDbContext dbContext, ILogger<Program> logger) =>
    await LoginHandler.Handle(request, dbContext, logger))
    .WithName("Login")
    .WithOpenApi();

// Category-add endpoint
app.MapPost("/category-add", async (CategoryAddRequest request, GifCampDbContext dbContext, ILogger<Program> logger) =>
    await CategoryAddHandler.Handle(request, dbContext, logger))
    .WithName("CategoryAdd")
    .WithOpenApi();

// Category-all endpoint
app.MapPost("/category-all", async (CategoriesAllRequest request, GifCampDbContext dbContext, ILogger<Program> logger) =>
    await CategoryAllHandler.Handle(request, dbContext, logger))
    .WithName("CategoryAll")
    .WithOpenApi();

// Category-delete endpoint
app.MapPost("/category-delete", async (CategoryDeleteRequest request, GifCampDbContext dbContext, ILogger<Program> logger) =>
    await CategoryDeleteHandler.Handle(request, dbContext, logger))
    .WithName("CategoryDelete")
    .WithOpenApi();

// Images-add-link endpoint
app.MapPost("/images-add-link", async (ImageAddLinkRequest request, GifCampDbContext dbContext, ILogger<Program> logger) =>
    await ImageHandlers.HandleAddLink(request, dbContext, logger))
    .WithName("ImageAddLink")
    .WithOpenApi();

// Images-add-blob endpoint
app.MapPost("/images-add-blob", async (HttpRequest httpRequest, GifCampDbContext dbContext, ILogger<Program> logger, ImageStorageService storageService) =>
    await ImageHandlers.HandleAddBlob(httpRequest, dbContext, logger, storageService))
    .WithName("ImageAddBlob")
    .WithOpenApi();

// Images-get endpoint
app.MapPost("/images-get", async (ImageGetRequest request, GifCampDbContext dbContext, ILogger<Program> logger, ImageStorageService storageService) =>
    await ImageHandlers.HandleGet(request, dbContext, logger, storageService))
    .WithName("ImageGet")
    .WithOpenApi();

// Images-delete endpoint
app.MapPost("/images-delete", async (ImageDeleteRequest request, GifCampDbContext dbContext, ILogger<Program> logger) =>
    await ImageHandlers.HandleDelete(request, dbContext, logger))
    .WithName("ImageDelete")
    .WithOpenApi();

app.Run();
