using Trainings.Application.DTOs;
using Trainings.Domain.Enums;

namespace Trainings.Application.Interfaces;

public interface IGroupService
{
    Task<IEnumerable<GroupDto>> GetAllAsync(CancellationToken ct = default);
    Task<GroupDto?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<GroupDto> CreateAsync(CreateGroupDto dto, CancellationToken ct = default);
    Task UpdateAsync(UpdateGroupDto dto, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
    Task<IEnumerable<GroupMembershipDto>> GetMembersAsync(int groupId, CancellationToken ct = default);
    Task AddMemberAsync(AddGroupMemberDto dto, CancellationToken ct = default);
    Task RemoveMemberAsync(int membershipId, CancellationToken ct = default);
    Task<IEnumerable<GroupDto>> GetGroupsForUserAsync(int userId, CancellationToken ct = default);
}
