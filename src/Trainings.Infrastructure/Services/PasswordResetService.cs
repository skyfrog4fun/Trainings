using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Trainings.Application.Interfaces;
using Trainings.Domain.Entities;
using Trainings.Infrastructure.Data;

namespace Trainings.Infrastructure.Services;

public class PasswordResetService : IPasswordResetService
{
    private readonly ApplicationDbContext _context;
    private readonly IEmailService _emailService;
    private readonly IPasswordHasher _passwordHasher;
    private readonly string _baseUrl;

    public PasswordResetService(
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

    public async Task RequestResetAsync(string email, CancellationToken ct = default)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email, ct);
        if (user == null)
        {
            // Don't reveal whether user exists
            return;
        }

        // Invalidate old tokens
        var oldTokens = await _context.PasswordResetTokens
            .Where(t => t.UserId == user.Id && !t.IsUsed)
            .ToListAsync(ct);
        foreach (var old in oldTokens)
        {
            old.IsUsed = true;
        }

        var token = new PasswordResetToken
        {
            UserId = user.Id,
            Token = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N"),
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            IsUsed = false
        };
        _context.PasswordResetTokens.Add(token);
        await _context.SaveChangesAsync(ct);

        var resetLink = $"{_baseUrl}/reset-password?token={token.Token}";
        await _emailService.SendPasswordResetAsync(user.Email, resetLink, ct);
    }

    public async Task ResetPasswordAsync(string token, string newPassword, CancellationToken ct = default)
    {
        var resetToken = await _context.PasswordResetTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.Token == token, ct);

        if (resetToken == null || resetToken.IsUsed || resetToken.ExpiresAt < DateTime.UtcNow)
        {
            throw new InvalidOperationException("Invalid or expired reset token.");
        }

        resetToken.User.PasswordHash = _passwordHasher.Hash(newPassword);
        resetToken.IsUsed = true;
        await _context.SaveChangesAsync(ct);
    }
}
