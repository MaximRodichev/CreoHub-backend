using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CreoHub.API.Models;
using Microsoft.IdentityModel.Tokens;

public class JwtService
{
    private readonly IConfiguration _config;

    public JwtService(IConfiguration config)
    {
        _config = config;
    }

    public string GenerateToken(UserClaimsModel model)
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        // Переносим данные из вашей модели в Claims
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, model.Id.ToString()),
            new Claim(ClaimTypes.Name, model.Name),
            new Claim(JwtRegisteredClaimNames.Email, model.EmailAddress),
            new Claim("telegram_id", model.TelegramId.ToString()),
            new Claim("shop_id", model.ShopId.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.Now.AddDays(7), // Срок жизни токена
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}