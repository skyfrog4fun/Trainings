using System.Globalization;
using System.Reflection;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Trainings.Application.Interfaces;
using Trainings.Domain.Enums;
using Trainings.Web.Auth;

namespace Trainings.Web.Pages;

public class LoginModel : PageModel
{
    private readonly IUserService _userService;
    private readonly IGroupService _groupService;
    private readonly IConfiguration _configuration;

    public LoginModel(IUserService userService, IGroupService groupService, IConfiguration configuration)
    {
        _userService = userService;
        _groupService = groupService;
        _configuration = configuration;
    }

    [BindProperty]
    public string Email { get; set; } = string.Empty;

    [BindProperty]
    public string Password { get; set; } = string.Empty;

    public string ErrorMessage { get; set; } = string.Empty;

    public string AppVersion { get; } =
        Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion
            .Split('+')[0]
        ?? Assembly.GetExecutingAssembly().GetName().Version?.ToString(3)
        ?? "1.0.0";

    public bool ShowInitialCredentials { get; private set; }
    public string SeedEmail { get; private set; } = string.Empty;

    public async Task<IActionResult> OnGetAsync()
    {
        if (User.Identity?.IsAuthenticated == true)
            return Redirect("/");

        var users = await _userService.GetAllAsync();
        if (!users.Any())
        {
            ShowInitialCredentials = true;
            SeedEmail = _configuration["Seed:Email"] ?? "superadmin@trainings.app";
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!await _userService.ValidatePasswordAsync(Email, Password))
        {
            ErrorMessage = "Invalid email or password.";
            return Page();
        }

        var user = await _userService.GetByEmailAsync(Email);
        if (user == null)
        {
            ErrorMessage = "Invalid email or password.";
            return Page();
        }

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString(CultureInfo.InvariantCulture)),
            new Claim(ClaimTypes.Name, user.DisplayName),
            new Claim(ClaimTypes.Email, user.Email)
        };

        if (user.Role == UserRole.SuperAdmin)
        {
            claims.Add(new Claim(AppClaimTypes.SuperAdmin, "true"));
        }

        // Add per-group role claims for all approved memberships
        var memberships = await _groupService.GetApprovedMembershipsForUserAsync(user.Id);
        foreach (var membership in memberships)
        {
            claims.Add(new Claim(AppClaimTypes.GroupRole(membership.GroupId), membership.Role.ToString()));
        }

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

        return Redirect("/");
    }
}
