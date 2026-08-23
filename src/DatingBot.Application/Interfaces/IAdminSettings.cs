namespace DatingBot.Application.Interfaces;

public interface IAdminSettings
{
    IReadOnlyList<long> AdminIds { get; }
}
