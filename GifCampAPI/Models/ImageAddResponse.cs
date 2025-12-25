namespace GifCampAPI.Models;

public class ImageAddResponse
{
    public bool Error { get; set; }
    public string Description { get; set; } = string.Empty;
    public int? ImageId { get; set; }
}

