using DatingBot.Domain.Entities;

namespace DatingBot.Application.Interfaces;

public interface IInactivityReminderService
{
    Task<IReadOnlyList<User>> GetUsersForInactivityReminderAsync(int inactivityDays, int limit = 100, CancellationToken cancellationToken = default);
    Task MarkReminderSentAsync(Guid userId, DateTime sentAt, CancellationToken cancellationToken = default);
    string GetRandomInactivityReminderKey();
}
