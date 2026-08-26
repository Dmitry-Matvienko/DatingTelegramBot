namespace DatingBot.Application.Interfaces;

public interface IBotInfoProvider
{
    Task<string> GetBotUsernameAsync(CancellationToken cancellationToken = default);
}
