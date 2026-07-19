using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Localization;
using System.Globalization;
using Trainings.Application;
using Trainings.Infrastructure;
using Trainings.Infrastructure.Data;
using Trainings.Web.Auth;
using Trainings.Web.Components;
using Trainings.Web.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddRazorPages();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ThemeService>();

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplication();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.LogoutPath = "/logout";
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

app.MapGet("/theme/set", (HttpContext httpContext, string value, string? returnUrl) =>
{
    httpContext.Response.Cookies.Append(
        ThemeService.ThemeCookieName,
        ThemeService.NormalizeTheme(value),
        ThemeService.CreateCookieOptions(httpContext.Request.IsHttps));

    var target = string.IsNullOrWhiteSpace(returnUrl) ? "/" : returnUrl;
    if (!Uri.IsWellFormedUriString(target, UriKind.Relative))
    {
        target = "/";
    }

    return Results.LocalRedirect(target);
});

app.MapGet("/culture/set", (HttpContext httpContext, string culture, string? returnUrl) =>
{
    var normalizedCulture = string.Equals(culture, "en", StringComparison.OrdinalIgnoreCase) ? "en" : "de";

    httpContext.Response.Cookies.Append(
        CookieRequestCultureProvider.DefaultCookieName,
        CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(normalizedCulture)),
        ThemeService.CreateCookieOptions(httpContext.Request.IsHttps));

    var target = string.IsNullOrWhiteSpace(returnUrl) ? "/" : returnUrl;
    if (!Uri.IsWellFormedUriString(target, UriKind.Relative))
    {
        target = "/";
    }

    return Results.LocalRedirect(target);
});

app.MapStaticAssets();
app.MapRazorPages();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

public partial class Program
{
    private static readonly Action<ILogger, Exception?> LogStartupFailed =
        LoggerMessage.Define(LogLevel.Critical, new EventId(1, nameof(LogStartupFailed)),
            "Application startup failed during database initialization");
}

