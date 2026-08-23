namespace DatingBot.Domain.Entities;

public class UserProfileInterest
{
    public Guid UserProfileId { get; set; }
    public UserProfile UserProfile { get; set; } = null!;

    public int InterestId { get; set; }
    public Interest Interest { get; set; } = null!;

    public DateTime AddedAt { get; set; } = DateTime.UtcNow;
}
