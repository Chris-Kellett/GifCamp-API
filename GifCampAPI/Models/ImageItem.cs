namespace GifCampAPI.Models;

public class ImageItem
{
    public int Id { get; set; }
    public string Url { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
}

