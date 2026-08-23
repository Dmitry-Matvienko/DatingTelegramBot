using DatingBot.Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace DatingBot.Infrastructure.Services;

public class AdminSettings(IConfiguration configuration) : IAdminSettings
{
    public IReadOnlyList<long> AdminIds =>
        configuration.GetSection("BotConfiguration:AdminIds")
            .GetChildren()
            .Select(c => long.TryParse(c.Value, out var id) ? id : 0)
            .Where(id => id != 0)
            .ToList();
}
