using DatingBot.Domain.Entities;
using DatingBot.Domain.Enums;

namespace DatingBot.Application.Interfaces;

public interface IInterestRepository
{
    Task<IReadOnlyList<Interest>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Interest?> GetByCodeAsync(InterestType code, CancellationToken cancellationToken = default);
}
