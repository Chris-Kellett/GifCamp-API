namespace GifCampAPI.Models;

public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Picture { get; set; }
    public string Method { get; set; } = string.Empty;
    public DateTime LastLogin { get; set; }
    public DateTime FirstLogin { get; set; }
}

