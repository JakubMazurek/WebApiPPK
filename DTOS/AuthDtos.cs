namespace WebApiPPK.Dtos;

/// <summary>DTO rejestracji.</summary>
public record RegisterDto(string Email, string Password);

/// <summary>DTO logowania.</summary>
public record LoginDto(string Email, string Password);

/// <summary>Odpowiedź z tokenem JWT.</summary>
public record AuthResponseDto(string Token, DateTime ExpiresAtUtc);
