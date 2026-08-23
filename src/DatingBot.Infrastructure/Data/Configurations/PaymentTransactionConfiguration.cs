using DatingBot.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DatingBot.Infrastructure.Data.Configurations;

public class PaymentTransactionConfiguration : IEntityTypeConfiguration<PaymentTransaction>
{
    public void Configure(EntityTypeBuilder<PaymentTransaction> builder)
    {
        builder.ToTable("PaymentTransactions");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.TelegramId)
            .IsRequired();

        builder.Property(t => t.Amount)
            .IsRequired();

        builder.Property(t => t.Currency)
            .IsRequired()
            .HasMaxLength(10)
            .HasDefaultValue("XTR");

        builder.Property(t => t.Type)
            .IsRequired();

        builder.Property(t => t.Payload)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(t => t.TelegramPaymentChargeId)
            .HasMaxLength(255);

        builder.Property(t => t.ProviderPaymentChargeId)
            .HasMaxLength(255);

        builder.Property(t => t.CreatedAt)
            .IsRequired();

        builder.HasIndex(t => t.TelegramId)
            .HasDatabaseName("IX_PaymentTransactions_TelegramId");

        builder.HasIndex(t => t.CreatedAt)
            .HasDatabaseName("IX_PaymentTransactions_CreatedAt");

        builder.HasOne(t => t.User)
            .WithMany()
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
