using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Trainings.Application.DTOs;
using Trainings.Domain.Enums;
using Trainings.Infrastructure.Services;

namespace Trainings.Application.Tests.Services;

public class TrainingBlockServiceTests
{
    [Fact]
    public async Task CreateDefinitionAndAddExecutionAsyncCreatesDefinitionAndLocalExecutionCopy()
    {
        await using var scope = await CreateScopeAsync(UserRole.User);
        var warmUpTag = await TrainingBlockTestData.AddWarmUpTagAsync(scope.Context, scope.TranslationService, ct: TestContext.Current.CancellationToken);

        var result = await scope.Service.CreateDefinitionAndAddExecutionAsync(new CreateTrainingBlockDto
        {
            TrainingId = scope.Training.Id,
            Title = "Reaction ladder",
            Description = "Quick feet drill",
            DurationMinutes = 14,
            TagId = warmUpTag.Id,
            MinParticipants = 4,
            MaxParticipants = 10
        }, ct: TestContext.Current.CancellationToken);

        var definition = await scope.Context.TrainingBlockDefinitions.SingleAsync(cancellationToken: TestContext.Current.CancellationToken);
        var execution = await scope.Context.TrainingBlocks.SingleAsync(cancellationToken: TestContext.Current.CancellationToken);

        definition.IsGlobal.Should().BeFalse();
        definition.GroupId.Should().Be(scope.Group.Id);
        execution.Title.Should().Be("Reaction ladder");
        execution.DefinitionId.Should().Be(definition.Id);
        result.TagDisplayText.Should().Be("Warm-Up");
    }

    [Fact]
    public async Task CreateDefinitionAndAddExecutionAsyncRequiresGameWhenUsingGameTag()
    {
        await using var scope = await CreateScopeAsync(UserRole.User);
        var gameTag = await TrainingBlockTestData.AddGameTagAsync(scope.Context, scope.TranslationService, ct: TestContext.Current.CancellationToken);

        var act = async () => await scope.Service.CreateDefinitionAndAddExecutionAsync(new CreateTrainingBlockDto
        {
            TrainingId = scope.Training.Id,
            Title = "Scrimmage",
            DurationMinutes = 20,
            TagId = gameTag.Id,
            MinParticipants = 8,
            MaxParticipants = 14
        });

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*must be selected or created*");
    }

    [Fact]
    public async Task CreateDefinitionAndAddExecutionAsyncCreatesAdHocGameForGameTag()
    {
        await using var scope = await CreateScopeAsync(UserRole.User);
        var gameTag = await TrainingBlockTestData.AddGameTagAsync(scope.Context, scope.TranslationService, ct: TestContext.Current.CancellationToken);

        var result = await scope.Service.CreateDefinitionAndAddExecutionAsync(new CreateTrainingBlockDto
        {
            TrainingId = scope.Training.Id,
            Title = "Scrimmage",
            DurationMinutes = 20,
            TagId = gameTag.Id,
            NewGameName = "Kickball",
            MinParticipants = 8,
            MaxParticipants = 14
        }, ct: TestContext.Current.CancellationToken);

        var createdGame = await scope.Context.Games.SingleAsync(cancellationToken: TestContext.Current.CancellationToken);
        createdGame.IsApproved.Should().BeFalse();
        createdGame.CreatedByUserId.Should().Be(scope.Creator.Id);
        result.GameDisplayText.Should().Be("Kickball");
    }

