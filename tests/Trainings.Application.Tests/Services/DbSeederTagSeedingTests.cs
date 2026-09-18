using System.Reflection;

using FluentAssertions;

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using Moq;

using Trainings.Application.Constants;
using Trainings.Application.Interfaces;
using Trainings.Domain.Enums;
using Trainings.Infrastructure.Data;

namespace Trainings.Application.Tests.Services;

public class DbSeederTagSeedingTests
{
    [Fact]
    public async Task SeedTagAndGameCatalogAsync_SeedsExpectedFixedTags()
    {
        await using var scope = await CreateSeederScopeAsync();

        await InvokeSeederAsync(scope.Seeder, "SeedTagsAsync");

        var tags = await scope.Context.Tags.OrderBy(tag => tag.DisplayOrder).ToListAsync();
        var translations = await scope.Context.Translations.Where(t => t.EntityType == TranslationEntityType.Tag).ToListAsync();

        tags.Should().HaveCount(6);
        tags.Select(tag => tag.Key).Should().Equal(
            TrainingBlockCatalog.TagKeys.WarmUp,
            TrainingBlockCatalog.TagKeys.Fitness,
            TrainingBlockCatalog.TagKeys.Technique,
            TrainingBlockCatalog.TagKeys.Game,
            TrainingBlockCatalog.TagKeys.CoolDown,
            TrainingBlockCatalog.TagKeys.Other);
        tags.Select(tag => tag.ColorToken).Should().Equal(
            TrainingBlockCatalog.ColorTokens.Community,
            TrainingBlockCatalog.ColorTokens.Accent,
            TrainingBlockCatalog.ColorTokens.Accent40,
            TrainingBlockCatalog.ColorTokens.Innovation,
            TrainingBlockCatalog.ColorTokens.AppreciationDark,
            TrainingBlockCatalog.ColorTokens.Tradition60);
        translations.Should().HaveCount(12);
        translations.Should().Contain(t => t.Culture == "de" && t.Text == "Technik");
    }

    [Fact]
    public async Task SeedTagAndGameCatalogAsync_SeedsStarterGamesIncludingFallback()
    {
        await using var scope = await CreateSeederScopeAsync();

        await InvokeSeederAsync(scope.Seeder, "SeedGamesAsync");

        var games = await scope.Context.Games.OrderBy(game => game.Id).ToListAsync();
        var translations = await scope.Context.Translations.Where(t => t.EntityType == TranslationEntityType.Game).ToListAsync();

        games.Should().HaveCount(9);
        games.Should().ContainSingle(game => game.IsSystemFallback && game.IsActive && game.IsApproved);
        translations.Should().Contain(t => t.Culture == "en" && t.Text == "Soccer");
        translations.Should().Contain(t => t.Culture == "de" && t.Text == "Sonstiges");
    }

    [Fact]
    public async Task SeedTagAndGameCatalogAsync_DoesNotDuplicateWhenRunTwice()
    {
        await using var scope = await CreateSeederScopeAsync();

        await InvokeSeederAsync(scope.Seeder, "SeedTagsAsync");
        await InvokeSeederAsync(scope.Seeder, "SeedGamesAsync");
        await InvokeSeederAsync(scope.Seeder, "SeedTagsAsync");
        await InvokeSeederAsync(scope.Seeder, "SeedGamesAsync");

        (await scope.Context.Tags.CountAsync()).Should().Be(6);
        (await scope.Context.Games.CountAsync()).Should().Be(9);
        (await scope.Context.Translations.CountAsync()).Should().Be(30);
    }

    private static async Task InvokeSeederAsync(DbSeeder seeder, string methodName)
    {
        var method = typeof(DbSeeder).GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        method.Should().NotBeNull();

        var result = method!.Invoke(seeder, null);
        result.Should().BeAssignableTo<Task>();
        await (Task)result!;
    }

    private static async Task<SeederScope> CreateSeederScopeAsync()
    {
        var (connection, context) = TrainingBlockTestData.CreateContext();
        var translationService = TrainingBlockTestData.CreateTranslationService(context);

        var passwordHasherMock = new Mock<IPasswordHasher>();
        passwordHasherMock.Setup(hasher => hasher.Hash(It.IsAny<string>())).Returns("hash");

        var seeder = new DbSeeder(
            context,
            passwordHasherMock.Object,
            new ConfigurationBuilder().Build(),
            Mock.Of<ILogger<DbSeeder>>(),
            translationService);

        await Task.CompletedTask;
        return new SeederScope(connection, context, seeder);
    }

    private sealed class SeederScope(SqliteConnection connection, ApplicationDbContext context, DbSeeder seeder) : IAsyncDisposable
    {
        public SqliteConnection Connection { get; } = connection;
        public ApplicationDbContext Context { get; } = context;
        public DbSeeder Seeder { get; } = seeder;

        public async ValueTask DisposeAsync()
        {
            await Context.DisposeAsync();
            await Connection.DisposeAsync();
        }
    }
}
