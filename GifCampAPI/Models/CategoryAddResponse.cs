namespace GifCampAPI.Models;

public class CategoryAddResponse
{
    public bool Error { get; set; }
    public string Description { get; set; } = string.Empty;
    public int? CategoryId { get; set; }
}

