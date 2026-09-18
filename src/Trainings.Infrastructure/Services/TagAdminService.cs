using Microsoft.EntityFrameworkCore;
using Trainings.Application.Constants;
using Trainings.Application.DTOs;
using Trainings.Application.Interfaces;
using Trainings.Domain.Entities;
using Trainings.Domain.Enums;
using Trainings.Infrastructure.Data;

namespace Trainings.Infrastructure.Services;

public class TagAdminService(ApplicationDbContext context, ITranslationService translationService, IAppRuntimeModeService appRuntimeModeService) : ITagAdminService
{
    private readonly ApplicationDbContext _context = context;
    private readonly ITranslationService _translationService = translationService;
    private readonly IAppRuntimeModeService _appRuntimeModeService = appRuntimeModeService;

    public async Task<IReadOnlyList<TagAdminDto>> GetAllAsync(CancellationToken ct = default)
    {
        var tags = await _context.Tags.OrderBy(t => t.DisplayOrder).ToListAsync(ct);
        var translations = await _translationService.GetTextLookupAsync(TranslationEntityType.Tag, tags.Select(t => t.Id), ct);

        return tags.Select(tag =>
        {
            translations.TryGetValue(tag.Id, out var text);
            return new TagAdminDto
            {
                Id = tag.Id,
                Key = tag.Key,
                EnglishText = text?.EnglishText ?? tag.Key,
                GermanText = text?.GermanText ?? text?.EnglishText ?? tag.Key,
                ColorToken = tag.ColorToken,
                DisplayOrder = tag.DisplayOrder,
                IsActive = tag.IsActive
            };
        }).ToList();
    }

    public async Task<IReadOnlyList<TagDto>> GetActiveForSelectionAsync(CancellationToken ct = default)
    {
        var tags = await _context.Tags
            .Where(t => t.IsActive)
            .OrderBy(t => t.DisplayOrder)
            .ToListAsync(ct);

        var translations = await _translationService.GetTextLookupAsync(TranslationEntityType.Tag, tags.Select(t => t.Id), ct);

        return [.. tags.Select(tag => new TagDto
        {
            Id = tag.Id,
            Key = tag.Key,
            Name = translations.TryGetValue(tag.Id, out var text) ? text.CurrentText : tag.Key,
            ColorToken = tag.ColorToken,
            DisplayOrder = tag.DisplayOrder,
            IsActive = tag.IsActive
        })];
    }

    public async Task<TagAdminDto> CreateAsync(TagAdminDto dto, CancellationToken ct = default)
    {
        _appRuntimeModeService.EnsureWriteAllowed();
        Validate(dto);

        var tag = new Tag
        {
            Key = dto.Key.Trim(),
            ColorToken = dto.ColorToken,
            DisplayOrder = dto.DisplayOrder,
            IsActive = dto.IsActive
        };

        _context.Tags.Add(tag);
        await _context.SaveChangesAsync(ct);
        await _translationService.UpsertAsync(TranslationEntityType.Tag, tag.Id, dto.EnglishText, dto.GermanText, ct);

        dto.Id = tag.Id;
        return dto;
    }

    public async Task UpdateAsync(TagAdminDto dto, CancellationToken ct = default)
    {
        _appRuntimeModeService.EnsureWriteAllowed();
        Validate(dto);

        var tag = await _context.Tags.FirstOrDefaultAsync(t => t.Id == dto.Id, ct)
            ?? throw new InvalidOperationException($"Tag {dto.Id} not found.");

        tag.Key = dto.Key.Trim();
        tag.ColorToken = dto.ColorToken;
        tag.DisplayOrder = dto.DisplayOrder;
        tag.IsActive = dto.IsActive;

        await _context.SaveChangesAsync(ct);
        await _translationService.UpsertAsync(TranslationEntityType.Tag, tag.Id, dto.EnglishText, dto.GermanText, ct);
    }

    public Task DeactivateAsync(int id, CancellationToken ct = default) => SetActiveAsync(id, false, ct);

    public Task ReactivateAsync(int id, CancellationToken ct = default) => SetActiveAsync(id, true, ct);

    private async Task SetActiveAsync(int id, bool isActive, CancellationToken ct)
    {
        _appRuntimeModeService.EnsureWriteAllowed();

        var tag = await _context.Tags.FirstOrDefaultAsync(t => t.Id == id, ct)
            ?? throw new InvalidOperationException($"Tag {id} not found.");

        tag.IsActive = isActive;
        await _context.SaveChangesAsync(ct);
    }

    private static void Validate(TagAdminDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Key))
        {
            throw new InvalidOperationException("Tag key is required.");
        }

        if (string.IsNullOrWhiteSpace(dto.EnglishText) || string.IsNullOrWhiteSpace(dto.GermanText))
        {
            throw new InvalidOperationException("English and German tag texts are required.");
        }

        if (!TrainingBlockCatalog.ColorTokens.Allowed.Contains(dto.ColorToken, StringComparer.Ordinal))
        {
            throw new InvalidOperationException($"Color token '{dto.ColorToken}' is not allowed.");
        }
    }
}
