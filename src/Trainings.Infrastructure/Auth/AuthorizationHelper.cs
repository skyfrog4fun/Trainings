using System.Security.Claims;
using Trainings.Application.Interfaces;

namespace Trainings.Infrastructure.Auth;

public class AuthorizationHelper : IAuthorizationHelper
{
    private const string SuperAdminClaim = "SuperAdmin";
    private const string GroupRoleClaimPrefix = "GroupRole::";

    public bool IsSuperAdmin(ClaimsPrincipal user)
    {
        return user.HasClaim(SuperAdminClaim, "true");
    }

    public bool IsGroupAdmin(ClaimsPrincipal user, int groupId)
    {
        if (IsSuperAdmin(user))
        {
            return true;
        }

        return user.HasClaim($"{GroupRoleClaimPrefix}{groupId}", "Admin");
    }

    public bool IsGroupTrainer(ClaimsPrincipal user, int groupId)
    {
        if (IsGroupAdmin(user, groupId))
        {
            return true;
        }

        return user.HasClaim($"{GroupRoleClaimPrefix}{groupId}", "Trainer");
    }

    public bool IsGroupMember(ClaimsPrincipal user, int groupId)
    {
        if (IsSuperAdmin(user))
        {
            return true;
        }

        return user.Claims.Any(c =>
            c.Type == $"{GroupRoleClaimPrefix}{groupId}" &&
            (c.Value == "Admin" || c.Value == "Trainer" || c.Value == "Participant"));
    }

    public bool HasAnyGroupRole(ClaimsPrincipal user, string role)
    {
        if (IsSuperAdmin(user))
        {
            return true;
        }

        return user.Claims.Any(c =>
            c.Type.StartsWith(GroupRoleClaimPrefix, StringComparison.Ordinal) &&
            c.Value == role);
    }

    public IReadOnlyList<int> GetGroupIdsForRole(ClaimsPrincipal user, string role)
    {
        return user.Claims
            .Where(c =>
                c.Type.StartsWith(GroupRoleClaimPrefix, StringComparison.Ordinal) &&
                c.Value == role)
            .Select(c => int.TryParse(c.Type[GroupRoleClaimPrefix.Length..], out var id) ? id : -1)
            .Where(id => id > 0)
            .ToList();
    }
}
