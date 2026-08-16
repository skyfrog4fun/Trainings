using Trainings.Application.DTOs;
using Trainings.Application.Interfaces;
using Trainings.Domain.Entities;
using Trainings.Domain.Enums;
using Trainings.Domain.Interfaces;

namespace Trainings.Application.Services;

public class RegistrationService(IRegistrationRepository registrationRepository, ITrainingRepository trainingRepository, IAppRuntimeModeService appRuntimeModeService) : IRegistrationService
{
    private readonly IRegistrationRepository _registrationRepository = registrationRepository;
    private readonly ITrainingRepository _trainingRepository = trainingRepository;
    private readonly IAppRuntimeModeService _appRuntimeModeService = appRuntimeModeService;

    public async Task<IEnumerable<RegistrationDto>> GetByUserIdAsync(int userId)
    {
        var registrations = await _registrationRepository.GetByUserIdAsync(userId);
        return registrations.Select(MapToDto);
    }

    public async Task<IEnumerable<RegistrationDto>> GetByTrainingIdAsync(int trainingId)
    {
        var registrations = await _registrationRepository.GetByTrainingIdAsync(trainingId);
        return registrations.Select(MapToDto);
    }

    public async Task<RegistrationDto> RegisterAsync(int userId, int trainingId)
    {
        _appRuntimeModeService.EnsureWriteAllowed();

        var training = await _trainingRepository.GetByIdAsync(trainingId)
            ?? throw new InvalidOperationException("Training not found.");

        var now = DateTime.UtcNow;
        bool isOpen = training.Status == TrainingStatus.Planned
            ? training.DateTime >= now && training.DateTime <= now.AddDays(28)
            : training.DateTime >= now && training.DateTime <= now.AddDays(4);
        if (!isOpen)
        {
            throw new InvalidOperationException("Registration is not open for this training.");
        }

        int activeRegistrations = (await _registrationRepository.GetByTrainingIdAsync(trainingId))
            .Count(r => r.Status == RegistrationStatus.Registered);
        if (activeRegistrations >= training.Capacity)
            throw new InvalidOperationException("Training is at full capacity.");

        var existing = await _registrationRepository.GetByUserAndTrainingAsync(userId, trainingId);
        if (existing != null)
        {
            if (existing.Status == RegistrationStatus.Registered)
                throw new InvalidOperationException("User is already registered.");
            existing.Status = RegistrationStatus.Registered;
            existing.RegisteredAt = DateTime.UtcNow;
            await _registrationRepository.UpdateAsync(existing);
            return MapToDto(existing);
        }

        var registration = new Registration
        {
            UserId = userId,
            TrainingId = trainingId,
            RegisteredAt = DateTime.UtcNow,
            Status = RegistrationStatus.Registered
        };
        await _registrationRepository.AddAsync(registration);
        return MapToDto(registration);
    }

    public async Task CancelAsync(int userId, int trainingId)
    {
        _appRuntimeModeService.EnsureWriteAllowed();

        var training = await _trainingRepository.GetByIdAsync(trainingId)
            ?? throw new InvalidOperationException("Training not found.");

        if (training.AttendanceLocked || training.DateTime < DateTime.UtcNow)
        {
            throw new InvalidOperationException("Registration changes are no longer allowed for this training.");
        }

        var registration = await _registrationRepository.GetByUserAndTrainingAsync(userId, trainingId)
            ?? throw new InvalidOperationException("Registration not found.");
        registration.Status = RegistrationStatus.Cancelled;
        await _registrationRepository.UpdateAsync(registration);
    }

    public async Task<bool> IsRegisteredAsync(int userId, int trainingId)
    {
        var registration = await _registrationRepository.GetByUserAndTrainingAsync(userId, trainingId);
        return registration != null && registration.Status == RegistrationStatus.Registered;
    }

    private static RegistrationDto MapToDto(Registration r) => new()
    {
        Id = r.Id,
        UserId = r.UserId,
        UserName = r.User?.DisplayName ?? string.Empty,
        TrainingId = r.TrainingId,
        TrainingTitle = r.Training?.Title ?? string.Empty,
        TrainingDateTime = r.Training?.DateTime ?? default,
        TrainingGroupName = r.Training?.Group?.Name,
        TrainingTrainerName = r.Training?.Trainer?.DisplayName,
        TrainingLocationName = r.Training?.Location?.Name,
        TrainingDurationMinutes = r.Training?.DurationMinutes,
        TrainingDescription = r.Training?.Description ?? string.Empty,
        TrainingCapacity = r.Training?.Capacity ?? 0,
        TrainingRegisteredCount = r.Training?.Registrations?.Count(registration => registration.Status == RegistrationStatus.Registered) ?? 0,
        RegisteredAt = r.RegisteredAt,
        Status = r.Status
    };
}
