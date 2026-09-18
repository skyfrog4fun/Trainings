using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Trainings.Application.Constants;
using Trainings.Application.Interfaces;
using Trainings.Domain.Entities;
using Trainings.Domain.Enums;

namespace Trainings.Infrastructure.Data;

public partial class DbSeeder(
    ApplicationDbContext context,
    IPasswordHasher passwordHasher,
    IConfiguration configuration,
    ILogger<DbSeeder> logger,
    ITranslationService translationService)
{
    private readonly ApplicationDbContext _context = context;
    private readonly IPasswordHasher _passwordHasher = passwordHasher;
    private readonly IConfiguration _configuration = configuration;
    private readonly ILogger<DbSeeder> _logger = logger;
    private readonly ITranslationService _translationService = translationService;

    private static readonly (string Key, string EnglishText, string GermanText, string ColorToken, int DisplayOrder)[] FixedTags =
    [
        (TrainingBlockCatalog.TagKeys.WarmUp, "Warm-Up", "Warm-Up", TrainingBlockCatalog.ColorTokens.Community, 1),
        (TrainingBlockCatalog.TagKeys.Fitness, "Fitness", "Fitness", TrainingBlockCatalog.ColorTokens.Accent, 2),
        (TrainingBlockCatalog.TagKeys.Technique, "Technique", "Technik", TrainingBlockCatalog.ColorTokens.Accent40, 3),
        (TrainingBlockCatalog.TagKeys.Game, "Game", "Spiel", TrainingBlockCatalog.ColorTokens.Innovation, 4),
        (TrainingBlockCatalog.TagKeys.CoolDown, "Cool-down", "Cool-down", TrainingBlockCatalog.ColorTokens.AppreciationDark, 5),
        (TrainingBlockCatalog.TagKeys.Other, "Other", "Sonstiges", TrainingBlockCatalog.ColorTokens.Tradition60, 6)
    ];

    private static readonly (string EnglishText, string GermanText, bool IsSystemFallback)[] SeedGames =
    [
        ("Soccer", "Fussball", false),
        ("Floorball", "Unihockey", false),
        ("Dodgeball", "Völkerball", false),
        ("Basketball", "Basketball", false),
        ("Volleyball", "Volleyball", false),
        ("Handball", "Handball", false),
        ("Tag", "Fangen", false),
        ("Relay", "Stafette", false),
        ("Other", "Sonstiges", true)
    ];

    public async Task SeedAsync()
    {
        await EnsureDataDirectoryExistsAsync();
        await HandlePreExistingDatabaseAsync();
        await _context.Database.MigrateAsync();
        await SeedLocationsAsync();
        await SeedTagsAsync();
        await SeedGamesAsync();

        if (!await _context.Users.AnyAsync())
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

    private async Task SeedTagsAsync()
    {
        foreach (var seed in FixedTags)
        {
            var tag = await _context.Tags.FirstOrDefaultAsync(t => t.Key == seed.Key);
            if (tag == null)
            {
                tag = new Tag
                {
                    Key = seed.Key,
                    ColorToken = seed.ColorToken,
                    DisplayOrder = seed.DisplayOrder,
                    IsActive = true
                };
                _context.Tags.Add(tag);
                await _context.SaveChangesAsync();
            }
            else
            {
                tag.ColorToken = seed.ColorToken;
                tag.DisplayOrder = seed.DisplayOrder;
                tag.IsActive = true;
                await _context.SaveChangesAsync();
            }

            await _translationService.UpsertAsync(TranslationEntityType.Tag, tag.Id, seed.EnglishText, seed.GermanText);
        }
    }

    private async Task SeedGamesAsync()
    {
        foreach (var seed in SeedGames)
        {
            var translationIds = await _context.Translations
                .Where(t => t.EntityType == TranslationEntityType.Game && (t.Text == seed.EnglishText || t.Text == seed.GermanText))
                .Select(t => t.EntityId)
                .Distinct()
                .ToListAsync();

            Game? game = null;
            if (translationIds.Count > 0)
            {
                game = await _context.Games.FirstOrDefaultAsync(g => translationIds.Contains(g.Id));
            }

            if (game == null)
            {
                game = new Game
                {
                    IsActive = true,
                    IsApproved = true,
                    IsSystemFallback = seed.IsSystemFallback,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Games.Add(game);
                await _context.SaveChangesAsync();
            }
            else
            {
                game.IsSystemFallback = seed.IsSystemFallback;
                game.IsActive = true;
                game.IsApproved = true;
                await _context.SaveChangesAsync();
            }

            await _translationService.UpsertAsync(TranslationEntityType.Game, game.Id, seed.EnglishText, seed.GermanText);
        }
    }

    private async Task SeedLocationsAsync()
    {
        if (await _context.Locations.AnyAsync())
        {
            return;
        }

        _context.Locations.AddRange(
            new Location
            {
                Name = "Outside",
                CityName = string.Empty,
                IsSystemWide = true,
                IsActive = true
            },
            new Location
            {
                Name = "Special",
                CityName = string.Empty,
                IsSystemWide = true,
                IsActive = true
            });

        await _context.SaveChangesAsync();
    }

    private Task EnsureDataDirectoryExistsAsync()
    {
        var connectionString = _context.Database.GetConnectionString();
        if (string.IsNullOrEmpty(connectionString))
        {
            return Task.CompletedTask;
        }

        var builder = new Microsoft.Data.Sqlite.SqliteConnectionStringBuilder(connectionString);
        var dbPath = builder.DataSource;
        if (string.IsNullOrEmpty(dbPath))
        {
            return Task.CompletedTask;
        }

        var directory = Path.GetDirectoryName(dbPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            LogCreatingDataDirectory(_logger, directory);
            Directory.CreateDirectory(directory);
        }

        return Task.CompletedTask;
    }

    private async Task HandlePreExistingDatabaseAsync()
    {
        var databaseCreator = _context.Database.GetService<IRelationalDatabaseCreator>();
        if (!await databaseCreator.ExistsAsync())
        {
            return;
        }

        var connection = _context.Database.GetDbConnection();
        await connection.OpenAsync();
        try
        {
            using var checkTablesCmd = connection.CreateCommand();
            checkTablesCmd.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='Users'";
            var tablesExist = await checkTablesCmd.ExecuteScalarAsync() is long tableCount && tableCount > 0;
            if (!tablesExist)
            {
                return;
            }

            using var checkHistoryCmd = connection.CreateCommand();
            checkHistoryCmd.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='__EFMigrationsHistory'";
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

            LogPreExistingDatabaseDetected(_logger);
            await AddMissingColumnsAsync(connection);

            using var createCmd = connection.CreateCommand();
            createCmd.CommandText = """
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

    [LoggerMessage(Level = LogLevel.Information, Message = "Creating data directory: {Directory}")]
    private static partial void LogCreatingDataDirectory(ILogger logger, string directory);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Pre-existing database detected without migration history. Marking InitialSchema migration as applied.")]
    private static partial void LogPreExistingDatabaseDetected(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Added missing column {Column} to table {Table}.")]
    private static partial void LogAddedMissingColumn(ILogger logger, string column, string table);

    private async Task AddMissingColumnsAsync(System.Data.Common.DbConnection connection)
    {
        var columnsToAdd = new (string Table, string Column, string TypeAndDefault)[]
        {
            ("GroupMemberships", "Status", "INTEGER NOT NULL DEFAULT 0"),
            ("GroupMemberships", "RequestedAt", "TEXT NOT NULL DEFAULT '0001-01-01 00:00:00'"),
            ("GroupMemberships", "ApprovedAt", "TEXT"),
            ("GroupMemberships", "DeclinedAt", "TEXT"),
            ("NotificationLogs", "AttemptId", "TEXT NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000'")
        };

        foreach (var (table, column, typeAndDefault) in columnsToAdd)
        {
            if (await ColumnExistsAsync(connection, table, column))
            {
                continue;
            }

            using var alterCmd = connection.CreateCommand();
            alterCmd.CommandText = $"ALTER TABLE \"{table}\" ADD COLUMN \"{column}\" {typeAndDefault}";
            await alterCmd.ExecuteNonQueryAsync();
            LogAddedMissingColumn(_logger, column, table);
        }
    }

    private static async Task<bool> ColumnExistsAsync(System.Data.Common.DbConnection connection, string table, string column)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = $"SELECT COUNT(*) FROM pragma_table_info('{table}') WHERE name = '{column}'";
        var result = await cmd.ExecuteScalarAsync();
        return result is long count && count > 0;
    }
}
