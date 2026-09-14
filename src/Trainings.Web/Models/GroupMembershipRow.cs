using Trainings.Domain.Enums;

namespace Trainings.Web.Models;

public class GroupMembershipRow
{
    public string GroupName { get; init; } = string.Empty;
    public GroupMemberRole Role { get; init; }
    public GroupMembershipStatus Status { get; init; }
    public DateTime RequestedAt { get; init; }
    public DateTime? ApprovedAt { get; init; }
    public DateTime? DeclinedAt { get; init; }
}
