using DatingBot.Domain.Entities;
using DatingBot.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DatingBot.Infrastructure.Data.Configurations;

public class UserProfileConfiguration : IEntityTypeConfiguration<UserProfile>
{
    public void Configure(EntityTypeBuilder<UserProfile> builder)
    {
        builder.ToTable("UserProfiles");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name)
            .HasMaxLength(100);

        builder.Property(p => p.City)
            .HasMaxLength(150);

        builder.Property(p => p.PhotoFileId)
            .HasMaxLength(250);

        builder.Property(p => p.AiDescription)
            .HasMaxLength(2500);

        builder.Property(p => p.AiVector)
            .HasMaxLength(1536)
            .IsRequired(false);

        builder.Property(p => p.Greeting)
            .HasMaxLength(500)
            .IsRequired(false);

        builder.HasOne(p => p.CityRef)
            .WithMany()
            .HasForeignKey(p => p.CityId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Property(p => p.IsCompleted)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(p => p.AgeFilters)
            .IsRequired()
            .HasDefaultValue(AgeCategoryFilter.None);

        builder.Property(p => p.SearchMinAge)
            .IsRequired(false);

        builder.Property(p => p.SearchMaxAge)
            .IsRequired(false);

        builder.Property(p => p.SearchDistance)
            .IsRequired()
            .HasDefaultValue(SearchDistancePreference.UpTo500Km);

        builder.Property(p => p.RatingCount)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(p => p.AverageRating)
            .IsRequired()
            .HasDefaultValue(0.0);

        builder.Property(p => p.TopBoostUntil)
            .IsRequired(false);

        builder.HasIndex(p => new { p.IsCompleted, p.Gender, p.TargetGender, p.DatingTarget })
            .HasDatabaseName("IX_UserProfiles_Matchmaking_Filter");

        builder.HasIndex(p => p.City)
            .HasDatabaseName("IX_UserProfiles_City");
    }
}
