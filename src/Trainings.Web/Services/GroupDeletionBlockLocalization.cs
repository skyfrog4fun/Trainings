using Trainings.Application.Exceptions;

namespace Trainings.Web.Services;

/// <summary>
/// Maps <see cref="GroupDeletionBlockReason"/> values from <c>IGroupService.DeleteAsync</c> to Web resource keys,
/// keeping localization concerns in the Web layer while the deletion rules themselves live in Trainings.Application.
/// </summary>
public static class GroupDeletionBlockLocalization
{
    public static string GetResourceKey(GroupDeletionBlockReason reason) => reason switch
    {
        GroupDeletionBlockReason.HasActiveMembers => "GroupDetailPage_DeleteBlockedActiveMembers",
        GroupDeletionBlockReason.HasScheduledTrainings => "GroupDetailPage_DeleteBlockedScheduledTrainings",
        GroupDeletionBlockReason.Both => "GroupDetailPage_DeleteBlockedBoth",
        _ => string.Empty
    };
}
