using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using HedgingTool.Platform.Application.DTOs.Auth;
using HedgingTool.Platform.Application.Interfaces;
using HedgingTool.Platform.Domain.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace HedgingTool.Platform.Infrastructure.Auth;

public sealed class JwtTokenGenerator : IJwtTokenGenerator
{
    private readonly JwtOptions _options;

    public JwtTokenGenerator(IOptions<JwtOptions> options)
    {
        _options = options.Value;
    }

    public JwtTokenResult CreateToken(User user)
    {
        var now = DateTime.UtcNow;
        var expiresAt = now.AddMinutes(_options.ExpirationMinutes);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.UniqueName, user.Name),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Name),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role)
        };

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: now,
            expires: expiresAt,
            signingCredentials: credentials);

        return new JwtTokenResult
        {
            AccessToken = new JwtSecurityTokenHandler().WriteToken(token),
            ExpiresAtUtc = expiresAt
        };
    }
}
