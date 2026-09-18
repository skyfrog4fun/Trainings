using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Trainings.Application.DTOs;
using Trainings.Application.Interfaces;
using Trainings.Domain.Entities;
using Trainings.Domain.Enums;
using Trainings.Infrastructure.Data;

namespace Trainings.Infrastructure.Services;

public class TranslationService(ApplicationDbContext context) : ITranslationService
{
    private readonly ApplicationDbContext _context = context;

    public string GetCurrentCulture()
    {
        string culture = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        return NormalizeCulture(culture);
    }

    public async Task<IReadOnlyDictionary<int, TranslationTextsDto>> GetTextLookupAsync(
        TranslationEntityType entityType,
        IEnumerable<int> entityIds,
        CancellationToken ct = default)
    {
        var ids = entityIds.Distinct().ToList();
        if (ids.Count == 0)
        {
            return new Dictionary<int, TranslationTextsDto>();
        }

        var translations = await _context.Translations
            .Where(t => t.EntityType == entityType && ids.Contains(t.EntityId))
            .ToListAsync(ct);

        string currentCulture = GetCurrentCulture();
        return translations
            .GroupBy(t => t.EntityId)
            .ToDictionary(
                group => group.Key,
                group =>
                {
                    var englishText = group.FirstOrDefault(t => t.Culture == "en")?.Text ?? string.Empty;
                    var germanText = group.FirstOrDefault(t => t.Culture == "de")?.Text ?? englishText;
                    var currentText = currentCulture == "de"
                        ? (string.IsNullOrWhiteSpace(germanText) ? englishText : germanText)
                        : (string.IsNullOrWhiteSpace(englishText) ? germanText : englishText);

                    return new TranslationTextsDto
                    {
                        EnglishText = englishText,
                        GermanText = germanText,
                        CurrentText = currentText
                    };
                });
    }

    public async Task UpsertAsync(TranslationEntityType entityType, int entityId, string englishText, string germanText, CancellationToken ct = default)
    {
        await UpsertCultureAsync(entityType, entityId, "en", englishText, ct);
        await UpsertCultureAsync(entityType, entityId, "de", germanText, ct);
        await _context.SaveChangesAsync(ct);
    }

    private async Task UpsertCultureAsync(TranslationEntityType entityType, int entityId, string culture, string text, CancellationToken ct)
    {
        string normalizedCulture = NormalizeCulture(culture);
        var translation = await _context.Translations.FirstOrDefaultAsync(
            t => t.EntityType == entityType && t.EntityId == entityId && t.Culture == normalizedCulture,
            ct);

        if (translation == null)
        {
            translation = new Translation
            {
                EntityType = entityType,
                EntityId = entityId,
                Culture = normalizedCulture,
                Text = text.Trim()
            };
            _context.Translations.Add(translation);
            return;
        }

        translation.Text = text.Trim();
    }

    private static string NormalizeCulture(string? culture)
    {
        if (string.IsNullOrWhiteSpace(culture))
        {
            return "en";
        }

        return culture.StartsWith("de", StringComparison.OrdinalIgnoreCase) ? "de" : "en";
    }
}
