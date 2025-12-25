using Amazon.S3;
using Amazon.S3.Model;
using GifCampAPI.Services;

namespace GifCampAPI.Services;

public class ImageStorageService
{
    private readonly StorageConfiguration _config;
    private readonly ILogger _logger;

    public ImageStorageService(StorageConfiguration config, ILogger<ImageStorageService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task<string?> SaveImageAsync(IFormFile file, int userId, string fileExtension)
    {
        return _config.StorageProvider.ToLowerInvariant() switch
        {
            "local" => await SaveImageLocalAsync(file, userId, fileExtension),
            "digitalocean" => await SaveImageDigitalOceanAsync(file, userId, fileExtension),
            _ => throw new NotSupportedException($"Storage provider '{_config.StorageProvider}' is not supported")
        };
    }

    private async Task<string?> SaveImageLocalAsync(IFormFile file, int userId, string fileExtension)
    {
        try
        {
            // Create directory structure: Content/{userId}/
            var contentDir = Path.Combine("Content", userId.ToString());
            Directory.CreateDirectory(contentDir);

            // Generate unique filename with GUID
            var fileName = $"{Guid.NewGuid()}{fileExtension}";
            var filePath = Path.Combine(contentDir, fileName);
            var storageUrl = $"Content/{userId}/{fileName}";

            // Save file
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            _logger.LogInformation("Image blob saved locally. Path: {Path}", storageUrl);
            return storageUrl;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving image blob locally");
            return null;
        }
    }

    private async Task<string?> SaveImageDigitalOceanAsync(IFormFile file, int userId, string fileExtension)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_config.DigitalOceanSpacesEndpoint) ||
                string.IsNullOrWhiteSpace(_config.DigitalOceanSpacesAccessKey) ||
                string.IsNullOrWhiteSpace(_config.DigitalOceanSpacesSecretKey) ||
                string.IsNullOrWhiteSpace(_config.DigitalOceanSpacesBucket))
            {
                _logger.LogError("DigitalOcean Spaces configuration is incomplete");
                return null;
            }

            // Configure S3 client for DigitalOcean Spaces
            var config = new AmazonS3Config
            {
                ServiceURL = _config.DigitalOceanSpacesEndpoint,
                ForcePathStyle = false,
                UseHttp = false
            };

            var s3Client = new AmazonS3Client(
                _config.DigitalOceanSpacesAccessKey,
                _config.DigitalOceanSpacesSecretKey,
                config);

            // Generate unique filename with GUID
            var fileName = $"{userId}/{Guid.NewGuid()}{fileExtension}";

            // Upload file to Spaces
            using (var fileStream = file.OpenReadStream())
            {
                var putRequest = new PutObjectRequest
                {
                    BucketName = _config.DigitalOceanSpacesBucket,
                    Key = fileName,
                    InputStream = fileStream,
                    ContentType = file.ContentType,
                    CannedACL = S3CannedACL.PublicRead // Make file publicly accessible
                };

                await s3Client.PutObjectAsync(putRequest);
            }

            // Construct the storage URL
            // If CDN URL is configured, use it; otherwise use the Spaces endpoint
            string storageUrl;
            if (!string.IsNullOrWhiteSpace(_config.DigitalOceanSpacesCdnUrl))
            {
                storageUrl = $"{_config.DigitalOceanSpacesCdnUrl.TrimEnd('/')}/{fileName}";
            }
            else
            {
                var endpoint = _config.DigitalOceanSpacesEndpoint.TrimEnd('/');
                storageUrl = $"{endpoint}/{_config.DigitalOceanSpacesBucket}/{fileName}";
            }

            // Store the relative path in database (without CDN/endpoint prefix)
            var dbStorageUrl = fileName;

            _logger.LogInformation("Image blob saved to DigitalOcean Spaces. StorageUrl: {StorageUrl}, PublicUrl: {PublicUrl}", 
                dbStorageUrl, storageUrl);
            
            return dbStorageUrl;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving image blob to DigitalOcean Spaces");
            return null;
        }
    }

    public string GetPublicUrl(string? storageUrl)
    {
        if (string.IsNullOrWhiteSpace(storageUrl))
            return string.Empty;

        // If using local storage, return as-is (relative path)
        if (_config.StorageProvider.ToLowerInvariant() == "local")
        {
            return storageUrl;
        }

        // If using DigitalOcean Spaces, construct full URL
        if (_config.StorageProvider.ToLowerInvariant() == "digitalocean")
        {
            if (!string.IsNullOrWhiteSpace(_config.DigitalOceanSpacesCdnUrl))
            {
                return $"{_config.DigitalOceanSpacesCdnUrl.TrimEnd('/')}/{storageUrl}";
            }
            else if (!string.IsNullOrWhiteSpace(_config.DigitalOceanSpacesEndpoint))
            {
                var endpoint = _config.DigitalOceanSpacesEndpoint.TrimEnd('/');
                return $"{endpoint}/{_config.DigitalOceanSpacesBucket}/{storageUrl}";
            }
        }

        return storageUrl;
    }
}

