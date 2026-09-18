using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Trainings.Infrastructure.Services;

namespace Trainings.Application.Tests.Services;

public class GameAdminServiceTests
{
    [Fact]
    public async Task CreateAdHocAsync_CreatesUnapprovedGameAndDeduplicatesByName()
    {
        await using var scope = await CreateScopeAsync();
        var creator = await TrainingBlockTestData.AddUserAsync(scope.Context, "Ava", "Coach", "ava@example.com");

        var created = await scope.Service.CreateAdHocAsync("Kickball", creator.Id);
        var duplicate = await scope.Service.CreateAdHocAsync("kickball", creator.Id);

        created.Id.Should().Be(duplicate.Id);
        created.IsApproved.Should().BeFalse();
        created.CreatedByUserId.Should().Be(creator.Id);
        (await scope.Context.Games.CountAsync()).Should().Be(1);
        (await scope.Context.Translations.CountAsync()).Should().Be(2);
    }

    [Fact]
    public async Task ApproveAndRenameAsync_PersistsStateAndTranslations()
    {
        await using var scope = await CreateScopeAsync();
        var game = await TrainingBlockTestData.AddGameAsync(scope.Context, scope.TranslationService, "Soccer", "Fussball", isApproved: false);

        await scope.Service.ApproveAsync(game.Id);
        await scope.Service.RenameAsync(game.Id, "Football", "Fussball neu");

        var storedGame = await scope.Context.Games.SingleAsync();
        var translations = await scope.Context.Translations.Where(t => t.EntityId == game.Id).ToListAsync();

        storedGame.IsApproved.Should().BeTrue();
        translations.Should().Contain(t => t.Culture == "en" && t.Text == "Football");
        translations.Should().Contain(t => t.Culture == "de" && t.Text == "Fussball neu");
    }

    [Fact]
    public async Task DeactivateAsync_ThrowsForSystemFallbackGame()
    {
        await using var scope = await CreateScopeAsync();
        var game = await TrainingBlockTestData.AddGameAsync(scope.Context, scope.TranslationService, "Other/Unspecified", "Andere/Nicht angegeben", isSystemFallback: true);

        var act = async () => await scope.Service.DeactivateAsync(game.Id);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*cannot be deactivated*");
    }

    private static async Task<GameAdminServiceScope> CreateScopeAsync()
    {
        var (connection, context) = TrainingBlockTestData.CreateContext();
        var translationService = TrainingBlockTestData.CreateTranslationService(context);
        var runtimeModeMock = TrainingBlockTestData.CreateRuntimeModeMock();
        var service = new GameAdminService(context, translationService, runtimeModeMock.Object);

        await Task.CompletedTask;
        return new GameAdminServiceScope(connection, context, translationService, service);
    }

    private sealed class GameAdminServiceScope(
        Microsoft.Data.Sqlite.SqliteConnection connection,
        Trainings.Infrastructure.Data.ApplicationDbContext context,
        TranslationService translationService,
        GameAdminService service) : IAsyncDisposable
    {
        public Microsoft.Data.Sqlite.SqliteConnection Connection { get; } = connection;
        public Trainings.Infrastructure.Data.ApplicationDbContext Context { get; } = context;
        public TranslationService TranslationService { get; } = translationService;
        public GameAdminService Service { get; } = service;

        public async ValueTask DisposeAsync()
        {
            await Context.DisposeAsync();
            await Connection.DisposeAsync();
        }
    }
}
