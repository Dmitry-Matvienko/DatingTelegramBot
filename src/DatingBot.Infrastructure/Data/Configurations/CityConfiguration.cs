using DatingBot.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DatingBot.Infrastructure.Data.Configurations;

public class CityConfiguration : IEntityTypeConfiguration<City>
{
    public void Configure(EntityTypeBuilder<City> builder)
    {
        builder.ToTable("Cities");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(c => c.Region)
            .HasMaxLength(150)
            .IsRequired(false);

        builder.Property(c => c.Country)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(c => c.Latitude)
            .IsRequired();

        builder.Property(c => c.Longitude)
            .IsRequired();

        builder.HasIndex(c => c.Name)
            .HasDatabaseName("IX_Cities_Name");

        builder.HasIndex(c => new { c.Latitude, c.Longitude })
            .HasDatabaseName("IX_Cities_Coordinates");
    }
}
