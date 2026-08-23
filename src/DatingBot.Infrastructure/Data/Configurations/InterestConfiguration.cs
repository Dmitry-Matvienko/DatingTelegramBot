using DatingBot.Domain.Entities;
using DatingBot.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DatingBot.Infrastructure.Data.Configurations;

public class InterestConfiguration : IEntityTypeConfiguration<Interest>
{
    public void Configure(EntityTypeBuilder<Interest> builder)
    {
        builder.ToTable("Interests");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.Code)
            .IsRequired();

        builder.HasIndex(i => i.Code)
            .IsUnique()
            .HasDatabaseName("IX_Interests_Code");

        builder.Property(i => i.Title)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(i => i.Icon)
            .IsRequired()
            .HasMaxLength(20);

        // Seeding 12 base interests
        builder.HasData(
            new Interest { Id = 1, Code = InterestType.Music, Title = "Музыка", Icon = "🎵" },
            new Interest { Id = 2, Code = InterestType.Gaming, Title = "Видеоигры", Icon = "🎮" },
            new Interest { Id = 3, Code = InterestType.Movies, Title = "Фильмы", Icon = "🎬" },
            new Interest { Id = 4, Code = InterestType.Anime, Title = "Аниме", Icon = "⛩️" },
            new Interest { Id = 5, Code = InterestType.Sports, Title = "Спорт", Icon = "⚽" },
            new Interest { Id = 6, Code = InterestType.Books, Title = "Книги", Icon = "📚" },
            new Interest { Id = 7, Code = InterestType.Travel, Title = "Путешествия", Icon = "✈️" },
            new Interest { Id = 8, Code = InterestType.Art, Title = "Искусство", Icon = "🎨" },
            new Interest { Id = 9, Code = InterestType.Cooking, Title = "Кулинария", Icon = "🍳" },
            new Interest { Id = 10, Code = InterestType.Tech, Title = "Технологии", Icon = "💻" },
            new Interest { Id = 11, Code = InterestType.BoardGames, Title = "Настолки", Icon = "🎲" },
            new Interest { Id = 12, Code = InterestType.Outdoor, Title = "Активный отдых", Icon = "🏕️" }
        );
    }
}
