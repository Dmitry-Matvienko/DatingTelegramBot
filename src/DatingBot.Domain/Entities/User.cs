using DatingBot.Domain.Enums;

namespace DatingBot.Domain.Entities;

public class User
{
    public Guid Id { get; set; }
    public long TelegramId { get; set; }
    public string? Username { get; set; }
    public string? FirstName { get; set; }
    public UserState State { get; set; } = UserState.None;
    public AppLanguage Language { get; set; } = AppLanguage.Russian;
    public int? LastBotMessageId { get; set; }
    public Guid? CurrentCandidateProfileId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public UserProfile? Profile { get; set; }
}
