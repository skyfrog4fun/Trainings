using Microsoft.EntityFrameworkCore;
using Trainings.Application.DTOs;
using Trainings.Application.Interfaces;
using Trainings.Domain.Entities;
using Trainings.Domain.Enums;
using Trainings.Infrastructure.Data;

namespace Trainings.Infrastructure.Services;

public class TrainingBlockLibraryService(ApplicationDbContext context, ITranslationService translationService) : ITrainingBlockLibraryService
{
    private readonly ApplicationDbContext _context = context;
    private readonly ITranslationService _translationService = translationService;

    public async Task<TrainingBlockLibrarySearchResultDto> SearchAsync(TrainingBlockLibrarySearchDto dto, CancellationToken ct = default)
    {
        var visibleQuery = BuildVisibleQuery(dto)
            .Include(d => d.Tag)
            .Include(d => d.Game)
            .Include(d => d.Creator)
            .AsNoTracking();

        var creatorScopeQuery = ApplyNonCreatorFilters(visibleQuery, dto);
        var creatorOptions = await creatorScopeQuery
            .OrderBy(d => d.Creator.LastName)
            .ThenBy(d => d.Creator.FirstName)
            .Select(d => new TrainingBlockLibraryCreatorDto
            {
                CreatorId = d.CreatorId,
                CreatorName = (d.Creator.FirstName + " " + d.Creator.LastName).Trim()
            })
            .Distinct()
            .ToListAsync(ct);

        var filteredQuery = ApplyAllFilters(visibleQuery, dto)
            .OrderByDescending(d => d.CreatedAt)
            .ThenByDescending(d => d.Id);

        var definitions = await filteredQuery
            .Skip(dto.Skip)
            .Take(dto.Take + 1)
            .ToListAsync(ct);

        bool hasMore = definitions.Count > dto.Take;
        if (hasMore)
        {
            definitions.RemoveAt(definitions.Count - 1);
        }

        var tagTexts = await _translationService.GetTextLookupAsync(TranslationEntityType.Tag, definitions.Select(d => d.TagId), ct);
        var gameTexts = await _translationService.GetTextLookupAsync(
            TranslationEntityType.Game,
            definitions.Where(d => d.GameId.HasValue).Select(d => d.GameId!.Value),
            ct);

        return new TrainingBlockLibrarySearchResultDto
        {
            Items = definitions.Select(d => TrainingBlockMappingHelper.MapDefinition(d, tagTexts, gameTexts)).ToList(),
            CreatorOptions = creatorOptions,
            HasMore = hasMore
        };
    }

    public async Task<TrainingBlockDefinitionDto?> GetByIdAsync(int definitionId, CancellationToken ct = default)
    {
        var definition = await _context.TrainingBlockDefinitions
            .Include(d => d.Tag)
            .Include(d => d.Game)
            .Include(d => d.Creator)
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == definitionId, ct);

        if (definition is null)
        {
            return null;
        }

        var tagTexts = await _translationService.GetTextLookupAsync(TranslationEntityType.Tag, [definition.TagId], ct);
        var gameTexts = await _translationService.GetTextLookupAsync(
            TranslationEntityType.Game,
            definition.GameId.HasValue ? [definition.GameId.Value] : [],
            ct);

        return TrainingBlockMappingHelper.MapDefinition(definition, tagTexts, gameTexts);
    }

    private IQueryable<TrainingBlockDefinition> BuildVisibleQuery(TrainingBlockLibrarySearchDto dto)
    {
        var query = _context.TrainingBlockDefinitions.Where(d => d.IsActive);
        if (dto.GroupId.HasValue)
        {
            query = query.Where(d => d.IsGlobal || d.GroupId == dto.GroupId.Value);
        }
        else
        {
            query = query.Where(d => d.IsGlobal);
        }

        return query;
    }

    private static IQueryable<TrainingBlockDefinition> ApplyNonCreatorFilters(IQueryable<TrainingBlockDefinition> query, TrainingBlockLibrarySearchDto dto)
    {
        if (dto.TagId.HasValue)
        {
            query = query.Where(d => d.TagId == dto.TagId.Value);
        }

        if (dto.MinDurationMinutes.HasValue)
        {
            query = query.Where(d => d.DurationMinutes >= dto.MinDurationMinutes.Value);
        }

        if (dto.MaxDurationMinutes.HasValue)
        {
            query = query.Where(d => d.DurationMinutes <= dto.MaxDurationMinutes.Value);
        }

        if (!string.IsNullOrWhiteSpace(dto.SearchText))
        {
            string searchText = dto.SearchText.Trim();
            query = query.Where(d => d.Title.Contains(searchText) || (d.Description != null && d.Description.Contains(searchText)));
        }

        return query;
    }

    private static IQueryable<TrainingBlockDefinition> ApplyAllFilters(IQueryable<TrainingBlockDefinition> query, TrainingBlockLibrarySearchDto dto)
    {
        query = ApplyNonCreatorFilters(query, dto);

        if (dto.CreatorId.HasValue)
        {
            query = query.Where(d => d.CreatorId == dto.CreatorId.Value);
        }

        return query;
    }
}
