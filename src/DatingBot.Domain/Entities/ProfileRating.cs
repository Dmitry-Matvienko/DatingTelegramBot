namespace DatingBot.Domain.Entities;

public class ProfileRating
{
    public Guid Id { get; set; }

    public Guid FromUserId { get; set; }
    public User FromUser { get; set; } = null!;

    public Guid ToUserId { get; set; }
    public User ToUser { get; set; } = null!;

    public int Score { get; set; } // 1 - 10
    public bool IsViewed { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
