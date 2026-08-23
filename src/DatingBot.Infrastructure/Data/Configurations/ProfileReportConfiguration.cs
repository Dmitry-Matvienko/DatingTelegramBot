using DatingBot.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DatingBot.Infrastructure.Data.Configurations;

public class ProfileReportConfiguration : IEntityTypeConfiguration<ProfileReport>
{
    public void Configure(EntityTypeBuilder<ProfileReport> builder)
    {
        builder.ToTable("ProfileReports");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Reason)
            .IsRequired();

        builder.Property(r => r.Details)
            .HasMaxLength(1000)
            .IsRequired(false);

        builder.Property(r => r.IsResolved)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(r => r.ResolvedAt)
            .IsRequired(false);

        builder.Property(r => r.CreatedAt)
            .IsRequired();

        builder.HasOne(r => r.Reporter)
            .WithMany()
            .HasForeignKey(r => r.ReporterId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.ReportedUser)
            .WithMany()
            .HasForeignKey(r => r.ReportedUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(r => new { r.ReporterId, r.ReportedUserId })
            .HasDatabaseName("IX_ProfileReports_Reporter_Reported");

        builder.HasIndex(r => new { r.IsResolved, r.CreatedAt })
            .HasDatabaseName("IX_ProfileReports_IsResolved_CreatedAt");
    }
}
