using DatingBot.Domain.Enums;

namespace DatingBot.Domain.Entities;

public class Interest
{
    public int Id { get; set; }
    public InterestType Code { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;

    public ICollection<UserProfileInterest> UserProfiles { get; set; } = [];
}
