namespace SUIA.Shared.Models;
public class RegisterResponse
{
    public string TokenType { get; set; } = default!;
    public string AccessToken { get; set; } = default!;
    public long ExpiresIn { get; set; }
    public string RefreshToken { get; set; } = default!;
}
