using Microsoft.EntityFrameworkCore;
using Trainings.Application.DTOs;
using Trainings.Application.Interfaces;
using Trainings.Domain.Entities;
using Trainings.Domain.Enums;
using Trainings.Infrastructure.Data;

namespace Trainings.Infrastructure.Services;

public class GameAdminService(ApplicationDbContext context, ITranslationService translationService, IAppRuntimeModeService appRuntimeModeService) : IGameAdminService
{
    private readonly ApplicationDbContext _context = context;
    private readonly ITranslationService _translationService = translationService;
    private readonly IAppRuntimeModeService _appRuntimeModeService = appRuntimeModeService;

    public async Task<IReadOnlyList<GameAdminDto>> GetAllAsync(CancellationToken ct = default)
    {
        var games = await _context.Games
            .OrderBy(g => g.IsApproved)
            .ThenBy(g => g.CreatedAt)
            .ToListAsync(ct);

        var translations = await _translationService.GetTextLookupAsync(TranslationEntityType.Game, games.Select(g => g.Id), ct);

        return [.. games.Select(game =>
        {
            translations.TryGetValue(game.Id, out var text);
            return new GameAdminDto
            {
                Id = game.Id,
                EnglishText = text?.EnglishText ?? string.Empty,
                GermanText = text?.GermanText ?? text?.EnglishText ?? string.Empty,
                IsActive = game.IsActive,
                IsApproved = game.IsApproved,
                IsSystemFallback = game.IsSystemFallback,
                CreatedByUserId = game.CreatedByUserId,
                CreatedAt = game.CreatedAt
            };
        })];
    }

    public async Task<IReadOnlyList<GameDto>> GetActiveForSelectionAsync(CancellationToken ct = default)
    {
        var games = await _context.Games
            .Where(g => g.IsActive)
            .OrderByDescending(g => g.IsSystemFallback)
            .ThenByDescending(g => g.IsApproved)
            .ThenBy(g => g.CreatedAt)
            .ToListAsync(ct);

        var translations = await _translationService.GetTextLookupAsync(TranslationEntityType.Game, games.Select(g => g.Id), ct);
        return [.. games.Select(game => MapGameDto(game, translations))];
    }

    public async Task<GameDto> CreateAdHocAsync(string name, int requestingUserId, CancellationToken ct = default)
    {
        _appRuntimeModeService.EnsureWriteAllowed();

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new InvalidOperationException("Game name is required.");
        }

        string normalizedName = name.Trim();
        var existingMatch = await FindExistingByNameAsync(normalizedName, ct);
        if (existingMatch is not null)
        {
            var existingTranslations = await _translationService.GetTextLookupAsync(TranslationEntityType.Game, [existingMatch.Id], ct);
            return MapGameDto(existingMatch, existingTranslations);
        }

        var game = new Game
        {
            IsActive = true,
            IsApproved = false,
            IsSystemFallback = false,
            CreatedByUserId = requestingUserId > 0 ? requestingUserId : null,
            CreatedAt = DateTime.UtcNow
        };

        _context.Games.Add(game);
        await _context.SaveChangesAsync(ct);
        await _translationService.UpsertAsync(TranslationEntityType.Game, game.Id, normalizedName, normalizedName, ct);

        var translations = await _translationService.GetTextLookupAsync(TranslationEntityType.Game, [game.Id], ct);
        return MapGameDto(game, translations);
    }

    public async Task ApproveAsync(int id, CancellationToken ct = default)
    {
        _appRuntimeModeService.EnsureWriteAllowed();
        var game = await GetGameAsync(id, ct);
        game.IsApproved = true;
        await _context.SaveChangesAsync(ct);
    }

    public async Task RenameAsync(int id, string englishText, string germanText, CancellationToken ct = default)
    {
        _appRuntimeModeService.EnsureWriteAllowed();

        if (string.IsNullOrWhiteSpace(englishText) || string.IsNullOrWhiteSpace(germanText))
        {
            throw new InvalidOperationException("English and German game texts are required.");
        }

        await GetGameAsync(id, ct);
        await _translationService.UpsertAsync(TranslationEntityType.Game, id, englishText, germanText, ct);
    }

    public async Task DeactivateAsync(int id, CancellationToken ct = default)
    {
        _appRuntimeModeService.EnsureWriteAllowed();
        var game = await GetGameAsync(id, ct);
        if (game.IsSystemFallback)
        {
            throw new InvalidOperationException("The system fallback game cannot be deactivated.");
        }

        game.IsActive = false;
        await _context.SaveChangesAsync(ct);
    }

    public async Task ReactivateAsync(int id, CancellationToken ct = default)
    {
        _appRuntimeModeService.EnsureWriteAllowed();
        var game = await GetGameAsync(id, ct);
        game.IsActive = true;
        await _context.SaveChangesAsync(ct);
    }

    private async Task<Game?> FindExistingByNameAsync(string name, CancellationToken ct)
    {
        var translations = await _context.Translations
            .Where(t => t.EntityType == TranslationEntityType.Game)
            .ToListAsync(ct);

        var matchingIds = translations
            .Where(t => string.Equals(t.Text, name, StringComparison.OrdinalIgnoreCase))
            .Select(t => t.EntityId)
            .Distinct()
            .ToList();

        if (matchingIds.Count == 0)
        {
            return null;
        }

        return await _context.Games.FirstOrDefaultAsync(g => matchingIds.Contains(g.Id), ct);
    }

    private async Task<Game> GetGameAsync(int id, CancellationToken ct) =>
        await _context.Games.FirstOrDefaultAsync(g => g.Id == id, ct)
        ?? throw new InvalidOperationException($"Game {id} not found.");

    private static GameDto MapGameDto(Game game, IReadOnlyDictionary<int, TranslationTextsDto> translations)
    {
        translations.TryGetValue(game.Id, out var text);
        return new GameDto
        {
            Id = game.Id,
            DisplayText = text?.CurrentText ?? text?.EnglishText ?? string.Empty,
            EnglishText = text?.EnglishText ?? string.Empty,
            GermanText = text?.GermanText ?? text?.EnglishText ?? string.Empty,
            IsActive = game.IsActive,
            IsApproved = game.IsApproved,
            IsSystemFallback = game.IsSystemFallback,
            CreatedByUserId = game.CreatedByUserId,
            CreatedAt = game.CreatedAt
        };
    }
}
