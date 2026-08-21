using Trainings.Application.DTOs;
using Trainings.Application.Interfaces;
using Trainings.Domain.Entities;
using Trainings.Domain.Enums;
using Trainings.Domain.Interfaces;

namespace Trainings.Application.Services;

public class UserService(
    IUserRepository userRepository,
    IAppRuntimeModeService appRuntimeModeService,
    IPasswordHasher passwordHasher,
    ICurrentUserContext currentUserContext) : IUserService
{
    private readonly IUserRepository _userRepository = userRepository;
    private readonly IAppRuntimeModeService _appRuntimeModeService = appRuntimeModeService;
    private readonly IPasswordHasher _passwordHasher = passwordHasher;
    private readonly ICurrentUserContext _currentUserContext = currentUserContext;

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
        _appRuntimeModeService.EnsureWriteAllowed();

        var user = new User
        {
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            Email = dto.Email,
            PasswordHash = _passwordHasher.Hash(dto.Password),
            Role = dto.Role,
            Gender = dto.Gender,
            Birthday = dto.Birthday,
            Mobile = dto.Mobile,
            City = dto.City,
            CountryId = dto.CountryId,
            IsActive = true,
            CreationDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
        await _userRepository.AddAsync(user);
        return MapToDto(user);
    }

    public async Task UpdateAsync(UpdateUserDto dto)
    {
        _appRuntimeModeService.EnsureWriteAllowed();

        var user = await _userRepository.GetByIdAsync(dto.Id)
            ?? throw new InvalidOperationException($"User {dto.Id} not found.");
        user.FirstName = dto.FirstName;
        user.LastName = dto.LastName;
        user.Email = dto.Email;
        user.Role = dto.Role;
        user.IsActive = dto.IsActive;
        user.Gender = dto.Gender;
        user.Birthday = dto.Birthday;
        user.Mobile = dto.Mobile;
        user.City = dto.City;
        user.CountryId = dto.CountryId;
        user.EntryDate = dto.EntryDate;
        user.WelcomeMessage = dto.WelcomeMessage;
        await _userRepository.UpdateAsync(user);
    }

    public async Task UpdateSelfAsync(UpdateSelfUserDto dto)
    {
        _appRuntimeModeService.EnsureWriteAllowed();

        int? userId = _currentUserContext.GetCurrentUserId();
        if (!userId.HasValue)
        {
            throw new InvalidOperationException("Authenticated user context is required.");
        }

        var user = await _userRepository.GetByIdAsync(userId.Value)
            ?? throw new InvalidOperationException($"User {userId.Value} not found.");

        user.FirstName = dto.FirstName;
        user.LastName = dto.LastName;
        user.Email = dto.Email;
        user.Gender = dto.Gender;
        user.Birthday = dto.Birthday;
        user.Mobile = dto.Mobile;
        user.City = dto.City;
        user.CountryId = dto.CountryId;
        user.WelcomeMessage = dto.WelcomeMessage;

        await _userRepository.UpdateAsync(user);
    }

    public async Task UpdatePreferencesAsync(int userId, string? language, string? theme)
    {
        _appRuntimeModeService.EnsureWriteAllowed();

        var user = await _userRepository.GetByIdAsync(userId)
            ?? throw new InvalidOperationException($"User {userId} not found.");

        user.Language = language;
        user.Theme = theme;

        await _userRepository.UpdateAsync(user);
    }

    public async Task DeleteAsync(int id)
    {
        _appRuntimeModeService.EnsureWriteAllowed();
        await _userRepository.DeleteAsync(id);
    }

    public async Task ChangePasswordAsync(int userId, string newPassword)
    {
        _appRuntimeModeService.EnsureWriteAllowed();
        var user = await _userRepository.GetByIdAsync(userId)
            ?? throw new InvalidOperationException($"User {userId} not found.");
        user.PasswordHash = _passwordHasher.Hash(newPassword);
        await _userRepository.UpdateAsync(user);
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
        FirstName = user.FirstName,
        LastName = user.LastName,
        Email = user.Email,
        Role = user.Role,
        IsActive = user.IsActive,
        Gender = user.Gender,
        Birthday = user.Birthday,
        Mobile = user.Mobile,
        City = user.City,
        CountryId = user.CountryId,
        CountryCode = user.Country?.Code,
        EmailConfirmedAt = user.EmailConfirmedAt,
        CreationDate = user.CreationDate,
        EntryDate = user.EntryDate,
        WelcomeMessage = user.WelcomeMessage,
        Language = user.Language,
        Theme = user.Theme,
        CreatedAt = user.CreatedAt
    };
}
