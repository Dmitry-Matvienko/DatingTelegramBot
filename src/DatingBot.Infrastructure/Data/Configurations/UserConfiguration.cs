using DatingBot.Domain.Entities;
using DatingBot.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DatingBot.Infrastructure.Data.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.TelegramId)
            .IsRequired();

        builder.HasIndex(u => u.TelegramId)
            .IsUnique()
            .HasDatabaseName("IX_Users_TelegramId");

        builder.Property(u => u.Username)
            .HasMaxLength(100);

        builder.Property(u => u.FirstName)
            .HasMaxLength(200);

        builder.Property(u => u.State)
            .IsRequired();

        builder.Property(u => u.Language)
            .IsRequired()
            .HasDefaultValue(AppLanguage.Russian)
            .HasSentinel(AppLanguage.None);

        builder.Property(u => u.LastBotMessageId)
            .IsRequired(false);

        builder.Property(u => u.CurrentCandidateProfileId)
            .IsRequired(false);

        builder.Property(u => u.CreatedAt)
            .IsRequired();

        builder.Property(u => u.LastActiveAt)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(u => u.LastInactivityReminderSentAt)
            .IsRequired(false);

        builder.HasIndex(u => new { u.LastActiveAt, u.LastInactivityReminderSentAt })
            .HasDatabaseName("IX_Users_LastActive_LastReminder");

        builder.HasOne(u => u.Profile)
            .WithOne(p => p.User)
            .HasForeignKey<UserProfile>(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
