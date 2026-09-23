using Microsoft.EntityFrameworkCore;
using Trainings.Application.Constants;
using Trainings.Application.DTOs;
using Trainings.Application.Interfaces;
using Trainings.Domain.Entities;
using Trainings.Domain.Enums;
using Trainings.Infrastructure.Data;

namespace Trainings.Infrastructure.Services;

public class TrainingBlockService(
    ApplicationDbContext context,
    IAppRuntimeModeService appRuntimeModeService,
    ITranslationService translationService,
    ICurrentUserContext currentUserContext,
    IGameAdminService gameAdminService) : ITrainingBlockService
{
    private readonly ApplicationDbContext _context = context;
    private readonly IAppRuntimeModeService _appRuntimeModeService = appRuntimeModeService;
    private readonly ITranslationService _translationService = translationService;
    private readonly ICurrentUserContext _currentUserContext = currentUserContext;
    private readonly IGameAdminService _gameAdminService = gameAdminService;

    public async Task<IReadOnlyList<TrainingBlockDto>> GetExecutionsAsync(int trainingId, CancellationToken ct = default)
    {
        var executions = await _context.TrainingBlocks
            .Where(b => b.TrainingId == trainingId)
            .OrderBy(b => b.OrderIndex)
            .Include(b => b.Definition)
                .ThenInclude(d => d.Tag)
            .Include(b => b.Definition)
                .ThenInclude(d => d.Game)
            .AsNoTracking()
            .ToListAsync(ct);

        return await MapExecutionsAsync(executions, ct);
    }

    public async Task<TrainingBlockDto> CreateDefinitionAndAddExecutionAsync(CreateTrainingBlockDto dto, CancellationToken ct = default)
    {
        _appRuntimeModeService.EnsureWriteAllowed();
        ValidateDefinitionInput(dto.Title, dto.Description, dto.DurationMinutes, dto.MinParticipants, dto.MaxParticipants);

        var training = await _context.Trainings.FirstOrDefaultAsync(t => t.Id == dto.TrainingId, ct)
            ?? throw new InvalidOperationException($"Training {dto.TrainingId} not found.");

        var tag = await _context.Tags.FirstOrDefaultAsync(t => t.Id == dto.TagId, ct)
            ?? throw new InvalidOperationException($"Tag {dto.TagId} not found.");

        int creatorId = _currentUserContext.GetCurrentUserId()
            ?? throw new InvalidOperationException("A signed-in user is required to create a training block definition.");

        var creator = await _context.Users.FirstOrDefaultAsync(u => u.Id == creatorId, ct)
            ?? throw new InvalidOperationException($"User {creatorId} not found.");

        int? resolvedGameId = await ResolveGameIdAsync(tag.Key, dto.GameId, dto.NewGameName, creatorId, ct);
        bool isSuperAdmin = creator.Role == UserRole.SuperAdmin;

        var definition = new TrainingBlockDefinition
        {
            Title = dto.Title.Trim(),
            Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim(),
            DurationMinutes = dto.DurationMinutes,
            TagId = tag.Id,
            GameId = resolvedGameId,
            MinParticipants = dto.MinParticipants,
            MaxParticipants = dto.MaxParticipants,
            GroupId = isSuperAdmin && dto.CreateGlobalDefinition ? null : training.GroupId,
            IsGlobal = isSuperAdmin && dto.CreateGlobalDefinition,
            CreatorId = creatorId,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        _context.TrainingBlockDefinitions.Add(definition);
        await _context.SaveChangesAsync(ct);

        var execution = await CreateExecutionAsync(training.Id, definition, ct);
        return await GetExecutionByIdAsync(execution.Id, ct);
    }

    public async Task<TrainingBlockDto> AddExecutionFromDefinitionAsync(AddTrainingBlockFromDefinitionDto dto, CancellationToken ct = default)
    {
        _appRuntimeModeService.EnsureWriteAllowed();

        var definition = await _context.TrainingBlockDefinitions
            .Include(d => d.Tag)
            .Include(d => d.Game)
            .Include(d => d.Creator)
            .FirstOrDefaultAsync(d => d.Id == dto.DefinitionId, ct)
            ?? throw new InvalidOperationException($"Definition {dto.DefinitionId} not found.");

        if (!definition.IsActive)
        {
            throw new InvalidOperationException("Inactive block definitions cannot be added to a training.");
        }

        var training = await _context.Trainings.FirstOrDefaultAsync(t => t.Id == dto.TrainingId, ct)
            ?? throw new InvalidOperationException($"Training {dto.TrainingId} not found.");

        if (!definition.IsGlobal && definition.GroupId != training.GroupId)
        {
            throw new InvalidOperationException("This block definition is not available for the current training group.");
        }

        var execution = await CreateExecutionAsync(training.Id, definition, ct);
        return await GetExecutionByIdAsync(execution.Id, ct);
    }

    public async Task UpdateExecutionAsync(UpdateTrainingBlockExecutionDto dto, CancellationToken ct = default)
    {
        _appRuntimeModeService.EnsureWriteAllowed();
        ValidateExecutionInput(dto.Title, dto.Description, dto.PlannedDurationMinutes, dto.MinParticipants, dto.MaxParticipants, dto.EffectiveDurationMinutes);

        var execution = await _context.TrainingBlocks.FirstOrDefaultAsync(b => b.Id == dto.Id, ct)
            ?? throw new InvalidOperationException($"Execution {dto.Id} not found.");

        execution.Title = dto.Title.Trim();
        execution.Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim();
        execution.PlannedDurationMinutes = dto.PlannedDurationMinutes;
        execution.MinParticipants = dto.MinParticipants;
        execution.MaxParticipants = dto.MaxParticipants;
        execution.EffectiveDurationMinutes = dto.EffectiveDurationMinutes;
        execution.TrainerComment = string.IsNullOrWhiteSpace(dto.TrainerComment) ? null : dto.TrainerComment.Trim();

        await _context.SaveChangesAsync(ct);
    }

    public async Task DeleteExecutionAsync(int executionId, CancellationToken ct = default)
    {
        _appRuntimeModeService.EnsureWriteAllowed();

        var execution = await _context.TrainingBlocks.FirstOrDefaultAsync(b => b.Id == executionId, ct)
            ?? throw new InvalidOperationException($"Execution {executionId} not found.");

        int trainingId = execution.TrainingId;
        _context.TrainingBlocks.Remove(execution);
        await _context.SaveChangesAsync(ct);
        await NormalizeOrderAsync(trainingId, ct);
    }

    public Task MoveExecutionUpAsync(int executionId, CancellationToken ct = default) => MoveExecutionAsync(executionId, -1, ct);

    public Task MoveExecutionDownAsync(int executionId, CancellationToken ct = default) => MoveExecutionAsync(executionId, 1, ct);

    private async Task MoveExecutionAsync(int executionId, int direction, CancellationToken ct)
    {
        _appRuntimeModeService.EnsureWriteAllowed();

        var current = await _context.TrainingBlocks.FirstOrDefaultAsync(b => b.Id == executionId, ct)
            ?? throw new InvalidOperationException($"Execution {executionId} not found.");

        var swapWith = await _context.TrainingBlocks.FirstOrDefaultAsync(
            b => b.TrainingId == current.TrainingId && b.OrderIndex == current.OrderIndex + direction,
            ct);

        if (swapWith is null)
        {
            return;
        }

        int targetOrderIndex = swapWith.OrderIndex;
        swapWith.OrderIndex = -1;
        await _context.SaveChangesAsync(ct);

        current.OrderIndex = targetOrderIndex;
        await _context.SaveChangesAsync(ct);

        swapWith.OrderIndex = current.OrderIndex - direction;
        await _context.SaveChangesAsync(ct);
    }

    private async Task<TrainingBlock> CreateExecutionAsync(int trainingId, TrainingBlockDefinition definition, CancellationToken ct)
    {
        int nextOrder = await _context.TrainingBlocks
            .Where(b => b.TrainingId == trainingId)
            .MaxAsync(b => (int?)b.OrderIndex, ct) ?? 0;

        var execution = new TrainingBlock
        {
            TrainingId = trainingId,
            DefinitionId = definition.Id,
            OrderIndex = nextOrder + 1,
            Title = definition.Title,
            Description = definition.Description,
            PlannedDurationMinutes = definition.DurationMinutes,
            MinParticipants = definition.MinParticipants,
            MaxParticipants = definition.MaxParticipants,
            CreatedAt = DateTime.UtcNow
        };

        _context.TrainingBlocks.Add(execution);
        await _context.SaveChangesAsync(ct);
        return execution;
    }

    private async Task<int?> ResolveGameIdAsync(string tagKey, int? gameId, string? newGameName, int creatorId, CancellationToken ct)
    {
        if (!string.Equals(tagKey, TrainingBlockCatalog.TagKeys.Game, StringComparison.Ordinal))
        {
            return null;
        }

        if (gameId.HasValue)
        {
            var existingGame = await _context.Games.FirstOrDefaultAsync(g => g.Id == gameId.Value && g.IsActive, ct)
                ?? throw new InvalidOperationException("The selected game is not available.");
            return existingGame.Id;
        }

        if (!string.IsNullOrWhiteSpace(newGameName))
        {
            var game = await _gameAdminService.CreateAdHocAsync(newGameName, creatorId, ct);
            return game.Id;
        }

        throw new InvalidOperationException("A game must be selected or created when the Game tag is used.");
    }

    private async Task<IReadOnlyList<TrainingBlockDto>> MapExecutionsAsync(IReadOnlyList<TrainingBlock> executions, CancellationToken ct)
    {
        var tagTexts = await _translationService.GetTextLookupAsync(TranslationEntityType.Tag, executions.Select(e => e.Definition.TagId), ct);
        var gameTexts = await _translationService.GetTextLookupAsync(
            TranslationEntityType.Game,
            executions.Where(e => e.Definition.GameId.HasValue).Select(e => e.Definition.GameId!.Value),
            ct);

        return [.. executions.Select(execution => TrainingBlockMappingHelper.MapExecution(execution, tagTexts, gameTexts))];
    }

    private async Task<TrainingBlockDto> GetExecutionByIdAsync(int executionId, CancellationToken ct)
    {
        var execution = await _context.TrainingBlocks
            .Include(b => b.Definition)
                .ThenInclude(d => d.Tag)
            .Include(b => b.Definition)
                .ThenInclude(d => d.Game)
            .FirstOrDefaultAsync(b => b.Id == executionId, ct)
            ?? throw new InvalidOperationException($"Execution {executionId} not found.");

        var mapped = await MapExecutionsAsync([execution], ct);
        return mapped[0];
    }

    private async Task NormalizeOrderAsync(int trainingId, CancellationToken ct)
    {
        var executions = await _context.TrainingBlocks
            .Where(b => b.TrainingId == trainingId)
            .OrderBy(b => b.OrderIndex)
            .ToListAsync(ct);

        for (int index = 0; index < executions.Count; index++)
        {
            executions[index].OrderIndex = index + 1;
        }

        await _context.SaveChangesAsync(ct);
    }

    private static void ValidateDefinitionInput(string title, string? description, int durationMinutes, int minParticipants, int maxParticipants)
    {
        ValidateTitleAndDescription(title, description);
        ValidateParticipantsAndDurations(durationMinutes, minParticipants, maxParticipants);
    }

    private static void ValidateExecutionInput(string title, string? description, int plannedDurationMinutes, int minParticipants, int maxParticipants, int? effectiveDurationMinutes)
    {
        ValidateTitleAndDescription(title, description);
        ValidateParticipantsAndDurations(plannedDurationMinutes, minParticipants, maxParticipants);

        if (effectiveDurationMinutes.HasValue && effectiveDurationMinutes.Value < 0)
        {
            throw new InvalidOperationException("Effective duration cannot be negative.");
        }
    }

    private static void ValidateTitleAndDescription(string title, string? description)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new InvalidOperationException("Title is required.");
        }

        if (title.Trim().Length > 64)
        {
            throw new InvalidOperationException("Title cannot be longer than 64 characters.");
        }

        if (!string.IsNullOrWhiteSpace(description) && description.Trim().Length > 512)
        {
            throw new InvalidOperationException("Description cannot be longer than 512 characters.");
        }
    }

    private static void ValidateParticipantsAndDurations(int durationMinutes, int minParticipants, int maxParticipants)
    {
        if (durationMinutes <= 0)
        {
            throw new InvalidOperationException("Duration must be greater than zero.");
        }

        if (minParticipants < 1)
        {
            throw new InvalidOperationException("Minimum participants must be at least 1.");
        }

        if (maxParticipants < minParticipants)
        {
            throw new InvalidOperationException("Minimum participants cannot be greater than maximum participants.");
        }
    }
}
