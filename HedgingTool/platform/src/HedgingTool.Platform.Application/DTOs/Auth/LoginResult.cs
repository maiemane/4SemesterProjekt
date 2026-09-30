namespace HedgingTool.Platform.Application.DTOs.Auth;

public sealed class LoginResult
{
    public bool IsSuccess { get; set; }
    public string? Error { get; set; }
    public string? AccessToken { get; set; }
    public DateTime? ExpiresAtUtc { get; set; }
    public string? Name { get; set; }
    public string? Email { get; set; }
    public string? Role { get; set; }
}
