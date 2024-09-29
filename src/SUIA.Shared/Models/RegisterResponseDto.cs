namespace SUIA.Shared.Models;
public record RegisterResponseDto(string TokenType, string AccessToken, long ExpiresIn, string RefreshToken);