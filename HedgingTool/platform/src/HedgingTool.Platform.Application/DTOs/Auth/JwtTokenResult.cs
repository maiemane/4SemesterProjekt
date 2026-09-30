namespace HedgingTool.Platform.Application.DTOs.Auth;

public sealed class JwtTokenResult
{
    public string AccessToken { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
}
