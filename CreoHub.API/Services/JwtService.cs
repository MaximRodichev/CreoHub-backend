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
        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, model.Id.ToString()),
            new Claim(ClaimTypes.Name, model.Name),
            new Claim(ClaimTypes.Role, model.Role),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim("telegram_id", model.TelegramId?.ToString() ?? ""),
        };

        // Only add shop_id when the user actually has a shop (avoid Guid.Parse("") crash)
        if (model.ShopId.HasValue && model.ShopId.Value != Guid.Empty)
            claims.Add(new Claim("shop_id", model.ShopId.Value.ToString()));
        
        if (!string.IsNullOrEmpty(model.EmailAddress))
        {
            claims.Add(new Claim(JwtRegisteredClaimNames.Email, model.EmailAddress));
        }

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.Now.AddDays(7), // Срок жизни токена
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}