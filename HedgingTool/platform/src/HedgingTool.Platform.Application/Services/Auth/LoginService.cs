using HedgingTool.Platform.Application.DTOs.Auth;
using HedgingTool.Platform.Application.Interfaces;

namespace HedgingTool.Platform.Application.Services.Auth;

public sealed class LoginService : ILoginService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordVerificationService _passwordVerificationService;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    public LoginService(
        IUserRepository userRepository,
        IPasswordVerificationService passwordVerificationService,
        IJwtTokenGenerator jwtTokenGenerator)
    {
        _userRepository = userRepository;
        _passwordVerificationService = passwordVerificationService;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    public async Task<LoginResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return new LoginResult
            {
                IsSuccess = false,
                Error = "MissingCredentials"
            };
        }

        var user = await _userRepository.GetActiveByEmailAsync(request.Email, cancellationToken);

        if (user is null || !_passwordVerificationService.Verify(user, request.Password))
        {
            return new LoginResult
            {
                IsSuccess = false,
                Error = "InvalidCredentials"
            };
        }

        var token = _jwtTokenGenerator.CreateToken(user);

        return new LoginResult
        {
            IsSuccess = true,
            AccessToken = token.AccessToken,
            ExpiresAtUtc = token.ExpiresAtUtc,
            Name = user.Name,
            Email = user.Email,
            Role = user.Role
        };
    }
}
