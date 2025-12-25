namespace GifCampAPI.Models;

public class ImageGetResponse
{
    public bool Error { get; set; }
    public string Description { get; set; } = string.Empty;
    public List<ImageItem> Images { get; set; } = new();
}

