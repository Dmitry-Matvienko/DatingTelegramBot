using DatingBot.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DatingBot.Infrastructure.Data.Configurations;

public class ReferralLinkConfiguration : IEntityTypeConfiguration<ReferralLink>
{
    public void Configure(EntityTypeBuilder<ReferralLink> builder)
    {
        builder.ToTable("ReferralLinks");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Code)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(r => r.InvitedCount)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(r => r.CreatedAt)
            .IsRequired();

        builder.HasIndex(r => r.UserId)
            .IsUnique()
            .HasDatabaseName("IX_ReferralLinks_UserId");

        builder.HasIndex(r => r.Code)
            .IsUnique()
            .HasDatabaseName("IX_ReferralLinks_Code");

        builder.HasOne(r => r.User)
            .WithOne(u => u.ReferralLink)
            .HasForeignKey<ReferralLink>(r => r.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
