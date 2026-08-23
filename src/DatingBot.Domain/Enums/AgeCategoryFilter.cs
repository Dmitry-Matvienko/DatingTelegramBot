namespace DatingBot.Domain.Enums;

[Flags]
public enum AgeCategoryFilter
{
    None = 0,
    Under18 = 1 << 0,   // До 18 лет (10-17)
    Age18To25 = 1 << 1, // 18–25
    Age25To30 = 1 << 2, // 25–30
    Age30To40 = 1 << 3, // 30–40
    Age40Plus = 1 << 4  // Больше 40
}
