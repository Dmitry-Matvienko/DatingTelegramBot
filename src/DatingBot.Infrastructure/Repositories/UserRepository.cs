using DatingBot.Application.Interfaces;
using DatingBot.Domain.Entities;
using DatingBot.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DatingBot.Infrastructure.Repositories;

public class UserRepository(AppDbContext dbContext) : IUserRepository
{
    public async Task<User?> GetByTelegramIdAsync(long telegramId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Users
            .FirstOrDefaultAsync(u => u.TelegramId == telegramId, cancellationToken);
    }

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    public async Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        await dbContext.Users.AddAsync(user, cancellationToken);
    }

    public void Update(User user)
    {
        dbContext.Users.Update(user);
    }

    public async Task<IReadOnlyList<User>> GetInactiveUsersAsync(DateTime cutoffDate, int limit = 100, CancellationToken cancellationToken = default)
    {
        return await dbContext.Users
            .Include(u => u.Profile)
            .Where(u => u.State != Domain.Enums.UserState.Banned
                     && u.Profile != null
                     && u.Profile.IsCompleted
                     && u.LastActiveAt <= cutoffDate
                     && (u.LastInactivityReminderSentAt == null || u.LastInactivityReminderSentAt <= cutoffDate))
            .OrderBy(u => u.LastInactivityReminderSentAt ?? DateTime.MinValue)
            .ThenBy(u => u.LastActiveAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task MarkInactivityReminderSentAsync(Guid userId, DateTime sentAt, CancellationToken cancellationToken = default)
    {
        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is not null)
        {
            user.LastInactivityReminderSentAt = sentAt;
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task UpdateLastActiveAtAsync(long telegramId, DateTime activeAt, CancellationToken cancellationToken = default)
    {
        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.TelegramId == telegramId, cancellationToken);
        if (user is not null)
        {
            user.LastActiveAt = activeAt;
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
