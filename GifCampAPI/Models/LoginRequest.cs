namespace GifCampAPI.Models;

public class LoginRequest
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Picture { get; set; }
    public string Method { get; set; } = string.Empty;
}

