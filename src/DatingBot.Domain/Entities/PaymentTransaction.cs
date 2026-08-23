using DatingBot.Domain.Enums;

namespace DatingBot.Domain.Entities;

public class PaymentTransaction
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public long TelegramId { get; set; }
    public int Amount { get; set; }
    public string Currency { get; set; } = "XTR";
    public PaymentType Type { get; set; }
    public string Payload { get; set; } = string.Empty;
    public string? TelegramPaymentChargeId { get; set; }
    public string? ProviderPaymentChargeId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
