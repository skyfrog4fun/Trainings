using Microsoft.AspNetCore.Localization;

namespace Trainings.Web.Services;

public static class CulturePreferenceService
{
    public static string NormalizeCulture(string? culture)
    {
        return string.Equals(culture, "de", StringComparison.OrdinalIgnoreCase) ? "de" : "en";
    }

    public static void AppendCultureCookie(HttpResponse response, HttpRequest request, string? culture)
    {
        response.Cookies.Append(
            CookieRequestCultureProvider.DefaultCookieName,
            CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(NormalizeCulture(culture))),
            ThemeService.CreateCookieOptions(request.IsHttps));
    }
}
