using System.Globalization;
using System.Net;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using MimeKit;
using Trainings.Application.DTOs;
using Trainings.Application.Interfaces;
using Trainings.Domain.Entities;
using Trainings.Domain.Enums;

namespace Trainings.Infrastructure.Services;

public partial class SmtpEmailService(
    IMailConfigurationService mailConfigService,
    INotificationLogService notificationLogService,
    IAppRuntimeModeService appRuntimeModeService,
    ILogger<SmtpEmailService> logger) : IEmailService
{
    private readonly IMailConfigurationService _mailConfigService = mailConfigService;
    private readonly INotificationLogService _notificationLogService = notificationLogService;
    private readonly IAppRuntimeModeService _appRuntimeModeService = appRuntimeModeService;
    private readonly ILogger<SmtpEmailService> _logger = logger;

    public async Task<EmailSendResult> SendPasswordResetAsync(string toEmail, string resetLink, CancellationToken ct = default)
    {
        string subject = "Password Reset Request";
        string body = $"""
            <p>You requested a password reset. Click the link below to reset your password:</p>
            <p><a href="{resetLink}">{resetLink}</a></p>
            <p>This link expires in 1 hour. If you did not request this, please ignore this email.</p>
            """;
        return await SendWithFallbackAsync(toEmail, subject, body, NotificationAction.PasswordReset, null, null, null, ct);
    }

    public async Task<EmailSendResult> SendEmailConfirmationAsync(string toEmail, string confirmLink, CancellationToken ct = default)
    {
        string subject = "Your Trainings Account";
        string body = $"""
            <p>Your Trainings account is ready.</p>
            <p>Please use the link below to verify your email address and finish the sign-in setup:</p>
            <p><a href="{confirmLink}">{confirmLink}</a></p>
            <p>This link expires in 3 days.</p>
            """;
        return await SendWithFallbackAsync(toEmail, subject, body, NotificationAction.EmailConfirmation, null, null, null, ct);
    }

    public async Task<EmailSendResult> SendAdminNewParticipantNotificationAsync(string adminEmail, string userName, string userEmail, string requestedGroups, string userDetailsLink, CancellationToken ct = default)
    {
        string encodedUserName = WebUtility.HtmlEncode(userName);
        string encodedUserEmail = WebUtility.HtmlEncode(userEmail);
        string encodedRequestedGroups = WebUtility.HtmlEncode(requestedGroups);
        string encodedUserDetailsLink = WebUtility.HtmlEncode(userDetailsLink);
        string subject = "New Participant Registration Pending Approval";
        string body = $"""
            <p>A new participant has registered and is pending approval:</p>
            <ul>
                <li><strong>Name:</strong> {encodedUserName}</li>
                <li><strong>Email:</strong> {encodedUserEmail}</li>
                <li><strong>Requested group(s):</strong> {encodedRequestedGroups}</li>
            </ul>
            <p>Open the user directly:</p>
            <p><a href="{encodedUserDetailsLink}">{encodedUserDetailsLink}</a></p>
            <p>Please review and approve or reject the registration in the admin panel.</p>
            """;
        return await SendWithFallbackAsync(adminEmail, subject, body, NotificationAction.Registration, null, null, null, ct);
    }

    public async Task<EmailSendResult> SendRegistrationApprovedAsync(string toEmail, string appLink, CancellationToken ct = default)
    {
        string encodedLink = WebUtility.HtmlEncode(appLink);
        string subject = "Your registration was approved";
        string body = $"""
            <p>Your registration has been approved.</p>
            <p>You can now sign in and use the Trainings app:</p>
            <p><a href="{encodedLink}">{encodedLink}</a></p>
            """;

        return await SendWithFallbackAsync(toEmail, subject, body, NotificationAction.Registration, null, null, null, ct);
    }

    public async Task<EmailSendResult> SendRegistrationRejectedAsync(string toEmail, string appLink, CancellationToken ct = default)
    {
        string encodedLink = WebUtility.HtmlEncode(appLink);
        string subject = "Your registration was reviewed";
        string body = $"""
            <p>Your registration request was reviewed and is currently not approved.</p>
            <p>If needed, please contact your administrator for details.</p>
            <p>Application link:</p>
            <p><a href="{encodedLink}">{encodedLink}</a></p>
            """;

        return await SendWithFallbackAsync(toEmail, subject, body, NotificationAction.Registration, null, null, null, ct);
    }

    public async Task<EmailSendResult> SendGroupMembershipApprovedAsync(string toEmail, int userId, int groupId, string groupName, string appLink, CancellationToken ct = default)
    {
        string encodedGroupName = WebUtility.HtmlEncode(groupName);
        string encodedLink = WebUtility.HtmlEncode(appLink);
        string subject = $"Your request to join {groupName} was approved";
        string body = $"""
            <p>Your request to join <strong>{encodedGroupName}</strong> has been approved.</p>
            <p>You can now sign in and use the Trainings app:</p>
            <p><a href="{encodedLink}">{encodedLink}</a></p>
            """;

        return await SendWithFallbackAsync(toEmail, subject, body, NotificationAction.GroupApproval, userId, groupId, null, ct);
    }

    public async Task<EmailSendResult> SendGroupMembershipDeclinedAsync(string toEmail, int userId, int groupId, string groupName, string appLink, CancellationToken ct = default)
    {
        string encodedGroupName = WebUtility.HtmlEncode(groupName);
        string encodedLink = WebUtility.HtmlEncode(appLink);
        string subject = $"Your request to join {groupName} was reviewed";
        string body = $"""
            <p>Your request to join <strong>{encodedGroupName}</strong> was reviewed and is currently not approved.</p>
            <p>If needed, please contact your administrator for details.</p>
            <p>Application link:</p>
            <p><a href="{encodedLink}">{encodedLink}</a></p>
            """;

        return await SendWithFallbackAsync(toEmail, subject, body, NotificationAction.GroupRejection, userId, groupId, null, ct);
    }

    public async Task<EmailSendResult> SendTrainingCancellationAsync(string toEmail, string trainingTitle, DateTime trainingDateTime, string appLink, CancellationToken ct = default)
    {
        string encodedTitle = WebUtility.HtmlEncode(trainingTitle);
        string encodedDateTime = WebUtility.HtmlEncode(trainingDateTime.ToString("f", CultureInfo.InvariantCulture));
        string encodedLink = WebUtility.HtmlEncode(appLink);
        string subject = $"Training Cancelled: {trainingTitle}";
        string body = $"""
            <p>The following training has been cancelled and removed:</p>
            <ul>
                <li><strong>Title:</strong> {encodedTitle}</li>
                <li><strong>Date and time:</strong> {encodedDateTime}</li>
            </ul>
            <p>Please use the application to find alternative trainings:</p>
            <p><a href="{encodedLink}">{encodedLink}</a></p>
            """;

        return await SendWithFallbackAsync(toEmail, subject, body, NotificationAction.TrainingCancellation, null, null, null, ct);
    }

    public async Task<EmailSendResult> SendTestEmailAsync(string toEmail, int? mailConfigurationId = null, CancellationToken ct = default)
    {
        string subject = "Test Email – SMTP Configuration Check";
        string body = """
            <p>This is a test email sent from the Trainings application.</p>
            <p>If you received this message, your SMTP configuration is working correctly.</p>
            """;
        return await SendWithFallbackAsync(toEmail, subject, body, NotificationAction.TestEmail, null, null, mailConfigurationId, ct);
    }

    private async Task<EmailSendResult> SendWithFallbackAsync(
        string toEmail,
        string subject,
        string htmlBody,
        NotificationAction action,
        int? userId,
        int? groupId,
        int? mailConfigurationId,
        CancellationToken ct)
    {
        var runtimeMode = _appRuntimeModeService.GetCurrent();
        var preview = new EmailPreviewDto
        {
            RecipientEmail = toEmail,
            Subject = subject,
            HtmlBody = htmlBody
        };

        if (runtimeMode.IsEmailSuppressed)
        {
            var previewAttemptId = Guid.NewGuid();
            string message = runtimeMode.IsReadOnly
                ? "Email delivery skipped because the application is running in Read Only mode."
                : "Email delivery skipped because the application is running in No E-Mail mode.";

            await _notificationLogService.LogAsync(action, toEmail, userId, null, groupId, true, message, previewAttemptId, ct);

            return new EmailSendResult
            {
                IsSuccess = true,
                Preview = preview,
                Attempts =
                [
                    new EmailSendAttemptResult
                    {
                        MailConfigurationId = 0,
                        ConfigurationName = "Preview Only",
                        IsActive = false,
                        IsSuccess = true,
                        Message = message
                    }
                ]
            };
        }

        var configs = await GetConfigsAsync(groupId, mailConfigurationId, action == NotificationAction.TestEmail, ct);
        var attemptId = Guid.NewGuid();
        var attempts = new List<EmailSendAttemptResult>();

        if (configs.Count == 0)
        {
            LogSmtpNotConfigured(_logger, subject);
            string message = action == NotificationAction.TestEmail && mailConfigurationId.HasValue
                ? "The selected mail configuration could not be found."
                : "No mail configurations available.";
            await _notificationLogService.LogAsync(action, toEmail, userId, mailConfigurationId, groupId, false, message, attemptId, ct);
            return new EmailSendResult
            {
                IsSuccess = false,
                Attempts = attempts,
                Preview = preview
            };
        }

        foreach (var config in configs)
        {
            try
            {
                await SendViaConfigAsync(config, toEmail, subject, htmlBody, ct);
                await _mailConfigService.RecordSuccessfulSendAsync(config.Id, DateTime.UtcNow, ct);
                await _notificationLogService.LogAsync(action, toEmail, userId, config.Id, groupId, true, null, attemptId, ct);
                attempts.Add(new EmailSendAttemptResult
                {
                    MailConfigurationId = config.Id,
                    ConfigurationName = config.Name,
                    IsActive = config.IsActive,
                    IsSuccess = true,
                    Message = $"Sent successfully via {config.Name}."
                });

                return new EmailSendResult
                {
                    IsSuccess = true,
                    Attempts = attempts,
                    Preview = preview
                };
            }
            catch (Exception ex)
            {
                string errorMessage = BuildExceptionMessage(ex);
                LogSmtpError(_logger, config.Host, config.Port, toEmail, subject, errorMessage, ex);
                await _mailConfigService.RecordFailedSendAsync(config.Id, errorMessage, ct);
                await _notificationLogService.LogAsync(action, toEmail, userId, config.Id, groupId, false, errorMessage, attemptId, ct);
                attempts.Add(new EmailSendAttemptResult
                {
                    MailConfigurationId = config.Id,
                    ConfigurationName = config.Name,
                    IsActive = config.IsActive,
                    IsSuccess = false,
                    Message = $"{config.Name}: {errorMessage}"
                });
            }
        }

        return new EmailSendResult
        {
            IsSuccess = false,
            Attempts = attempts,
            Preview = preview
        };
    }

    protected virtual async Task SendViaConfigAsync(MailConfiguration config, string toEmail, string subject, string htmlBody, CancellationToken ct)
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

    private async Task<IReadOnlyList<MailConfiguration>> GetConfigsAsync(int? groupId, int? mailConfigurationId, bool includeInactiveForTest, CancellationToken ct)
    {
        if (mailConfigurationId.HasValue)
        {
            var selectedConfig = await _mailConfigService.GetByIdAsync(mailConfigurationId.Value, ct);
            return selectedConfig is null ? [] : [selectedConfig];
        }

        if (includeInactiveForTest)
        {
            return await _mailConfigService.GetAllAsync(ct);
        }

        return await _mailConfigService.GetActiveConfigsForGroupAsync(groupId, ct);
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
