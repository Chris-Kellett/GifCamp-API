namespace GifCampAPI.Services;

public class StorageConfiguration
{
    public string StorageProvider { get; set; } = "local"; // "local" or "digitalocean"
    
    // DigitalOcean Spaces configuration (S3-compatible)
    public string? DigitalOceanSpacesEndpoint { get; set; }
    public string? DigitalOceanSpacesAccessKey { get; set; }
    public string? DigitalOceanSpacesSecretKey { get; set; }
    public string? DigitalOceanSpacesBucket { get; set; }
    public string? DigitalOceanSpacesRegion { get; set; }
    public string? DigitalOceanSpacesCdnUrl { get; set; }
}