    [Fact]
    public async Task CreateDefinitionAndAddExecutionAsyncValidatesTitleAndParticipantRules()
    {
        await using var scope = await CreateScopeAsync(UserRole.User);
        var warmUpTag = await TrainingBlockTestData.AddWarmUpTagAsync(scope.Context, scope.TranslationService, ct: TestContext.Current.CancellationToken);

        var act = async () => await scope.Service.CreateDefinitionAndAddExecutionAsync(new CreateTrainingBlockDto
        {
            TrainingId = scope.Training.Id,
            Title = new string('A', 65),
            DurationMinutes = 10,
            TagId = warmUpTag.Id,
            MinParticipants = 6,
            MaxParticipants = 4
        });

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task AddUpdateMoveAndDeleteAsyncManageExecutionOrderAndOverrides()
    {
        await using var scope = await CreateScopeAsync(UserRole.SuperAdmin);
        var warmUpTag = await TrainingBlockTestData.AddWarmUpTagAsync(scope.Context, scope.TranslationService, ct: TestContext.Current.CancellationToken);
        var definitionOne = await TrainingBlockTestData.AddDefinitionAsync(scope.Context, warmUpTag.Id, scope.Creator.Id, null, "Block one", 10, 4, 8, isGlobal: true, ct: TestContext.Current.CancellationToken);
        var definitionTwo = await TrainingBlockTestData.AddDefinitionAsync(scope.Context, warmUpTag.Id, scope.Creator.Id, null, "Block two", 12, 4, 8, isGlobal: true, ct: TestContext.Current.CancellationToken);

        var first = await scope.Service.AddExecutionFromDefinitionAsync(new AddTrainingBlockFromDefinitionDto { TrainingId = scope.Training.Id, DefinitionId = definitionOne.Id }, ct: TestContext.Current.CancellationToken);
        var second = await scope.Service.AddExecutionFromDefinitionAsync(new AddTrainingBlockFromDefinitionDto { TrainingId = scope.Training.Id, DefinitionId = definitionTwo.Id }, ct: TestContext.Current.CancellationToken);

        await scope.Service.UpdateExecutionAsync(new UpdateTrainingBlockExecutionDto
        {
            Id = first.Id,
            Title = "Adjusted block one",
            Description = "Adjusted",
            PlannedDurationMinutes = 11,
            MinParticipants = 5,
            MaxParticipants = 9,
            EffectiveDurationMinutes = 9,
            TrainerComment = "Good"
        }, ct: TestContext.Current.CancellationToken);

        await scope.Service.MoveExecutionDownAsync(first.Id, ct: TestContext.Current.CancellationToken);
        await scope.Service.DeleteExecutionAsync(first.Id, ct: TestContext.Current.CancellationToken);

        var executions = await scope.Service.GetExecutionsAsync(scope.Training.Id, ct: TestContext.Current.CancellationToken);
        executions.Should().ContainSingle();
        executions[0].Title.Should().Be("Block two");
        executions[0].OrderIndex.Should().Be(1);
        executions[0].DefinitionId.Should().Be(second.DefinitionId);
    }

    private static async Task<TrainingBlockServiceScope> CreateScopeAsync(UserRole role)
    {
        var (connection, context) = TrainingBlockTestData.CreateContext();
        var translationService = TrainingBlockTestData.CreateTranslationService(context);
        var runtimeModeMock = TrainingBlockTestData.CreateRuntimeModeMock();
        var creator = await TrainingBlockTestData.AddUserAsync(context, "Sam", "Trainer", $"{Guid.NewGuid():N}@example.com", role);
        var group = await TrainingBlockTestData.AddGroupAsync(context);
        var training = await TrainingBlockTestData.AddTrainingAsync(context, group.Id);
        var currentUserContextMock = TrainingBlockTestData.CreateCurrentUserContextMock(creator.Id);
        var gameAdminService = new GameAdminService(context, translationService, runtimeModeMock.Object);
        var service = new TrainingBlockService(context, runtimeModeMock.Object, translationService, currentUserContextMock.Object, gameAdminService);

        return new TrainingBlockServiceScope(connection, context, translationService, service, creator, group, training);
    }

    private sealed class TrainingBlockServiceScope(
        Microsoft.Data.Sqlite.SqliteConnection connection,
        Trainings.Infrastructure.Data.ApplicationDbContext context,
        TranslationService translationService,
        TrainingBlockService service,
        Trainings.Domain.Entities.User creator,
        Trainings.Domain.Entities.Group group,
        Trainings.Domain.Entities.Training training) : IAsyncDisposable
    {
        public Microsoft.Data.Sqlite.SqliteConnection Connection { get; } = connection;
        public Trainings.Infrastructure.Data.ApplicationDbContext Context { get; } = context;
        public TranslationService TranslationService { get; } = translationService;
        public TrainingBlockService Service { get; } = service;
        public Trainings.Domain.Entities.User Creator { get; } = creator;
        public Trainings.Domain.Entities.Group Group { get; } = group;
        public Trainings.Domain.Entities.Training Training { get; } = training;

        public async ValueTask DisposeAsync()
        {
            await Context.DisposeAsync();
            await Connection.DisposeAsync();
        }
    }
}
