using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Trainings.Application.Interfaces;

namespace Trainings.Infrastructure.Services;

public partial class SmtpEmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(IConfiguration configuration, ILogger<SmtpEmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendPasswordResetAsync(string toEmail, string resetLink, CancellationToken ct = default)
    {
        var subject = "Password Reset Request";
        var body = $"""
            <p>You requested a password reset. Click the link below to reset your password:</p>
            <p><a href="{resetLink}">{resetLink}</a></p>
            <p>This link expires in 1 hour. If you did not request this, please ignore this email.</p>
            """;
        await SendAsync(toEmail, subject, body, ct);
    }

    public async Task SendEmailConfirmationAsync(string toEmail, string confirmLink, CancellationToken ct = default)
    {
        var subject = "Confirm Your Email Address";
        var body = $"""
            <p>Thank you for registering! Please confirm your email address by clicking the link below:</p>
            <p><a href="{confirmLink}">{confirmLink}</a></p>
            <p>This link expires in 24 hours.</p>
            """;
        await SendAsync(toEmail, subject, body, ct);
    }

    public async Task SendAdminNewParticipantNotificationAsync(string adminEmail, string userName, CancellationToken ct = default)
    {
        var subject = "New Participant Registration Pending Approval";
        var body = $"""
            <p>A new participant has registered and is pending approval:</p>
            <p><strong>{userName}</strong></p>
            <p>Please review and approve or reject the registration in the admin panel.</p>
            """;
        await SendAsync(adminEmail, subject, body, ct);
    }

    public async Task SendTestEmailAsync(string toEmail, CancellationToken ct = default)
    {
        var subject = "Test Email – SMTP Configuration Check";
        var body = """
            <p>This is a test email sent from the Trainings application.</p>
            <p>If you received this message, your SMTP configuration is working correctly.</p>
            """;
        await SendAsync(toEmail, subject, body, ct);
    }

    private async Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken ct)
    {
        var smtpSection = _configuration.GetSection("Smtp") ?? throw new InvalidOperationException("SMTP configuration section is missing.");
        var host = smtpSection["Host"] ?? throw new InvalidOperationException("SMTP Host is not configured.");
        var from = smtpSection["From"] ?? smtpSection["User"] ?? throw new InvalidOperationException("SMTP From address is not configured.");

        if (string.IsNullOrWhiteSpace(host))
        {
            LogSmtpNotConfigured(_logger, subject);
            return;
        }

        var port = int.TryParse(smtpSection["Port"], out var p) ? p : 587;
        var user = smtpSection["User"];
        var password = smtpSection["Password"];

        using var client = new SmtpClient(host, port)
        {
            EnableSsl = true,
            UseDefaultCredentials = false,
            Credentials = new NetworkCredential(user, password),
        };

        var message = new MailMessage
        {
            From = new MailAddress(from, "Trainings App"),
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true
        };
        message.To.Add(toEmail);

        await client.SendMailAsync(message, ct);
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "SMTP not configured. Email notification skipped for subject: {Subject}")]
    private static partial void LogSmtpNotConfigured(ILogger logger, string subject);
}
