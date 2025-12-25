namespace GifCampAPI.Models;

public class Image
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int CategoryId { get; set; }
    public string? ImageUrl { get; set; }
    public string? StorageUrl { get; set; }
}

