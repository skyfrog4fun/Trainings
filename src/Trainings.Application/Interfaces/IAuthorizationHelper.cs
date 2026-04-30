using System.Security.Claims;

namespace Trainings.Application.Interfaces;

public interface IAuthorizationHelper
{
    bool IsSuperAdmin(ClaimsPrincipal user);
    bool IsGroupAdmin(ClaimsPrincipal user, int groupId);
    bool IsGroupTrainer(ClaimsPrincipal user, int groupId);
    bool IsGroupMember(ClaimsPrincipal user, int groupId);
    bool HasAnyGroupRole(ClaimsPrincipal user, string role);
    IReadOnlyList<int> GetGroupIdsForRole(ClaimsPrincipal user, string role);
}
