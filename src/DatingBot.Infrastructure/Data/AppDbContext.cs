using DatingBot.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DatingBot.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
    public DbSet<Interest> Interests => Set<Interest>();
    public DbSet<UserProfileInterest> UserProfileInterests => Set<UserProfileInterest>();
    public DbSet<ProfileRating> ProfileRatings => Set<ProfileRating>();
    public DbSet<ProfileReport> ProfileReports => Set<ProfileReport>();
    public DbSet<City> Cities => Set<City>();
    public DbSet<PaymentTransaction> PaymentTransactions => Set<PaymentTransaction>();
    public DbSet<ReferralLink> ReferralLinks => Set<ReferralLink>();
    public DbSet<ReferralRecord> ReferralRecords => Set<ReferralRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
