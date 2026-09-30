using HedgingTool.Platform.Application.Interfaces;
using HedgingTool.Platform.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace HedgingTool.Platform.Infrastructure.Auth;

public sealed class PasswordVerificationService : IPasswordVerificationService
{
    private readonly PasswordHasher<User> _passwordHasher = new();

    public bool Verify(User user, string password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            return false;
        }

        var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);

        return result is PasswordVerificationResult.Success or PasswordVerificationResult.SuccessRehashNeeded;
    }
}
