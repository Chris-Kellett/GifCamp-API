namespace GifCampAPI.Models;

public class ImageAddLinkRequest
{
    public int UserId { get; set; }
    public int CategoryId { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
}

