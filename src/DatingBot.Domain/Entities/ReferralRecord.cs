namespace DatingBot.Domain.Entities;

public class ReferralRecord
{
    public Guid Id { get; set; }
    public Guid ReferralLinkId { get; set; }
    public ReferralLink ReferralLink { get; set; } = null!;
    public Guid ReferrerUserId { get; set; }
    public User ReferrerUser { get; set; } = null!;
    public Guid ReferredUserId { get; set; }
    public User ReferredUser { get; set; } = null!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
