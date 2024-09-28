namespace SUIA.Shared.Models;

public sealed class LoginResponse
{
    public string TokenType { get; set; } = default!;
    public string AccessToken { get; set; } = default!;
    public long ExpiresIn { get; set; }
    public string RefreshToken { get; set; } = default!;
    public string? Claims { get; set; }
}