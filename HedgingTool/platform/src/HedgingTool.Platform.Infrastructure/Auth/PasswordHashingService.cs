using HedgingTool.Platform.Application.Interfaces;
using HedgingTool.Platform.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace HedgingTool.Platform.Infrastructure.Auth;

public sealed class PasswordHashingService : IPasswordHashingService
{
    private readonly PasswordHasher<User> _passwordHasher = new();

    public string Hash(User user, string password)
    {
        return _passwordHasher.HashPassword(user, password);
    }
}
