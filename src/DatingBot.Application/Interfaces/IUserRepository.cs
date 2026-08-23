using DatingBot.Domain.Entities;

namespace DatingBot.Application.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByTelegramIdAsync(long telegramId, CancellationToken cancellationToken = default);
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(User user, CancellationToken cancellationToken = default);
    void Update(User user);
    Task<IReadOnlyList<User>> GetInactiveUsersAsync(DateTime cutoffDate, int limit = 100, CancellationToken cancellationToken = default);
    Task MarkInactivityReminderSentAsync(Guid userId, DateTime sentAt, CancellationToken cancellationToken = default);
    Task UpdateLastActiveAtAsync(long telegramId, DateTime activeAt, CancellationToken cancellationToken = default);
}
