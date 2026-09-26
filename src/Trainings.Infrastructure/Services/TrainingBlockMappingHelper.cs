using Trainings.Application.DTOs;
using Trainings.Domain.Entities;

namespace Trainings.Infrastructure.Services;

internal static class TrainingBlockMappingHelper
{
    public static TrainingBlockDefinitionDto MapDefinition(
        TrainingBlockDefinition definition,
        IReadOnlyDictionary<int, TranslationTextsDto> tagTexts,
        IReadOnlyDictionary<int, TranslationTextsDto> gameTexts)
    {
        string tagText = tagTexts.TryGetValue(definition.TagId, out var resolvedTagText)
            ? resolvedTagText.CurrentText
            : definition.Tag.Key;

        string? gameText = null;
        if (definition.GameId.HasValue && gameTexts.TryGetValue(definition.GameId.Value, out var resolvedGameText))
        {
            gameText = resolvedGameText.CurrentText;
        }

        return new TrainingBlockDefinitionDto
        {
            Id = definition.Id,
            Title = definition.Title,
            Description = definition.Description,
            DurationMinutes = definition.DurationMinutes,
            TagId = definition.TagId,
            TagKey = definition.Tag.Key,
            TagDisplayText = tagText,
            TagColorToken = definition.Tag.ColorToken,
            GameId = definition.GameId,
            GameDisplayText = gameText,
            MinParticipants = definition.MinParticipants,
            MaxParticipants = definition.MaxParticipants,
            GroupId = definition.GroupId,
            IsGlobal = definition.IsGlobal,
            CreatorId = definition.CreatorId,
            CreatorName = definition.Creator.DisplayName,
            CreatedAt = definition.CreatedAt,
            IsActive = definition.IsActive
        };
    }

    public static TrainingBlockDto MapExecution(
        TrainingBlock execution,
        IReadOnlyDictionary<int, TranslationTextsDto> tagTexts,
        IReadOnlyDictionary<int, TranslationTextsDto> gameTexts)
    {
        var definition = execution.Definition;
        string tagText = tagTexts.TryGetValue(definition.TagId, out var resolvedTagText) ? resolvedTagText.CurrentText : definition.Tag.Key;

        string? gameText = null;
        if (definition.GameId.HasValue && gameTexts.TryGetValue(definition.GameId.Value, out var resolvedGameText))
        {
            gameText = resolvedGameText.CurrentText;
        }

        return new TrainingBlockDto
        {
            Id = execution.Id,
            TrainingId = execution.TrainingId,
            OrderIndex = execution.OrderIndex,
            DefinitionId = execution.DefinitionId,
            Title = execution.Title,
            Description = execution.Description,
            PlannedDurationMinutes = execution.PlannedDurationMinutes,
            MinParticipants = execution.MinParticipants,
            MaxParticipants = execution.MaxParticipants,
            TagId = definition.TagId,
            TagKey = definition.Tag.Key,
            TagDisplayText = tagText,
            TagColorToken = definition.Tag.ColorToken,
            GameId = definition.GameId,
            GameDisplayText = gameText,
            DefinitionIsActive = definition.IsActive,
            CreatedAt = execution.CreatedAt
        };
    }
}
