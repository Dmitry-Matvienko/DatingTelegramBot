using DatingBot.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DatingBot.Infrastructure.Data.Configurations;

public class UserProfileInterestConfiguration : IEntityTypeConfiguration<UserProfileInterest>
{
    public void Configure(EntityTypeBuilder<UserProfileInterest> builder)
    {
        builder.ToTable("UserProfileInterests");

        builder.HasKey(upi => new { upi.UserProfileId, upi.InterestId });

        builder.HasOne(upi => upi.UserProfile)
            .WithMany(p => p.Interests)
            .HasForeignKey(upi => upi.UserProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(upi => upi.Interest)
            .WithMany(i => i.UserProfiles)
            .HasForeignKey(upi => upi.InterestId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(upi => upi.AddedAt)
            .IsRequired();
    }
}
