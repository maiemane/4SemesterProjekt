using HedgingTool.Platform.Domain.Entities;

namespace HedgingTool.Platform.Application.Interfaces;

public interface IUserRepository
{
    Task<User?> GetActiveByEmailAsync(string email, CancellationToken cancellationToken = default);
}
