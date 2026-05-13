namespace Trainings.Application.DTOs;

public sealed class EmailPreviewDto
{
    public string RecipientEmail { get; init; } = string.Empty;
    public string Subject { get; init; } = string.Empty;
    public string HtmlBody { get; init; } = string.Empty;
}
