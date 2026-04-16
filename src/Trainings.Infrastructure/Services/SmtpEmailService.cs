using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;
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

        LogSmtpSending(_logger, host, port, from, toEmail, subject);

        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("Trainings App", from));
            message.To.Add(MailboxAddress.Parse(toEmail));
            message.Subject = subject;
            message.Body = new TextPart("html") { Text = htmlBody };

            using var client = new SmtpClient();
            await client.ConnectAsync(host, port, SecureSocketOptions.Auto, ct);

            if (!string.IsNullOrEmpty(user) && !string.IsNullOrEmpty(password))
            {
                await client.AuthenticateAsync(user, password, ct);
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
        catch (Exception ex)
        {
            LogSmtpError(_logger, host, port, toEmail, subject, BuildExceptionMessage(ex), ex);
            throw new InvalidOperationException(BuildExceptionMessage(ex), ex);
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

    [LoggerMessage(Level = LogLevel.Warning, Message = "SMTP not configured. Email notification skipped for subject: {Subject}")]
    private static partial void LogSmtpNotConfigured(ILogger logger, string subject);

    [LoggerMessage(Level = LogLevel.Information, Message = "Sending email via {Host}:{Port} from {From} to {To}, subject: {Subject}")]
    private static partial void LogSmtpSending(ILogger logger, string host, int port, string from, string to, string subject);

    [LoggerMessage(Level = LogLevel.Information, Message = "Email sent successfully to {To}, subject: {Subject}")]
    private static partial void LogSmtpSent(ILogger logger, string to, string subject);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to send email via {Host}:{Port} to {To}, subject: {Subject}. Error: {Error}")]
    private static partial void LogSmtpError(ILogger logger, string host, int port, string to, string subject, string error, Exception ex);
}
