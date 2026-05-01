using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using MimeKit;
using Trainings.Application.Interfaces;
using Trainings.Domain.Entities;
using Trainings.Domain.Enums;

namespace Trainings.Infrastructure.Services;

public partial class SmtpEmailService : IEmailService
{
    private readonly IMailConfigurationService _mailConfigService;
    private readonly INotificationLogService _notificationLogService;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(
        IMailConfigurationService mailConfigService,
        INotificationLogService notificationLogService,
        ILogger<SmtpEmailService> logger)
    {
        _mailConfigService = mailConfigService;
        _notificationLogService = notificationLogService;
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
        await SendWithFallbackAsync(toEmail, subject, body, NotificationAction.PasswordReset, null, null, ct);
    }

    public async Task SendEmailConfirmationAsync(string toEmail, string confirmLink, CancellationToken ct = default)
    {
        var subject = "Confirm Your Email Address";
        var body = $"""
            <p>Thank you for registering! Please confirm your email address by clicking the link below:</p>
            <p><a href="{confirmLink}">{confirmLink}</a></p>
            <p>This link expires in 24 hours.</p>
            """;
        await SendWithFallbackAsync(toEmail, subject, body, NotificationAction.EmailConfirmation, null, null, ct);
    }

    public async Task SendAdminNewParticipantNotificationAsync(string adminEmail, string userName, CancellationToken ct = default)
    {
        var subject = "New Participant Registration Pending Approval";
        var body = $"""
            <p>A new participant has registered and is pending approval:</p>
            <p><strong>{userName}</strong></p>
            <p>Please review and approve or reject the registration in the admin panel.</p>
            """;
        await SendWithFallbackAsync(adminEmail, subject, body, NotificationAction.Registration, null, null, ct);
    }

    public async Task SendTestEmailAsync(string toEmail, CancellationToken ct = default)
    {
        var subject = "Test Email – SMTP Configuration Check";
        var body = """
            <p>This is a test email sent from the Trainings application.</p>
            <p>If you received this message, your SMTP configuration is working correctly.</p>
            """;
        await SendWithFallbackAsync(toEmail, subject, body, NotificationAction.TestEmail, null, null, ct);
    }

    public async Task SendWelcomeWithPasswordResetAsync(string toEmail, string resetLink, CancellationToken ct = default)
    {
        var subject = "Welcome to Trainings – Set Your Password";
        var body = $"""
            <p>Welcome to the Trainings application! Your account has been created.</p>
            <p>Please click the link below to set your password and get started:</p>
            <p><a href="{resetLink}">{resetLink}</a></p>
            <p>This link expires in 1 hour. If you did not expect this email, please ignore it.</p>
            """;
        await SendWithFallbackAsync(toEmail, subject, body, NotificationAction.WelcomeMail, null, null, ct);
    }

    private async Task SendWithFallbackAsync(string toEmail, string subject, string htmlBody, NotificationAction action, int? userId, int? groupId, CancellationToken ct)
    {
        var configs = await _mailConfigService.GetActiveConfigsForGroupAsync(groupId, ct);

        if (configs.Count == 0)
        {
            LogSmtpNotConfigured(_logger, subject);
            await _notificationLogService.LogAsync(action, toEmail, userId, null, groupId, false, "No active mail configurations available.", ct);
            return;
        }

        foreach (var config in configs)
        {
            try
            {
                await SendViaConfigAsync(config, toEmail, subject, htmlBody, ct);
                await _notificationLogService.LogAsync(action, toEmail, userId, config.Id, groupId, true, null, ct);
                return;
            }
            catch (Exception ex)
            {
                var errorMessage = BuildExceptionMessage(ex);
                LogSmtpError(_logger, config.Host, config.Port, toEmail, subject, errorMessage, ex);
                await _notificationLogService.LogAsync(action, toEmail, userId, config.Id, groupId, false, errorMessage, ct);
            }
        }
    }

    private async Task SendViaConfigAsync(MailConfiguration config, string toEmail, string subject, string htmlBody, CancellationToken ct)
    {
        LogSmtpSending(_logger, config.Host, config.Port, config.FromAddress, toEmail, subject);

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("Trainings App", config.FromAddress));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = subject;
        message.Body = new TextPart("html") { Text = htmlBody };

        using var client = new SmtpClient();
        await client.ConnectAsync(config.Host, config.Port, SecureSocketOptions.Auto, ct);

        if (!string.IsNullOrEmpty(config.Username) && !string.IsNullOrEmpty(config.Password))
        {
            await client.AuthenticateAsync(config.Username, config.Password, ct);
        }

        try
        {
            await client.SendAsync(message, ct);
            LogSmtpSent(_logger, toEmail, subject);
        }
        finally
        {
            if (client.IsConnected)
            {
                await client.DisconnectAsync(true, CancellationToken.None);
            }
        }
    }

    private static string BuildExceptionMessage(Exception ex)
    {
        var messages = new System.Text.StringBuilder();
        var current = ex;
        while (current != null)
        {
            if (messages.Length > 0)
            {
                messages.Append(" → ");
            }
            messages.Append(current.Message);
            current = current.InnerException;
        }
        return messages.ToString();
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "SMTP not configured (no active mail configurations). Email notification skipped for subject: {Subject}. Configure at least one mail configuration in the admin panel.")]
    private static partial void LogSmtpNotConfigured(ILogger logger, string subject);

    [LoggerMessage(Level = LogLevel.Information, Message = "Sending email via {Host}:{Port} from {From} to {To}, subject: {Subject}")]
    private static partial void LogSmtpSending(ILogger logger, string host, int port, string from, string to, string subject);

    [LoggerMessage(Level = LogLevel.Information, Message = "Email sent successfully to {To}, subject: {Subject}")]
    private static partial void LogSmtpSent(ILogger logger, string to, string subject);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to send email via {Host}:{Port} to {To}, subject: {Subject}. Error: {Error}")]
    private static partial void LogSmtpError(ILogger logger, string host, int port, string to, string subject, string error, Exception ex);
}
