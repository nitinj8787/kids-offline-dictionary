using Microsoft.EntityFrameworkCore;
using KidsDictionaryApi.Models;

namespace KidsDictionaryApi.Data
{
    public class ApiDbContext : DbContext
    {
        public ApiDbContext(DbContextOptions<ApiDbContext> options) : base(options) { }

        public DbSet<UserAccount> UserAccounts => Set<UserAccount>();
        public DbSet<CentralProfile> CentralProfiles => Set<CentralProfile>();
        public DbSet<AppUsage> AppUsages => Set<AppUsage>();
        public DbSet<OtpRecord> OtpRecords => Set<OtpRecord>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // UserAccount: email must be unique
            modelBuilder.Entity<UserAccount>()
                .HasIndex(u => u.Email)
                .IsUnique();

            // CentralProfile belongs to UserAccount
            modelBuilder.Entity<CentralProfile>()
                .HasOne(p => p.UserAccount)
                .WithMany(u => u.Profiles)
                .HasForeignKey(p => p.UserAccountId)
                .OnDelete(DeleteBehavior.Cascade);

            // AppUsage belongs to UserAccount
            modelBuilder.Entity<AppUsage>()
                .HasOne(a => a.UserAccount)
                .WithMany(u => u.Usages)
                .HasForeignKey(a => a.UserAccountId)
                .OnDelete(DeleteBehavior.Cascade);

            // OtpRecord: index on email for fast lookup
            modelBuilder.Entity<OtpRecord>()
                .HasIndex(o => o.Email);
        }
    }
}
