namespace GifCampAPI.Models;

public class CategoryAddRequest
{
    public int UserId { get; set; }
    public string Name { get; set; } = string.Empty;
}

