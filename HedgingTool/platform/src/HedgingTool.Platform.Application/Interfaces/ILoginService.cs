using HedgingTool.Platform.Application.DTOs.Auth;

namespace HedgingTool.Platform.Application.Interfaces;

public interface ILoginService
{
    Task<LoginResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
}
