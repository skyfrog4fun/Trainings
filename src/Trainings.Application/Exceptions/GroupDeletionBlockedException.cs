namespace Trainings.Application.Exceptions;

/// <summary>
/// Reasons why a <c>Group</c> cannot be deleted.
/// </summary>
public enum GroupDeletionBlockReason
{
    HasActiveMembers,
    HasScheduledTrainings,
    Both
}

/// <summary>
/// Thrown by <c>IGroupService.DeleteAsync</c> when a group still has approved/active members
/// and/or future scheduled trainings. Carries the structured <see cref="GroupDeletionBlockReason"/>
/// so callers (e.g. the Web layer) can localize the failure instead of relying on
/// <see cref="Exception.Message"/>, which is always English.
/// </summary>
public class GroupDeletionBlockedException : InvalidOperationException
{
    public GroupDeletionBlockReason Reason { get; }

    public GroupDeletionBlockedException(GroupDeletionBlockReason reason, string message) : base(message)
    {
        Reason = reason;
    }
}
