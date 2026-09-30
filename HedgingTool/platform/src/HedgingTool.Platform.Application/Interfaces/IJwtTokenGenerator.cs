using HedgingTool.Platform.Application.DTOs.Auth;
using HedgingTool.Platform.Domain.Entities;

namespace HedgingTool.Platform.Application.Interfaces;

public interface IJwtTokenGenerator
{
    JwtTokenResult CreateToken(User user);
}
