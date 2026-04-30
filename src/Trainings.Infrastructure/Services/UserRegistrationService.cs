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
    private readonly IPasswordHasher _passwordHasher;
    private readonly string _baseUrl;

    public UserRegistrationService(
        ApplicationDbContext context,
        IEmailService emailService,
        IPasswordHasher passwordHasher,
        IConfiguration configuration)
    {
        _context = context;
        _emailService = emailService;
        _passwordHasher = passwordHasher;
        _baseUrl = configuration["App:BaseUrl"]?.TrimEnd('/') ?? string.Empty;
    }

    public async Task<UserDto> RegisterAsync(RegisterRequestDto dto, CancellationToken ct = default)
    {
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

        // Add group membership requests with Pending status
        foreach (var groupId in dto.RequestedGroupIds)
        {
            var groupExists = await _context.Groups.AnyAsync(g => g.Id == groupId, ct);
            if (groupExists)
            {
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
        }

        // Create email confirmation token
        var confirmToken = new EmailConfirmationToken
        {
            UserId = user.Id,
            Token = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N"),
            ExpiresAt = DateTime.UtcNow.AddDays(1),
            IsUsed = false
        };
        _context.EmailConfirmationTokens.Add(confirmToken);
        await _context.SaveChangesAsync(ct);

        var confirmLink = $"{_baseUrl}/confirm-email?token={confirmToken.Token}";
        await _emailService.SendEmailConfirmationAsync(user.Email, confirmLink, ct);

        // Notify admins (SuperAdmins at system level)
        var admins = await _context.Users
            .Where(u => u.Role == UserRole.SuperAdmin)
            .ToListAsync(ct);
        foreach (var admin in admins)
        {
            await _emailService.SendAdminNewParticipantNotificationAsync(
                admin.Email, user.DisplayName, ct);
        }

        return MapToDto(user);
    }

    public async Task ConfirmEmailAsync(string token, CancellationToken ct = default)
    {
        var confirmToken = await _context.EmailConfirmationTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.Token == token, ct);

        if (confirmToken == null || confirmToken.IsUsed || confirmToken.ExpiresAt < DateTime.UtcNow)
        {
            throw new InvalidOperationException("Invalid or expired confirmation token.");
        }

        confirmToken.User.EmailConfirmedAt = DateTime.UtcNow;
        confirmToken.IsUsed = true;
        await _context.SaveChangesAsync(ct);
    }

    public async Task ApproveUserAsync(int userId, int adminUserId, CancellationToken ct = default)
    {
        var user = await _context.Users.FindAsync([userId], ct)
            ?? throw new InvalidOperationException($"User {userId} not found.");

        user.EntryDate = DateTime.UtcNow;

        // Approve pending group membership requests
        var pendingMemberships = await _context.GroupMemberships
            .Where(gm => gm.UserId == userId && gm.Status == GroupMembershipStatus.Pending)
            .ToListAsync(ct);

        foreach (var membership in pendingMemberships)
        {
            membership.Status = GroupMembershipStatus.Approved;
            membership.ApprovedAt = DateTime.UtcNow;
            membership.IsActive = true;
        }

        await _context.SaveChangesAsync(ct);
    }

    public async Task RejectUserAsync(int userId, int adminUserId, CancellationToken ct = default)
    {
        var user = await _context.Users.FindAsync([userId], ct)
            ?? throw new InvalidOperationException($"User {userId} not found.");

        // Decline pending group membership requests
        var pendingMemberships = await _context.GroupMemberships
            .Where(gm => gm.UserId == userId && gm.Status == GroupMembershipStatus.Pending)
            .ToListAsync(ct);

        foreach (var membership in pendingMemberships)
        {
            membership.Status = GroupMembershipStatus.Declined;
            membership.DeclinedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(ct);
    }

    public async Task<IEnumerable<UserDto>> GetPendingApprovalsAsync(CancellationToken ct = default)
    {
        var userIds = await _context.GroupMemberships
            .Where(gm => gm.Status == GroupMembershipStatus.Pending)
            .Select(gm => gm.UserId)
            .Distinct()
            .ToListAsync(ct);

        var users = await _context.Users
            .Where(u => userIds.Contains(u.Id))
            .OrderBy(u => u.CreationDate)
            .ToListAsync(ct);

        return users.Select(MapToDto);
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
