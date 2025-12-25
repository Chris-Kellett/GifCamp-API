namespace GifCampAPI.Models;

public class LoginResponse
{
    public bool Error { get; set; }
    public string Description { get; set; } = string.Empty;
    public User? User { get; set; }
}

