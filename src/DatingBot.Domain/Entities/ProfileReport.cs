using DatingBot.Domain.Enums;

namespace DatingBot.Domain.Entities;

public class ProfileReport
{
    public Guid Id { get; set; }

    public Guid ReporterId { get; set; }
    public User Reporter { get; set; } = null!;

    public Guid ReportedUserId { get; set; }
    public User ReportedUser { get; set; } = null!;

    public ReportReason Reason { get; set; }
    public string? Details { get; set; }
    public bool IsResolved { get; set; } = false;
    public DateTime? ResolvedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
