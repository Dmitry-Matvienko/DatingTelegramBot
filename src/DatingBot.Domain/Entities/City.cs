namespace DatingBot.Domain.Entities;

public class City
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Region { get; set; }
    public string Country { get; set; } = "Россия";
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}
