using Trainings.Application.DTOs;
using Trainings.Domain.Enums;

namespace Trainings.Application.Interfaces;

public interface ITranslationService
{
    string GetCurrentCulture();
    Task<IReadOnlyDictionary<int, TranslationTextsDto>> GetTextLookupAsync(TranslationEntityType entityType, IEnumerable<int> entityIds, CancellationToken ct = default);
    Task UpsertAsync(TranslationEntityType entityType, int entityId, string englishText, string germanText, CancellationToken ct = default);
}
