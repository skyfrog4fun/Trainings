using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http;

namespace Trainings.Web.Services;

public sealed class ThemeService(IHttpContextAccessor httpContextAccessor, NavigationManager navigationManager)
{
    public const string ThemeCookieName = "theme-preference";

    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
    private readonly NavigationManager _navigationManager = navigationManager;

    public string CurrentTheme { get; private set; } = "light";

    public event Action? ThemeChanged;

    public void InitializeFromCookie()
    {
        var cookieValue = _httpContextAccessor.HttpContext?.Request.Cookies[ThemeCookieName];
        CurrentTheme = NormalizeTheme(cookieValue);
    }

    public void ToggleTheme()
    {
        SetTheme(CurrentTheme == "dark" ? "light" : "dark");
    }

    public void SetTheme(string? theme)
    {
        var normalizedTheme = NormalizeTheme(theme);
        if (normalizedTheme == CurrentTheme)
        {
            return;
        }

        CurrentTheme = normalizedTheme;
        ThemeChanged?.Invoke();

        var relativeUri = _navigationManager.ToBaseRelativePath(_navigationManager.Uri);
        if (string.IsNullOrWhiteSpace(relativeUri))
        {
            relativeUri = "/";
        }
        else if (!relativeUri.StartsWith('/'))
        {
            relativeUri = "/" + relativeUri;
        }

        var setThemeUri = $"/theme/set?value={normalizedTheme}&returnUrl={Uri.EscapeDataString(relativeUri)}";
        _navigationManager.NavigateTo(setThemeUri, forceLoad: true);
    }

    public static CookieOptions CreateCookieOptions(bool isHttps)
    {
        return new CookieOptions
        {
            Expires = DateTimeOffset.UtcNow.AddYears(1),
            HttpOnly = false,
            IsEssential = true,
            SameSite = SameSiteMode.Lax,
            Secure = isHttps,
            Path = "/"
        };
    }

    public static string NormalizeTheme(string? theme)
    {
        return string.Equals(theme, "dark", StringComparison.OrdinalIgnoreCase) ? "dark" : "light";
    }
}