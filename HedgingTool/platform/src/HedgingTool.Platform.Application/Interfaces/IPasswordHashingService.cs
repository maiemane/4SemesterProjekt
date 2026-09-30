using HedgingTool.Platform.Domain.Entities;

namespace HedgingTool.Platform.Application.Interfaces;

public interface IPasswordHashingService
{
    string Hash(User user, string password);
}
