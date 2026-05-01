namespace Trainings.Web.Auth;

/// <summary>Application-specific claim type names used in authentication cookies.</summary>
public static class AppClaimTypes
{
    /// <summary>Claim value is "true" when the user has the SuperAdmin system role.</summary>
    public const string SuperAdmin = "SuperAdmin";

    /// <summary>
    /// Prefix for per-group role claims. Full claim type is "GroupRole::{groupId}".
    /// The value is the user's role in that group (Admin, Trainer, or Participant).
    /// </summary>
    public const string GroupRolePrefix = "GroupRole::";

    /// <summary>Returns the full claim type for a user's role in the specified group.</summary>
    public static string GroupRole(int groupId) => $"{GroupRolePrefix}{groupId}";
}
