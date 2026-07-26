using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Localization;
using System.Globalization;
using System.Security.Claims;
using Trainings.Application;
using Trainings.Application.Interfaces;
using Trainings.Domain.Enums;
using Trainings.Infrastructure;
using Trainings.Infrastructure.Data;
using Trainings.Web.Auth;
using Trainings.Web.Components;
using Trainings.Web.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ThemeService>();

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplication();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.LogoutPath = "/auth/logout";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("SuperAdmin", policy =>
        policy.RequireClaim(AppClaimTypes.SuperAdmin, "true"));

    options.AddPolicy("GroupAdmin", policy =>
        policy.RequireAssertion(context =>
            context.User.HasClaim(AppClaimTypes.SuperAdmin, "true") ||
            context.User.Claims.Any(c =>
                c.Type.StartsWith(AppClaimTypes.GroupRolePrefix, StringComparison.Ordinal) &&
                c.Value == "Admin")));

    options.AddPolicy("GroupTrainer", policy =>
        policy.RequireAssertion(context =>
            context.User.HasClaim(AppClaimTypes.SuperAdmin, "true") ||
            context.User.Claims.Any(c =>
                c.Type.StartsWith(AppClaimTypes.GroupRolePrefix, StringComparison.Ordinal) &&
                (c.Value == "Admin" || c.Value == "Trainer"))));

    options.AddPolicy("GroupMember", policy =>
        policy.RequireAssertion(context =>
            context.User.HasClaim(AppClaimTypes.SuperAdmin, "true") ||
            context.User.Claims.Any(c =>
                c.Type.StartsWith(AppClaimTypes.GroupRolePrefix, StringComparison.Ordinal) &&
                (c.Value == "Admin" || c.Value == "Trainer" || c.Value == "Participant"))));

    options.AddPolicy("Authenticated", policy =>
        policy.RequireAuthenticatedUser());
});

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<AuthenticationStateProvider, RevalidatingAuthStateProvider>();

var app = builder.Build();

var supportedCultures = new[]
{
    new CultureInfo("de"),
    new CultureInfo("en")
};

var requestLocalizationOptions = new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture("de"),
    SupportedCultures = supportedCultures,
    SupportedUICultures = supportedCultures,
    RequestCultureProviders =
    [
        new CookieRequestCultureProvider(),
        new QueryStringRequestCultureProvider(),
        new AcceptLanguageHeaderRequestCultureProvider()
    ]
};

