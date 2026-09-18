using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using Trainings.Application.Constants;
using Trainings.Application.DTOs;
using Trainings.Application.Interfaces;
using Trainings.Domain.Entities;
using Trainings.Domain.Enums;
using Trainings.Infrastructure.Data;
using Trainings.Infrastructure.Services;

namespace Trainings.Application.Tests.Services;

internal static class TrainingBlockTestData
{
    public static (SqliteConnection Connection, ApplicationDbContext Context) CreateContext()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;

        var context = new ApplicationDbContext(options);
        context.Database.EnsureCreated();

        return (connection, context);
    }

    public static TranslationService CreateTranslationService(ApplicationDbContext context) => new(context);

    public static Mock<IAppRuntimeModeService> CreateRuntimeModeMock()
    {
        var runtimeModeMock = new Mock<IAppRuntimeModeService>();
        runtimeModeMock.Setup(service => service.EnsureWriteAllowed());
        runtimeModeMock.Setup(service => service.GetCurrent()).Returns(new AppRuntimeModeDto());
        return runtimeModeMock;
    }

    public static Mock<ICurrentUserContext> CreateCurrentUserContextMock(int? userId)
    {
        var currentUserContextMock = new Mock<ICurrentUserContext>();
        currentUserContextMock.Setup(service => service.GetCurrentUserId()).Returns(userId);
        return currentUserContextMock;
    }

    public static async Task<User> AddUserAsync(
        ApplicationDbContext context,
        string firstName,
        string lastName,
        string email,
        UserRole role = UserRole.User,
        CancellationToken ct = default)
    {
        var user = new User
        {
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            PasswordHash = "hash",
            Role = role,
            Gender = Gender.Other,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreationDate = DateTime.UtcNow
        };

        context.Users.Add(user);
        await context.SaveChangesAsync(ct);
        return user;
    }

    public static async Task<Group> AddGroupAsync(
        ApplicationDbContext context,
        string name = "Group One",
        string slug = "group-one",
        string identifier = "group-one",
        CancellationToken ct = default)
    {
        var group = new Group
        {
            Name = name,
            Slug = slug,
            Identifier = identifier,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        context.Groups.Add(group);
        await context.SaveChangesAsync(ct);
        return group;
    }

    public static async Task<Training> AddTrainingAsync(
        ApplicationDbContext context,
        int groupId,
        string title = "Training Plan",
        CancellationToken ct = default)
    {
        var training = new Training
        {
            Title = title,
            Description = "Session",
            GroupId = groupId,
            DateTime = DateTime.UtcNow.AddDays(1),
            Capacity = 20,
            Status = TrainingStatus.InPlanning,
            IsActive = true
        };

        context.Trainings.Add(training);
        await context.SaveChangesAsync(ct);
        return training;
    }

    public static async Task<Tag> AddTagAsync(
        ApplicationDbContext context,
        TranslationService translationService,
        string key,
        string englishText,
        string germanText,
        string colorToken,
        int displayOrder,
        bool isActive = true,
        CancellationToken ct = default)
    {
        var tag = new Tag
        {
            Key = key,
            ColorToken = colorToken,
            DisplayOrder = displayOrder,
            IsActive = isActive
        };

        context.Tags.Add(tag);
        await context.SaveChangesAsync(ct);
        await translationService.UpsertAsync(TranslationEntityType.Tag, tag.Id, englishText, germanText, ct);
        return tag;
    }

    public static async Task<Game> AddGameAsync(
        ApplicationDbContext context,
        TranslationService translationService,
        string englishText,
        string germanText,
        bool isActive = true,
        bool isApproved = true,
        bool isSystemFallback = false,
        int? createdByUserId = null,
        CancellationToken ct = default)
    {
        var game = new Game
        {
            IsActive = isActive,
            IsApproved = isApproved,
            IsSystemFallback = isSystemFallback,
            CreatedByUserId = createdByUserId,
            CreatedAt = DateTime.UtcNow
        };

        context.Games.Add(game);
        await context.SaveChangesAsync(ct);
        await translationService.UpsertAsync(TranslationEntityType.Game, game.Id, englishText, germanText, ct);
        return game;
    }

    public static async Task<TrainingBlockDefinition> AddDefinitionAsync(
        ApplicationDbContext context,
        int tagId,
        int creatorId,
        int? groupId,
        string title,
        int durationMinutes,
        int minParticipants,
        int maxParticipants,
        bool isGlobal = false,
        bool isActive = true,
        int? gameId = null,
        string? description = null,
        CancellationToken ct = default)
    {
        var definition = new TrainingBlockDefinition
        {
            Title = title,
            Description = description,
            DurationMinutes = durationMinutes,
            TagId = tagId,
            GameId = gameId,
            MinParticipants = minParticipants,
            MaxParticipants = maxParticipants,
            GroupId = groupId,
            IsGlobal = isGlobal,
            CreatorId = creatorId,
            CreatedAt = DateTime.UtcNow,
            IsActive = isActive
        };

        context.TrainingBlockDefinitions.Add(definition);
        await context.SaveChangesAsync(ct);
        return definition;
    }

    public static async Task<TrainingBlock> AddExecutionAsync(
        ApplicationDbContext context,
        int trainingId,
        int definitionId,
        int orderIndex,
        string title,
        int plannedDurationMinutes,
        int minParticipants,
        int maxParticipants,
        string? description = null,
        CancellationToken ct = default)
    {
        var execution = new TrainingBlock
        {
            TrainingId = trainingId,
            DefinitionId = definitionId,
            OrderIndex = orderIndex,
            Title = title,
            Description = description,
            PlannedDurationMinutes = plannedDurationMinutes,
            MinParticipants = minParticipants,
            MaxParticipants = maxParticipants,
            CreatedAt = DateTime.UtcNow
        };

        context.TrainingBlocks.Add(execution);
        await context.SaveChangesAsync(ct);
        return execution;
    }

    public static Task<Tag> AddWarmUpTagAsync(ApplicationDbContext context, TranslationService translationService, CancellationToken ct = default)
        => AddTagAsync(
            context,
            translationService,
            TrainingBlockCatalog.TagKeys.WarmUp,
            "Warm-Up",
            "Warm-Up",
            TrainingBlockCatalog.ColorTokens.Community,
            1,
            true,
            ct);

    public static Task<Tag> AddGameTagAsync(ApplicationDbContext context, TranslationService translationService, CancellationToken ct = default)
        => AddTagAsync(
            context,
            translationService,
            TrainingBlockCatalog.TagKeys.Game,
            "Game",
            "Spiel",
            TrainingBlockCatalog.ColorTokens.Innovation,
            4,
            true,
            ct);
}
