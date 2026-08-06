using System.Globalization;

namespace Trainings.Web.Services;

public static class PreferenceNavigationService
{
    public static string GetCurrentLanguageCode()
    {
        return CulturePreferenceService.NormalizeCulture(CultureInfo.CurrentUICulture.TwoLetterISOLanguageName);
    }

    public static string BuildReturnUrlFromRequest(HttpRequest request, string fallback = "/")
    {
        var returnUrl = string.IsNullOrWhiteSpace(request.Path.Value)
            ? fallback
            : request.Path.Value!;

        if (request.QueryString.HasValue)
        {
            returnUrl += request.QueryString.Value;
        }

        return returnUrl;
    }

    public static string BuildReturnUrlFromRelativePath(string? relativeUri)
    {
        if (string.IsNullOrWhiteSpace(relativeUri))
        {
            return "/";
        }

        return relativeUri.StartsWith('/')
            ? relativeUri
            : "/" + relativeUri;
    }

    public static string BuildCultureUrl(string culture, string returnUrl)
    {
        var normalizedCulture = CulturePreferenceService.NormalizeCulture(culture);
        return $"/culture/set?culture={normalizedCulture}&returnUrl={Uri.EscapeDataString(returnUrl)}";
    }

    public static string BuildThemeToggleUrl(string? currentTheme, string returnUrl)
    {
        var normalizedTheme = ThemeService.NormalizeTheme(currentTheme);
        var targetTheme = normalizedTheme == "dark" ? "light" : "dark";

        return $"/theme/set?value={targetTheme}&returnUrl={Uri.EscapeDataString(returnUrl)}";
    }
}
