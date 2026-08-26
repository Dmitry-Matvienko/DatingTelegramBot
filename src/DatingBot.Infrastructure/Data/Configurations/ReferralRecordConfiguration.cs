using DatingBot.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DatingBot.Infrastructure.Data.Configurations;

public class ReferralRecordConfiguration : IEntityTypeConfiguration<ReferralRecord>
{
    public void Configure(EntityTypeBuilder<ReferralRecord> builder)
    {
        builder.ToTable("ReferralRecords");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.CreatedAt)
            .IsRequired();

        builder.HasIndex(r => r.ReferredUserId)
            .IsUnique()
            .HasDatabaseName("IX_ReferralRecords_ReferredUserId");

        builder.HasIndex(r => r.ReferrerUserId)
            .HasDatabaseName("IX_ReferralRecords_ReferrerUserId");

        builder.HasOne(r => r.ReferralLink)
            .WithMany()
            .HasForeignKey(r => r.ReferralLinkId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.ReferrerUser)
            .WithMany()
            .HasForeignKey(r => r.ReferrerUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.ReferredUser)
            .WithMany()
            .HasForeignKey(r => r.ReferredUserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
