using DatingBot.Domain.Entities;

namespace DatingBot.Application.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByTelegramIdAsync(long telegramId, CancellationToken cancellationToken = default);
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(User user, CancellationToken cancellationToken = default);
    void Update(User user);
}
