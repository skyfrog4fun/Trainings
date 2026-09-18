using FluentAssertions;
using Trainings.Application.Constants;
using Trainings.Application.DTOs;
using Trainings.Infrastructure.Services;

namespace Trainings.Application.Tests.Services;

public class TrainingBlockLibraryServiceTests
{
    [Fact]
    public async Task SearchAsync_FiltersByVisibilityAndSearchInputs()
    {
        await using var scope = await CreateScopeAsync();
        var creatorOne = await TrainingBlockTestData.AddUserAsync(scope.Context, "Nina", "North", "nina@example.com");
        var creatorTwo = await TrainingBlockTestData.AddUserAsync(scope.Context, "Omar", "Oak", "omar@example.com");
        var groupOne = await TrainingBlockTestData.AddGroupAsync(scope.Context, "Group One", "group-one", "group-one");
        var groupTwo = await TrainingBlockTestData.AddGroupAsync(scope.Context, "Group Two", "group-two", "group-two");
        var warmUpTag = await TrainingBlockTestData.AddWarmUpTagAsync(scope.Context, scope.TranslationService);
        var gameTag = await TrainingBlockTestData.AddGameTagAsync(scope.Context, scope.TranslationService);
        var game = await TrainingBlockTestData.AddGameAsync(scope.Context, scope.TranslationService, "Soccer", "Fussball");

        await TrainingBlockTestData.AddDefinitionAsync(scope.Context, warmUpTag.Id, creatorOne.Id, groupOne.Id, "Passing prep", 12, 4, 10, description: "Warm up with passing");
        await TrainingBlockTestData.AddDefinitionAsync(scope.Context, warmUpTag.Id, creatorTwo.Id, null, "Global mobility", 8, 4, 12, isGlobal: true, description: "Joint mobility");
        await TrainingBlockTestData.AddDefinitionAsync(scope.Context, gameTag.Id, creatorOne.Id, groupTwo.Id, "Hidden scrimmage", 25, 8, 16, gameId: game.Id, description: "Should not be visible");
        await TrainingBlockTestData.AddDefinitionAsync(scope.Context, warmUpTag.Id, creatorOne.Id, groupOne.Id, "Inactive drill", 10, 4, 8, isActive: false);

        var result = await scope.Service.SearchAsync(new TrainingBlockLibrarySearchDto
        {
            GroupId = groupOne.Id,
            TagId = warmUpTag.Id,
            MinDurationMinutes = 10,
            SearchText = "passing",
            Take = 10
        });

        result.Items.Should().ContainSingle();
        result.Items[0].Title.Should().Be("Passing prep");
        result.CreatorOptions.Should().ContainSingle(option => option.CreatorId == creatorOne.Id);
    }

    [Fact]
    public async Task SearchAsync_PaginatesVisibleDefinitions()
    {
        await using var scope = await CreateScopeAsync();
        var creator = await TrainingBlockTestData.AddUserAsync(scope.Context, "Lia", "Lane", "lia@example.com");
        var group = await TrainingBlockTestData.AddGroupAsync(scope.Context);
        var tag = await TrainingBlockTestData.AddWarmUpTagAsync(scope.Context, scope.TranslationService);

        await TrainingBlockTestData.AddDefinitionAsync(scope.Context, tag.Id, creator.Id, null, "Newest", 14, 4, 8, isGlobal: true);
        await Task.Delay(5);
        await TrainingBlockTestData.AddDefinitionAsync(scope.Context, tag.Id, creator.Id, null, "Middle", 12, 4, 8, isGlobal: true);
        await Task.Delay(5);
        await TrainingBlockTestData.AddDefinitionAsync(scope.Context, tag.Id, creator.Id, null, "Oldest", 10, 4, 8, isGlobal: true);

        var firstPage = await scope.Service.SearchAsync(new TrainingBlockLibrarySearchDto { GroupId = group.Id, Skip = 0, Take = 2 });
        var secondPage = await scope.Service.SearchAsync(new TrainingBlockLibrarySearchDto { GroupId = group.Id, Skip = 2, Take = 2 });

        firstPage.Items.Should().HaveCount(2);
        firstPage.HasMore.Should().BeTrue();
        secondPage.Items.Should().ContainSingle();
        secondPage.HasMore.Should().BeFalse();
    }

    private static async Task<TrainingBlockLibraryServiceScope> CreateScopeAsync()
    {
        var (connection, context) = TrainingBlockTestData.CreateContext();
        var translationService = TrainingBlockTestData.CreateTranslationService(context);
        var service = new TrainingBlockLibraryService(context, translationService);

        await Task.CompletedTask;
        return new TrainingBlockLibraryServiceScope(connection, context, translationService, service);
    }

    private sealed class TrainingBlockLibraryServiceScope(
        Microsoft.Data.Sqlite.SqliteConnection connection,
        Trainings.Infrastructure.Data.ApplicationDbContext context,
        TranslationService translationService,
        TrainingBlockLibraryService service) : IAsyncDisposable
    {
        public Microsoft.Data.Sqlite.SqliteConnection Connection { get; } = connection;
        public Trainings.Infrastructure.Data.ApplicationDbContext Context { get; } = context;
        public TranslationService TranslationService { get; } = translationService;
        public TrainingBlockLibraryService Service { get; } = service;

        public async ValueTask DisposeAsync()
        {
            await Context.DisposeAsync();
            await Connection.DisposeAsync();
        }
    }
}
