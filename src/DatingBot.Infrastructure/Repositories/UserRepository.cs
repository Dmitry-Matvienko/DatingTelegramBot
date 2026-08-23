using DatingBot.Application.Interfaces;
using DatingBot.Domain.Entities;
using DatingBot.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DatingBot.Infrastructure.Repositories;

public class UserRepository(AppDbContext dbContext) : IUserRepository
{
    public async Task<User?> GetByTelegramIdAsync(long telegramId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Users
            .FirstOrDefaultAsync(u => u.TelegramId == telegramId, cancellationToken);
    }

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    public async Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        await dbContext.Users.AddAsync(user, cancellationToken);
    }

    public void Update(User user)
    {
        dbContext.Users.Update(user);
    }
}
