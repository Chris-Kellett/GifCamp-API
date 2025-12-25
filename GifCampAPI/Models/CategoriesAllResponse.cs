namespace GifCampAPI.Models;

public class CategoriesAllResponse
{
    public bool Error { get; set; }
    public string Description { get; set; } = string.Empty;
    public List<CategoryItem> Categories { get; set; } = new();
}

