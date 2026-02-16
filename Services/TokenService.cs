using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using WebApiPPK.Dtos;
using WebApiPPK.Models;

namespace WebApiPPK.Services;

/// <summary>
/// Serwis odpowiedzialny za tworzenie tokenów JWT (bez mieszania logiki w kontrolerach).
/// </summary>
public class TokenService
{
    private readonly IConfiguration _config;

    public TokenService(IConfiguration config) => _config = config;

    public AuthResponseDto CreateToken(ApplicationUser user)
    {
        var jwt = _config.GetSection("Jwt");

        var keyBytes = Encoding.UTF8.GetBytes(jwt["Key"]!);
        var key = new SymmetricSecurityKey(keyBytes);

        var expires = DateTime.UtcNow.AddMinutes(int.Parse(jwt["ExpiresMinutes"]!));

        //minimalny zestaw claimów do identyfikacji użytkownika w API
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Email, user.Email ?? string.Empty),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: jwt["Issuer"],
            audience: jwt["Audience"],
            claims: claims,
            expires: expires,
            signingCredentials: creds
        );

        return new AuthResponseDto(new JwtSecurityTokenHandler().WriteToken(token), expires);
    }
}

