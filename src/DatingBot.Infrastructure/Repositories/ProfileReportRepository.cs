using DatingBot.Application.Interfaces;
using DatingBot.Domain.Entities;
using DatingBot.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DatingBot.Infrastructure.Repositories;

public class ProfileReportRepository(AppDbContext dbContext) : IProfileReportRepository
{
    public async Task<ProfileReport?> GetByIdWithUsersAsync(Guid reportId, CancellationToken cancellationToken = default)
    {
        return await dbContext.ProfileReports
            .Include(r => r.Reporter)
            .Include(r => r.ReportedUser)
                .ThenInclude(u => u.Profile)
            .FirstOrDefaultAsync(r => r.Id == reportId, cancellationToken);
    }

    public async Task<HashSet<Guid>> GetReportedUserIdsAsync(Guid reporterId, CancellationToken cancellationToken = default)
    {
        var ids = await dbContext.ProfileReports
            .AsNoTracking()
            .Where(r => r.ReporterId == reporterId)
            .Select(r => r.ReportedUserId)
            .ToListAsync(cancellationToken);

        return ids.ToHashSet();
    }

    public async Task<int> GetPendingReportsCountAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.ProfileReports
            .AsNoTracking()
            .CountAsync(r => !r.IsResolved, cancellationToken);
    }

    public async Task<IReadOnlyList<ProfileReport>> GetPendingReportsAsync(int skip = 0, int take = 10, CancellationToken cancellationToken = default)
    {
        return await dbContext.ProfileReports
            .AsNoTracking()
            .Include(r => r.Reporter)
            .Include(r => r.ReportedUser)
                .ThenInclude(u => u.Profile)
                    .ThenInclude(p => p!.Interests)
            .Where(r => !r.IsResolved)
            .OrderByDescending(r => r.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(ProfileReport report, CancellationToken cancellationToken = default)
    {
        await dbContext.ProfileReports.AddAsync(report, cancellationToken);
    }

    public void Update(ProfileReport report)
    {
        dbContext.ProfileReports.Update(report);
    }
}
