using DatingBot.Domain.Entities;

namespace DatingBot.Application.Interfaces;

public interface IProfileReportRepository
{
    Task<ProfileReport?> GetByIdWithUsersAsync(Guid reportId, CancellationToken cancellationToken = default);
    Task<HashSet<Guid>> GetReportedUserIdsAsync(Guid reporterId, CancellationToken cancellationToken = default);
    Task<int> GetPendingReportsCountAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProfileReport>> GetPendingReportsAsync(int skip = 0, int take = 10, CancellationToken cancellationToken = default);
    Task AddAsync(ProfileReport report, CancellationToken cancellationToken = default);
    void Update(ProfileReport report);
}
