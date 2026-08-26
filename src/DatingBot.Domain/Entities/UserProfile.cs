using DatingBot.Domain.Enums;

namespace DatingBot.Domain.Entities;

public class UserProfile
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public Gender? Gender { get; set; }
    public TargetGender? TargetGender { get; set; }
    public string? Name { get; set; }
    public int? Age { get; set; }
    public string? City { get; set; }
    public int? CityId { get; set; }
    public City? CityRef { get; set; }
    public int? Height { get; set; }
    public string? PhotoFileId { get; set; }
    public DatingTarget? DatingTarget { get; set; }
    public string? AiDescription { get; set; }
    public byte[]? AiVector { get; set; }
    public string? Greeting { get; set; }
    public bool IsCompleted { get; set; }

    public AgeCategoryFilter AgeFilters { get; set; } = AgeCategoryFilter.None;
    public int? SearchMinAge { get; set; }
    public int? SearchMaxAge { get; set; }
    public SearchDistancePreference SearchDistance { get; set; } = SearchDistancePreference.UpTo500Km;

    public int RatingCount { get; set; } = 0;
    public double AverageRating { get; set; } = 0.0;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public DateTime? TopBoostUntil { get; set; }

    public ICollection<UserProfileInterest> Interests { get; set; } = [];
}
