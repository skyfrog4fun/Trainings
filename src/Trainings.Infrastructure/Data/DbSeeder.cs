using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Trainings.Application.Interfaces;
using Trainings.Domain.Entities;
using Trainings.Domain.Enums;

namespace Trainings.Infrastructure.Data;

public class DbSeeder
{
    private readonly ApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DbSeeder> _logger;

    public DbSeeder(ApplicationDbContext context, IPasswordHasher passwordHasher, IConfiguration configuration, ILogger<DbSeeder> logger)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        await EnsureDataDirectoryExistsAsync();
        await HandlePreExistingDatabaseAsync();
        await _context.Database.MigrateAsync();

        if (!_context.Users.Any())
        {
            var email = _configuration["Seed:Email"] ?? "superadmin@trainings.app";
            var password = _configuration["Seed:Password"] ?? "Admin123!";

            var superAdmin = new User
            {
                FirstName = "Super",
                LastName = "Admin",
                Email = email,
                PasswordHash = _passwordHasher.Hash(password),
                Role = UserRole.SuperAdmin,
                Gender = Gender.Other,
                IsActive = true,
                EmailConfirmedAt = DateTime.UtcNow,
                CreationDate = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };
            _context.Users.Add(superAdmin);
            await _context.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Ensures the data directory for the SQLite database file exists.
    /// When running in Docker with a volume mount, the directory may not be writable
    /// if it wasn't created beforehand on the host.
    /// </summary>
    private Task EnsureDataDirectoryExistsAsync()
    {
        var connectionString = _context.Database.GetConnectionString();
        if (string.IsNullOrEmpty(connectionString))
        {
            return Task.CompletedTask;
        }

        const string dataSourcePrefix = "Data Source=";
        var idx = connectionString.IndexOf(dataSourcePrefix, StringComparison.OrdinalIgnoreCase);
        if (idx < 0)
        {
            return Task.CompletedTask;
        }

        var dbPath = connectionString[(idx + dataSourcePrefix.Length)..].Trim();
        var directory = Path.GetDirectoryName(dbPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            _logger.LogInformation("Creating data directory: {Directory}", directory);
            Directory.CreateDirectory(directory);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Handles databases that were created by EnsureCreatedAsync (without migration history).
    /// If the database has tables but no __EFMigrationsHistory table, it marks the initial
    /// migration as applied so that MigrateAsync does not try to recreate existing tables.
    /// </summary>
    private async Task HandlePreExistingDatabaseAsync()
    {
        var databaseCreator = _context.Database.GetService<IRelationalDatabaseCreator>();
        if (!await databaseCreator.ExistsAsync())
        {
            return;
        }

        // Check directly via SQL whether application tables already exist
        // but migration history is missing — indicating the database was created
        // by EnsureCreatedAsync rather than by migrations.
        var connection = _context.Database.GetDbConnection();
        await connection.OpenAsync();
        try
        {
            // Check if the Users table exists (proxy for "schema is already present")
            using var checkTablesCmd = connection.CreateCommand();
            checkTablesCmd.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='Users'";
            var tablesExist = await checkTablesCmd.ExecuteScalarAsync() is long tableCount && tableCount > 0;
            if (!tablesExist)
            {
                return;
            }

            // Check if the __EFMigrationsHistory table exists and has rows
            using var checkHistoryCmd = connection.CreateCommand();
            checkHistoryCmd.CommandText =
                "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='__EFMigrationsHistory'";
            var historyTableExists = await checkHistoryCmd.ExecuteScalarAsync() is long historyCount && historyCount > 0;

            if (historyTableExists)
            {
                using var checkRowsCmd = connection.CreateCommand();
                checkRowsCmd.CommandText = "SELECT COUNT(*) FROM \"__EFMigrationsHistory\"";
                var rowCount = await checkRowsCmd.ExecuteScalarAsync() is long rows ? rows : 0;
                if (rowCount > 0)
                {
                    return;
                }
            }

            _logger.LogWarning(
                "Pre-existing database detected without migration history. " +
                "Marking InitialSchema migration as applied.");

            // Create the history table and insert the initial migration so MigrateAsync skips it.
            using var createCmd = connection.CreateCommand();
            createCmd.CommandText =
                """
                CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
                    "MigrationId" TEXT NOT NULL CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY,
                    "ProductVersion" TEXT NOT NULL
                );
                INSERT OR IGNORE INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
                VALUES ('20260430181938_InitialSchema', '10.0.3');
                """;
            await createCmd.ExecuteNonQueryAsync();
        }
        finally
        {
            await connection.CloseAsync();
        }
    }
}
