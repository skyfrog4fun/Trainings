using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Trainings.Application.DTOs;
using Trainings.Application.Interfaces;
using Trainings.Domain.Entities;
using Trainings.Domain.Enums;
using Trainings.Infrastructure.Data;

namespace Trainings.Infrastructure.Services;

public class UserRegistrationService : IUserRegistrationService
{
    private readonly ApplicationDbContext _context;
    private readonly IEmailService _emailService;
    private readonly IAppRuntimeModeService _appRuntimeModeService;
    private readonly IAuthorizationHelper _authorizationHelper;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IPasswordHasher _passwordHasher;
    private readonly string _baseUrl;

    public UserRegistrationService(
        ApplicationDbContext context,
        IEmailService emailService,
        IAppRuntimeModeService appRuntimeModeService,
        IAuthorizationHelper authorizationHelper,
        IHttpContextAccessor httpContextAccessor,
        IPasswordHasher passwordHasher,
        IConfiguration configuration)
    {
        _context = context;
        _emailService = emailService;
        _appRuntimeModeService = appRuntimeModeService;
        _authorizationHelper = authorizationHelper;
        _httpContextAccessor = httpContextAccessor;
        _passwordHasher = passwordHasher;
        _baseUrl = configuration["App:BaseUrl"]?.TrimEnd('/') ?? string.Empty;
    }

    public async Task<RegistrationResultDto> RegisterAsync(RegisterRequestDto dto, CancellationToken ct = default)
    {
        _appRuntimeModeService.EnsureWriteAllowed();

        if (await _context.Users.AnyAsync(u => u.Email == dto.Email, ct))
        {
            throw new InvalidOperationException("An account with this email already exists.");
        }

        var user = new User
        {
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            Email = dto.Email,
            PasswordHash = _passwordHasher.Hash(dto.Password),
            Role = UserRole.User,
            Gender = dto.Gender,
            Birthday = dto.Birthday,
            Mobile = dto.Mobile,
            City = dto.City,
            WelcomeMessage = dto.WelcomeMessage,
            IsActive = true,
            CreationDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync(ct);

        foreach (var groupId in dto.RequestedGroupIds)
        {
            var groupExists = await _context.Groups.AnyAsync(g => g.Id == groupId, ct);
            if (!groupExists)
            {
                continue;
            }

            _context.GroupMemberships.Add(new GroupMembership
            {
                UserId = user.Id,
                GroupId = groupId,
                Role = GroupMemberRole.Participant,
                Status = GroupMembershipStatus.Pending,
                IsActive = false,
                RequestedAt = DateTime.UtcNow,
                JoinedAt = DateTime.UtcNow
            });
        }

        var confirmToken = new EmailConfirmationToken
        {
            UserId = user.Id,
            Token = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N"),
            ExpiresAt = DateTime.UtcNow.AddDays(3),
            IsUsed = false
        };
        _context.EmailConfirmationTokens.Add(confirmToken);
        await _context.SaveChangesAsync(ct);

        var confirmLink = BuildConfirmLink(confirmToken.Token);
        var confirmationEmail = await _emailService.SendEmailConfirmationAsync(user.Email, confirmLink, ct);

        var admins = await _context.Users
            .Where(u => u.Role == UserRole.SuperAdmin)
            .ToListAsync(ct);

        var requestedGroups = await _context.Groups
            .Where(g => dto.RequestedGroupIds.Contains(g.Id))
            .Select(g => g.Name)
            .ToListAsync(ct);
        var requestedGroupsLabel = requestedGroups.Count == 0 ? "No group selected" : string.Join(", ", requestedGroups);
        var userDetailsLink = BuildUserDetailsLink(user.Id);

        foreach (var admin in admins)
        {
            await _emailService.SendAdminNewParticipantNotificationAsync(
                admin.Email,
                user.DisplayName,
                user.Email,
                requestedGroupsLabel,
                userDetailsLink,
                ct);
        }

        return new RegistrationResultDto
        {
            User = MapToDto(user),
            ConfirmationEmail = confirmationEmail
        };
    }

    public async Task<EmailConfirmationResultDto> ConfirmEmailAsync(string token, CancellationToken ct = default)
    {
        var confirmToken = await _context.EmailConfirmationTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.Token == token, ct);

        if (confirmToken == null)
        {
            return new EmailConfirmationResultDto
            {
                Message = "Invalid confirmation token."
            };
        }

        if (confirmToken.IsUsed)
        {
            return new EmailConfirmationResultDto
            {
                Message = "This confirmation link has already been used."
            };
        }

        if (confirmToken.ExpiresAt < DateTime.UtcNow)
        {
            return new EmailConfirmationResultDto
            {
                IsExpired = true,
                UserId = confirmToken.UserId,
                Message = "This confirmation link has expired."
            };
        }

        confirmToken.User.EmailConfirmedAt = DateTime.UtcNow;
        confirmToken.IsUsed = true;
        await _context.SaveChangesAsync(ct);

        return new EmailConfirmationResultDto
        {
            IsSuccess = true,
            UserId = confirmToken.UserId,
            Message = "Your email has been confirmed. Your account is pending admin approval."
        };
    }

