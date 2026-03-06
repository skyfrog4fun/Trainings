using Trainings.Application.DTOs;
using Trainings.Application.Interfaces;
using Trainings.Domain.Entities;
using Trainings.Domain.Enums;
using Trainings.Domain.Interfaces;

namespace Trainings.Application.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;

    public UserService(IUserRepository userRepository, IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
    }

    public async Task<UserDto?> GetByIdAsync(int id)
    {
        var user = await _userRepository.GetByIdAsync(id);
        return user == null ? null : MapToDto(user);
    }

    public async Task<UserDto?> GetByEmailAsync(string email)
    {
        var user = await _userRepository.GetByEmailAsync(email);
        return user == null ? null : MapToDto(user);
    }

    public async Task<IEnumerable<UserDto>> GetAllAsync()
    {
        var users = await _userRepository.GetAllAsync();
        return users.Select(MapToDto);
    }

    public async Task<IEnumerable<UserDto>> GetByRoleAsync(UserRole role)
    {
        var users = await _userRepository.GetByRoleAsync(role);
        return users.Select(MapToDto);
    }

    public async Task<UserDto> CreateAsync(CreateUserDto dto)
    {
        var user = new User
        {
            Name = dto.Name,
            Email = dto.Email,
            PasswordHash = _passwordHasher.Hash(dto.Password),
            Role = dto.Role,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        await _userRepository.AddAsync(user);
        return MapToDto(user);
    }

    public async Task UpdateAsync(UpdateUserDto dto)
    {
        var user = await _userRepository.GetByIdAsync(dto.Id)
            ?? throw new InvalidOperationException($"User {dto.Id} not found.");
        user.Name = dto.Name;
        user.Email = dto.Email;
        user.Role = dto.Role;
        user.IsActive = dto.IsActive;
        await _userRepository.UpdateAsync(user);
    }

    public async Task DeleteAsync(int id)
    {
        await _userRepository.DeleteAsync(id);
    }

    public async Task<bool> ValidatePasswordAsync(string email, string password)
    {
        var user = await _userRepository.GetByEmailAsync(email);
        if (user == null || !user.IsActive) return false;
        return _passwordHasher.Verify(password, user.PasswordHash);
    }

    private static UserDto MapToDto(User user) => new()
    {
        Id = user.Id,
        Name = user.Name,
        Email = user.Email,
        Role = user.Role,
        IsActive = user.IsActive,
        CreatedAt = user.CreatedAt
    };
}
