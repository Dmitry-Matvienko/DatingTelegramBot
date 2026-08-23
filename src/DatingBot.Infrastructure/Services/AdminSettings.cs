using DatingBot.Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace DatingBot.Infrastructure.Services;

public class AdminSettings(IConfiguration configuration) : IAdminSettings
{
    public IReadOnlyList<long> AdminIds
    {
        get
        {
            var ids = new HashSet<long>();

            // 1. Попытка прочитать массив/секцию BotConfiguration:AdminIds (JSON array или BotConfiguration__AdminIds__0)
            var section = configuration.GetSection("BotConfiguration:AdminIds");
            foreach (var child in section.GetChildren())
            {
                if (long.TryParse(child.Value, out var id) && id > 0)
                {
                    ids.Add(id);
                }
            }

            // 2. Попытка прочитать как единую строку (например, "123456789,987654321" или "123456789")
            var singleValue = configuration["BotConfiguration:AdminIds"] ?? configuration["ADMIN_IDS"];
            if (!string.IsNullOrWhiteSpace(singleValue))
            {
                var parts = singleValue.Split([',', ';', ' ', '\n', '\r'], StringSplitOptions.RemoveEmptyEntries);
                foreach (var part in parts)
                {
                    if (long.TryParse(part.Trim(), out var parsedId) && parsedId > 0)
                    {
                        ids.Add(parsedId);
                    }
                }
            }

            return ids.ToList();
        }
    }
}
