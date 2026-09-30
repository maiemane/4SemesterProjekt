using HedgingTool.Platform.Domain.Entities;

namespace HedgingTool.Platform.Application.Interfaces;

public interface IPasswordVerificationService
{
    bool Verify(User user, string password);
}
