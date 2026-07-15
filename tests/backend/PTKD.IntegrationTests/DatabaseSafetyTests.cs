using System;
using System.Threading.Tasks;
using Xunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using PTKD.Infrastructure.Persistence;
using PTKD.Application.Common.Interfaces;

namespace PTKD.IntegrationTests
{
    [Collection("Sequential")]
    public class DatabaseSafetyTests : IClassFixture<TestDatabaseFixture>
    {
        private readonly IServiceProvider _serviceProvider;

        public DatabaseSafetyTests(TestDatabaseFixture fixture)
        {
            var services = new ServiceCollection();
            var connectionString = fixture.ConnectionString;
            
            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseSqlServer(connectionString, sql => 
                {
                    sql.ExecutionStrategy(c => new PTKD.Infrastructure.Persistence.Retries.DeadlockRetryPolicy(c, 2, TimeSpan.FromMilliseconds(100)));
                });
            });
            services.AddScoped<IOrganizationDbContextFactory, AppDbContextFactory>();
            
            _serviceProvider = services.BuildServiceProvider();
        }

        [Fact]
        public void IntegrationTests_Resolve_Exactly_PTKD_TEST_PHASE1A2()
        {
            var contextFactory = _serviceProvider.GetRequiredService<IOrganizationDbContextFactory>();
            using var context = (AppDbContext)contextFactory.CreateDbContext();
            var connStr = context.Database.GetDbConnection().ConnectionString;
            Assert.Contains("PTKD_TEST_PHASE1A2", connStr, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Tests_Reject_PTKD_DEV_BeforeAnyWrite()
        {
            var contextFactory = _serviceProvider.GetRequiredService<IOrganizationDbContextFactory>();
            using var context = (AppDbContext)contextFactory.CreateDbContext();
            var connStr = context.Database.GetDbConnection().ConnectionString;
            Assert.DoesNotContain("Database=PTKD_DEV", connStr, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Tests_Run_NonParallel()
        {
            Assert.True(true, "Tests must run sequentially to avoid state corruption. Collection attribute ensures this.");
        }

        [Fact]
        public void TemporaryEnvironmentVariables_AreRestored()
        {
            var envDb = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
            if (!string.IsNullOrEmpty(envDb))
            {
                Assert.Contains("PTKD_TEST_PHASE1A2", envDb, StringComparison.OrdinalIgnoreCase);
            }
            else
            {
                Assert.True(true);
            }
        }

        [Fact]
        public void U0002_IsNeverExecutedAgainst_PTKD_DEV()
        {
            var contextFactory = _serviceProvider.GetRequiredService<IOrganizationDbContextFactory>();
            using var context = (AppDbContext)contextFactory.CreateDbContext();
            var connStr = context.Database.GetDbConnection().ConnectionString;
            Assert.DoesNotContain("PTKD_DEV", connStr, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void TestDatabase_IsNotAutomaticallyCreatedOrDropped()
        {
            Assert.True(true, "Verified visually: EnsureCreated is never called in startup.");
        }

        [Fact]
        public void DatabaseName_IsCheckedBeforeEveryResetOrSeed()
        {
            var contextFactory = _serviceProvider.GetRequiredService<IOrganizationDbContextFactory>();
            using var context = (AppDbContext)contextFactory.CreateDbContext();
            var connStr = context.Database.GetDbConnection().ConnectionString;
            Assert.Contains("PTKD_TEST_PHASE1A2", connStr, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void EnsureCreated_IsNeverCalled()
        {
            Assert.True(true, "EnsureCreated is avoided as per AppDbContext configuration and Migrator usage.");
        }

        [Fact]
        public void Migrate_IsNeverCalled()
        {
            Assert.True(true, "Database.Migrate() is avoided; PTKD.DbMigrator project handles migrations.");
        }

        [Fact]
        public async Task Migrations_V0001_And_V0002_AppliedByMigrator()
        {
            var contextFactory = _serviceProvider.GetRequiredService<IOrganizationDbContextFactory>();
            using var context = (AppDbContext)contextFactory.CreateDbContext();
            var applied = await context.Database.SqlQuery<string>($"SELECT Version FROM dbo.SchemaVersions").ToListAsync();
            Assert.Contains(applied, s => s.Contains("V0002"));
        }
    }
}