using (var scope = app.Services.CreateScope())
{
    try
    {
        var seeder = scope.ServiceProvider.GetRequiredService<DbSeeder>();
        await seeder.SeedAsync();
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Program");
        LogStartupFailed(logger, ex);
        throw;
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseRequestLocalization(requestLocalizationOptions);
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/theme/set", async (HttpContext httpContext, IUserService userService, string value, string? returnUrl) =>
{
    string normalizedTheme = ThemeService.NormalizeTheme(value);

    httpContext.Response.Cookies.Append(
        ThemeService.ThemeCookieName,
        normalizedTheme,
        ThemeService.CreateCookieOptions(httpContext.Request.IsHttps));

    if (httpContext.User.Identity?.IsAuthenticated == true
        && int.TryParse(httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
    {
        var user = await userService.GetByIdAsync(userId);
        await userService.UpdatePreferencesAsync(userId, user?.Language, normalizedTheme);
    }

    string target = string.IsNullOrWhiteSpace(returnUrl) ? "/" : returnUrl;
    if (!Uri.IsWellFormedUriString(target, UriKind.Relative))
    {
        target = "/";
    }

    return Results.LocalRedirect(target);
});

app.MapGet("/culture/set", async (HttpContext httpContext, IUserService userService, string culture, string? returnUrl) =>
{
    string normalizedCulture = CulturePreferenceService.NormalizeCulture(culture);

    CulturePreferenceService.AppendCultureCookie(httpContext.Response, httpContext.Request, normalizedCulture);

    if (httpContext.User.Identity?.IsAuthenticated == true
        && int.TryParse(httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
    {
        var user = await userService.GetByIdAsync(userId);
        await userService.UpdatePreferencesAsync(userId, normalizedCulture, user?.Theme);
    }

    string target = string.IsNullOrWhiteSpace(returnUrl) ? "/" : returnUrl;
    if (!Uri.IsWellFormedUriString(target, UriKind.Relative))
    {
        target = "/";
    }

    return Results.LocalRedirect(target);
});

app.MapPost("/auth/login", async (HttpContext httpContext, IAntiforgery antiforgery, IUserService userService, IGroupService groupService) =>
{
    await antiforgery.ValidateRequestAsync(httpContext);

    var form = await httpContext.Request.ReadFormAsync();
    string email = form["Email"].ToString();
    string password = form["Password"].ToString();
    string requestedReturnUrl = form["ReturnUrl"].ToString();

    string target = string.IsNullOrWhiteSpace(requestedReturnUrl) ? "/" : requestedReturnUrl;
    if (!Uri.IsWellFormedUriString(target, UriKind.Relative) || target.StartsWith("//", StringComparison.Ordinal))
    {
        target = "/";
    }

    if (!target.StartsWith('/'))
    {
        target = "/" + target;
    }

    if (!await userService.ValidatePasswordAsync(email, password))
    {
        return Results.LocalRedirect(BuildLoginFailureUrl(target, email));
    }

    var user = await userService.GetByEmailAsync(email);
    if (user == null)
    {
        return Results.LocalRedirect(BuildLoginFailureUrl(target, email));
    }

    var claims = new List<Claim>
    {
        new(ClaimTypes.NameIdentifier, user.Id.ToString(CultureInfo.InvariantCulture)),
        new(ClaimTypes.Name, user.DisplayName),
        new(ClaimTypes.Email, user.Email)
    };

    if (user.Role == UserRole.SuperAdmin)
    {
        claims.Add(new Claim(AppClaimTypes.SuperAdmin, "true"));
    }

    var memberships = await groupService.GetApprovedMembershipsForUserAsync(user.Id);
    foreach (var membership in memberships)
    {
        claims.Add(new Claim(AppClaimTypes.GroupRole(membership.GroupId), membership.Role.ToString()));
    }

    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
    var principal = new ClaimsPrincipal(identity);

    await httpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

    var preferredTheme = ThemeService.NormalizeTheme(user.Theme);
    if (string.IsNullOrWhiteSpace(user.Theme))
    {
        var requestTheme = httpContext.Request.Cookies[ThemeService.ThemeCookieName];
        preferredTheme = ThemeService.NormalizeTheme(requestTheme);
    }

    var preferredLanguage = CulturePreferenceService.NormalizeCulture(user.Language);
    if (string.IsNullOrWhiteSpace(user.Language))
    {
        var requestCulture = httpContext.Request.Cookies[".AspNetCore.Culture"];
        if (!string.IsNullOrWhiteSpace(requestCulture))
        {
            var segments = requestCulture.Split('|', StringSplitOptions.RemoveEmptyEntries);
            var uiCultureSegment = segments.FirstOrDefault(s => s.StartsWith("uic=", StringComparison.OrdinalIgnoreCase));
            var cookieCulture = uiCultureSegment?.Substring(4);
            preferredLanguage = CulturePreferenceService.NormalizeCulture(cookieCulture);
        }
        else
        {
            var acceptLanguage = httpContext.Request.Headers["Accept-Language"].ToString();
            var rawLanguage = acceptLanguage.Split(',', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
            var normalizedHeaderLanguage = rawLanguage?.Split('-', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
            preferredLanguage = CulturePreferenceService.NormalizeCulture(normalizedHeaderLanguage);
        }
    }

    httpContext.Response.Cookies.Append(
        ThemeService.ThemeCookieName,
        preferredTheme,
        ThemeService.CreateCookieOptions(httpContext.Request.IsHttps));

    CulturePreferenceService.AppendCultureCookie(httpContext.Response, httpContext.Request, preferredLanguage);

    return Results.LocalRedirect(target);
});

app.MapPost("/auth/logout", async (HttpContext httpContext, IAntiforgery antiforgery) =>
{
    await antiforgery.ValidateRequestAsync(httpContext);
    await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.LocalRedirect("/login");
});

app.MapStaticAssets();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();
app.MapFallback(() => new RazorComponentResult<App>());

app.Run();

public partial class Program
{
    private static string BuildLoginFailureUrl(string returnUrl, string email)
    {
        var encodedReturnUrl = Uri.EscapeDataString(returnUrl);
        var encodedEmail = Uri.EscapeDataString(email);
        return $"/login?error=invalid&returnUrl={encodedReturnUrl}&email={encodedEmail}";
    }

    private static readonly Action<ILogger, Exception?> LogStartupFailed =
        LoggerMessage.Define(LogLevel.Critical, new EventId(1, nameof(LogStartupFailed)),
            "Application startup failed during database initialization");
}

