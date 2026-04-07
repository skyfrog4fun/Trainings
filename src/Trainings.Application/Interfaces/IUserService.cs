using Trainings.Application.DTOs;
using Trainings.Domain.Enums;

namespace Trainings.Application.Interfaces;

public interface IUserService
{
    Task<UserDto?> GetByIdAsync(int id);
    Task<UserDto?> GetByEmailAsync(string email);
    Task<IEnumerable<UserDto>> GetAllAsync();
    Task<IEnumerable<UserDto>> GetByRoleAsync(UserRole role);
    Task<UserDto> CreateAsync(CreateUserDto dto);
    Task UpdateAsync(UpdateUserDto dto);
    Task DeleteAsync(int id);
    Task<bool> ValidatePasswordAsync(string email, string password);
}
