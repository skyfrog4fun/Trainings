using Microsoft.Extensions.Localization;

namespace Trainings.Web.Services;

/// <summary>
/// Resolves localized display text for enum values by convention, so any enum can be
/// translated without a dedicated per-enum mapping. Resource keys follow the pattern
/// "Enum_{EnumTypeName}_{Value}" (e.g. "Enum_Gender_Male") in <see cref="SharedResources"/>.
/// </summary>
public static class EnumLocalizer
{
    public static string GetResourceKey(Enum value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return $"Enum_{value.GetType().Name}_{value}";
    }

    public static string Localize(this Enum value, IStringLocalizer<SharedResources> localizer)
    {
        ArgumentNullException.ThrowIfNull(localizer);
        return localizer[GetResourceKey(value)];
    }
}
