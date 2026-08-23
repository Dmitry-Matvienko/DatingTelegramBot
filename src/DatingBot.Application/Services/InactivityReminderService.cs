using DatingBot.Application.Interfaces;
using DatingBot.Domain.Entities;

namespace DatingBot.Application.Services;

public class InactivityReminderService(IUserRepository userRepository) : IInactivityReminderService
{
    private static readonly string[] ReminderKeys =
    [
        "Notification_Inactivity_1",
        "Notification_Inactivity_2",
        "Notification_Inactivity_3",
        "Notification_Inactivity_4",
        "Notification_Inactivity_5",
        "Notification_Inactivity_6",
        "Notification_Inactivity_7",
        "Notification_Inactivity_8",
        "Notification_Inactivity_9",
        "Notification_Inactivity_10"
    ];

    public async Task<IReadOnlyList<User>> GetUsersForInactivityReminderAsync(int inactivityDays, int limit = 100, CancellationToken cancellationToken = default)
    {
        var cutoff = DateTime.UtcNow.AddDays(-inactivityDays);
        return await userRepository.GetInactiveUsersAsync(cutoff, limit, cancellationToken);
    }

    public async Task MarkReminderSentAsync(Guid userId, DateTime sentAt, CancellationToken cancellationToken = default)
    {
        await userRepository.MarkInactivityReminderSentAsync(userId, sentAt, cancellationToken);
    }

    public string GetRandomInactivityReminderKey()
    {
        var index = Random.Shared.Next(ReminderKeys.Length);
        return ReminderKeys[index];
    }
}
