using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using Trainings.Application;
using Trainings.Infrastructure;
using Trainings.Infrastructure.Data;
using Trainings.Web.Auth;
using Trainings.Web.Components;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddRazorPages();

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
        policy.RequireClaim("SuperAdmin", "true"));

    options.AddPolicy("GroupAdmin", policy =>
        policy.RequireAssertion(context =>
            context.User.HasClaim("SuperAdmin", "true") ||
            context.User.Claims.Any(c =>
                c.Type.StartsWith("GroupRole::", StringComparison.Ordinal) &&
                c.Value == "Admin")));

    options.AddPolicy("GroupTrainer", policy =>
        policy.RequireAssertion(context =>
            context.User.HasClaim("SuperAdmin", "true") ||
            context.User.Claims.Any(c =>
                c.Type.StartsWith("GroupRole::", StringComparison.Ordinal) &&
                (c.Value == "Admin" || c.Value == "Trainer"))));

    options.AddPolicy("GroupMember", policy =>
        policy.RequireAssertion(context =>
            context.User.HasClaim("SuperAdmin", "true") ||
            context.User.Claims.Any(c =>
                c.Type.StartsWith("GroupRole::", StringComparison.Ordinal) &&
                (c.Value == "Admin" || c.Value == "Trainer" || c.Value == "Participant"))));

    options.AddPolicy("Authenticated", policy =>
        policy.RequireAuthenticatedUser());
});

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<AuthenticationStateProvider, RevalidatingAuthStateProvider>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Program");
    try
    {
        var seeder = scope.ServiceProvider.GetRequiredService<DbSeeder>();
        await seeder.SeedAsync();
    }
    catch (Exception ex)
    {
        logger.LogCritical(ex, "Application startup failed during database initialization");
        throw;
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
