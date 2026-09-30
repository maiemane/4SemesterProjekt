using HedgingTool.Platform.Application.DTOs.Auth;
using HedgingTool.Platform.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HedgingTool.Platform.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AuthController : ControllerBase
{
    private readonly ILoginService _loginService;

    public AuthController(ILoginService loginService)
    {
        _loginService = loginService;
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<LoginResult>> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await _loginService.LoginAsync(request, cancellationToken);

        if (!result.IsSuccess)
        {
            return result.Error switch
            {
                "MissingCredentials" => BadRequest("Email og password er påkrævet."),
                "InvalidCredentials" => Unauthorized(),
                _ => StatusCode(StatusCodes.Status500InternalServerError)
            };
        }

        return Ok(new LoginResult
        {
            IsSuccess = true,
            AccessToken = result.AccessToken,
            ExpiresAtUtc = result.ExpiresAtUtc,
            Name = result.Name,
            Email = result.Email,
            Role = result.Role
        });
    }
}
