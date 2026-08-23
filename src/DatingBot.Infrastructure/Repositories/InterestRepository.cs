using DatingBot.Application.Interfaces;
using DatingBot.Domain.Entities;
using DatingBot.Domain.Enums;
using DatingBot.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DatingBot.Infrastructure.Repositories;

public class InterestRepository(AppDbContext dbContext) : IInterestRepository
{
    public async Task<IReadOnlyList<Interest>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Interests
            .AsNoTracking()
            .OrderBy(i => i.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<Interest?> GetByCodeAsync(InterestType code, CancellationToken cancellationToken = default)
    {
        return await dbContext.Interests
            .FirstOrDefaultAsync(i => i.Code == code, cancellationToken);
    }
}
