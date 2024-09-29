namespace SUIA.Shared.Models;

public record LoginResponseDto(string TokenType, string AccessToken, long ExpiresIn, string RefreshToken)
{
    public string? Claims { get; set; }    
}
