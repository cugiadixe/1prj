using System;
using System.IO;
using System.Linq;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace PTKD.DbMigrator;

class Program
{
    static int Main(string[] args)
    {
        Console.WriteLine("PTKD DbMigrator started.");
        bool dryRun = args.Contains("--dry-run");

        var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddEnvironmentVariables()
            .AddUserSecrets<Program>(optional: true)
            .Build();

        string? connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? config.GetConnectionString("DefaultConnection");

        if (string.IsNullOrEmpty(connectionString))
        {
            Console.WriteLine("Error: Connection string not found.");
            return 1;
        }

        if (dryRun)
        {
            Console.WriteLine("--- DRY RUN MODE ---");
        }

        // Resolve migrations directory. Try several candidate paths to support
        // running from the repo root (dotnet run --project), from the project
        // directory, and from the output bin directory.
        string? migrationsPath = null;
        string[] candidates =
        [
            // 1. CWD/database/migrations — when run from the repo root
            Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "database", "migrations")),
            // 2. Up two levels from CWD (e.g. CWD = src/backend/PTKD.DbMigrator)
            Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "database", "migrations")),
            // 3. Relative to the output assembly (bin/Debug/net10.0 → repo root)
            Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "..", "..", "database", "migrations")),
        ];

        foreach (var candidate in candidates)
        {
            if (Directory.Exists(candidate))
            {
                migrationsPath = candidate;
                break;
            }
        }

        if (migrationsPath is null || !Directory.Exists(migrationsPath))
        {
            Console.WriteLine($"Error: Migrations directory not found. Tried: {string.Join(", ", candidates)}");
            return 1;
        }

        Console.WriteLine($"Using migrations directory: {migrationsPath}");
        var files = Directory.GetFiles(migrationsPath, "V*.sql").OrderBy(f => f).ToList();

        if (files.Count == 0)
        {
            Console.WriteLine("No migration files found.");
            return 0;
        }

        try
        {
            using var connection = new SqlConnection(connectionString);
            connection.Open();
            Console.WriteLine("Connected to database successfully.");

            var checkTableSql = "SELECT 1 FROM sys.tables WHERE name = 'SchemaVersions' AND schema_id = SCHEMA_ID('dbo')";
            bool tableExists = false;
            using (var cmd = new SqlCommand(checkTableSql, connection))
            {
                tableExists = cmd.ExecuteScalar() != null;
            }

            foreach (var file in files)
            {
                string fileName = Path.GetFileName(file);
                string version = fileName.Split("__")[0];

                if (tableExists)
                {
                    var checkSql = "SELECT 1 FROM dbo.SchemaVersions WHERE ScriptName = @Name";
                    using var cmdCheck = new SqlCommand(checkSql, connection);
                    cmdCheck.Parameters.AddWithValue("@Name", fileName);
                    if (cmdCheck.ExecuteScalar() != null)
                    {
                        Console.WriteLine($"Skipping {fileName} (already applied)");
                        continue;
                    }
                }

                Console.WriteLine($"Applying {fileName}...");
                if (!dryRun)
                {
                    using var transaction = connection.BeginTransaction();
                    try
                    {
                        string sql = File.ReadAllText(file);
                        var batches = sql.Split(["\r\nGO", "\nGO"], StringSplitOptions.RemoveEmptyEntries);

                        foreach (var batch in batches)
                        {
                            if (string.IsNullOrWhiteSpace(batch)) continue;
                            using var cmd = new SqlCommand(batch, connection, transaction);
                            cmd.ExecuteNonQuery();
                        }

                        if (!tableExists) tableExists = true;

                        var insertSql = "INSERT INTO dbo.SchemaVersions (Version, ScriptName, Status) VALUES (@Ver, @Name, 'APPLIED')";
                        using var cmdInsert = new SqlCommand(insertSql, connection, transaction);
                        cmdInsert.Parameters.AddWithValue("@Ver", version);
                        cmdInsert.Parameters.AddWithValue("@Name", fileName);
                        cmdInsert.ExecuteNonQuery();

                        transaction.Commit();
                        Console.WriteLine($"Applied {fileName} successfully.");
                    }
                    catch (Exception)
                    {
                        transaction.Rollback();
                        Console.WriteLine($"Failed applying {fileName}, transaction rolled back.");
                        throw;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during migration: {ex.Message}");
            return 1;
        }

        Console.WriteLine("PTKD DbMigrator finished.");
        return 0;
    }
}