    public async Task ApproveUserAsync(int userId, int adminUserId, CancellationToken ct = default)
    {
        _appRuntimeModeService.EnsureWriteAllowed();

        var user = await _context.Users.FindAsync([userId], ct)
            ?? throw new InvalidOperationException($"User {userId} not found.");

        user.EntryDate = DateTime.UtcNow;

        var managedGroupIds = GetManagedGroupIds();
        var pendingMemberships = await _context.GroupMemberships
            .Where(gm =>
                gm.UserId == userId &&
                gm.Status == GroupMembershipStatus.Pending &&
                (managedGroupIds == null || managedGroupIds.Contains(gm.GroupId)))
            .ToListAsync(ct);

        if (pendingMemberships.Count == 0)
        {
            throw new InvalidOperationException("No pending group requests are available for approval.");
        }

        foreach (var membership in pendingMemberships)
        {
            membership.Status = GroupMembershipStatus.Approved;
            membership.ApprovedAt = DateTime.UtcNow;
            membership.IsActive = true;
        }

        await _context.SaveChangesAsync(ct);

        await _emailService.SendRegistrationApprovedAsync(user.Email, BuildAppBaseUrl(), ct);
    }

    public async Task RejectUserAsync(int userId, int adminUserId, CancellationToken ct = default)
    {
        _appRuntimeModeService.EnsureWriteAllowed();

        var user = await _context.Users.FindAsync([userId], ct)
            ?? throw new InvalidOperationException($"User {userId} not found.");

        var managedGroupIds = GetManagedGroupIds();
        var pendingMemberships = await _context.GroupMemberships
            .Where(gm =>
                gm.UserId == userId &&
                gm.Status == GroupMembershipStatus.Pending &&
                (managedGroupIds == null || managedGroupIds.Contains(gm.GroupId)))
            .ToListAsync(ct);

        if (pendingMemberships.Count == 0)
        {
            throw new InvalidOperationException("No pending group requests are available for rejection.");
        }

        foreach (var membership in pendingMemberships)
        {
            membership.Status = GroupMembershipStatus.Declined;
            membership.DeclinedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(ct);

        await _emailService.SendRegistrationRejectedAsync(user.Email, BuildAppBaseUrl(), ct);
    }

    public async Task<IEnumerable<UserDto>> GetPendingApprovalsAsync(CancellationToken ct = default)
    {
        var managedGroupIds = GetManagedGroupIds();
        var userIds = await _context.GroupMemberships
            .Where(gm =>
                gm.Status == GroupMembershipStatus.Pending &&
                (managedGroupIds == null || managedGroupIds.Contains(gm.GroupId)))
            .Select(gm => gm.UserId)
            .Distinct()
            .ToListAsync(ct);

        var users = await _context.Users
            .Where(u => userIds.Contains(u.Id))
            .OrderBy(u => u.CreationDate)
            .ToListAsync(ct);

        return users.Select(MapToDto);
    }

    public async Task<EmailSendResult> ResendEmailConfirmationAsync(int userId, CancellationToken ct = default)
    {
        _appRuntimeModeService.EnsureWriteAllowed();

        var user = await _context.Users.FindAsync([userId], ct)
            ?? throw new InvalidOperationException($"User {userId} not found.");

        var oldTokens = await _context.EmailConfirmationTokens
            .Where(t => t.UserId == userId && !t.IsUsed)
            .ToListAsync(ct);

        foreach (var old in oldTokens)
        {
            old.IsUsed = true;
        }

        var confirmToken = new EmailConfirmationToken
        {
            UserId = user.Id,
            Token = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N"),
            ExpiresAt = DateTime.UtcNow.AddDays(3),
            IsUsed = false
        };
        _context.EmailConfirmationTokens.Add(confirmToken);
        await _context.SaveChangesAsync(ct);

        var confirmLink = BuildConfirmLink(confirmToken.Token);
        return await _emailService.SendEmailConfirmationAsync(user.Email, confirmLink, ct);
    }

    private string BuildConfirmLink(string token)
    {
        var baseUrl = BuildAppBaseUrl();
        return $"{baseUrl}/confirm-email?token={token}";
    }

    private string BuildUserDetailsLink(int userId)
    {
        var baseUrl = BuildAppBaseUrl();
        return $"{baseUrl}/users?editUserId={userId}";
    }

    private string BuildAppBaseUrl()
    {
        var request = _httpContextAccessor.HttpContext?.Request;
        if (request is not null && request.Host.HasValue)
        {
            return $"{request.Scheme}://{request.Host.Value}";
        }

        return _baseUrl;
    }

    private HashSet<int>? GetManagedGroupIds()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated != true || _authorizationHelper.IsSuperAdmin(user))
        {
            return null;
        }

        return _authorizationHelper.GetGroupIdsForRole(user, "Admin").ToHashSet();
    }

    private static UserDto MapToDto(User user) => new()
    {
        Id = user.Id,
        FirstName = user.FirstName,
        LastName = user.LastName,
        Email = user.Email,
        Role = user.Role,
        IsActive = user.IsActive,
        Gender = user.Gender,
        Birthday = user.Birthday,
        Mobile = user.Mobile,
        City = user.City,
        EmailConfirmedAt = user.EmailConfirmedAt,
        CreationDate = user.CreationDate,
        EntryDate = user.EntryDate,
        WelcomeMessage = user.WelcomeMessage,
        CreatedAt = user.CreatedAt
    };
}
