using DatingBot.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DatingBot.Infrastructure.Data.Configurations;

public class ProfileRatingConfiguration : IEntityTypeConfiguration<ProfileRating>
{
    public void Configure(EntityTypeBuilder<ProfileRating> builder)
    {
        builder.ToTable("ProfileRatings");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Score)
            .IsRequired();

        builder.Property(r => r.CreatedAt)
            .IsRequired();

        builder.HasOne(r => r.FromUser)
            .WithMany()
            .HasForeignKey(r => r.FromUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.ToUser)
            .WithMany()
            .HasForeignKey(r => r.ToUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Уникальный составной индекс: один пользователь может оценить другого пользователя только один раз
        builder.HasIndex(r => new { r.FromUserId, r.ToUserId })
            .IsUnique()
            .HasDatabaseName("IX_ProfileRatings_FromUser_ToUser");

        // Составной индекс для выборки входящих высоких оценок пользователя
        builder.HasIndex(r => new { r.ToUserId, r.Score, r.CreatedAt })
            .HasDatabaseName("IX_ProfileRatings_ToUser_Score_CreatedAt");
    }
}
