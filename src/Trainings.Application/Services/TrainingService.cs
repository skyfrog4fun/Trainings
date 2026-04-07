using Trainings.Application.DTOs;
using Trainings.Application.Interfaces;
using Trainings.Domain.Entities;
using Trainings.Domain.Interfaces;

namespace Trainings.Application.Services;

public class TrainingService : ITrainingService
{
    private readonly ITrainingRepository _trainingRepository;

    public TrainingService(ITrainingRepository trainingRepository)
    {
        _trainingRepository = trainingRepository;
    }

    public async Task<TrainingDto?> GetByIdAsync(int id)
    {
        var training = await _trainingRepository.GetByIdAsync(id);
        return training == null ? null : MapToDto(training);
    }

    public async Task<IEnumerable<TrainingDto>> GetAllAsync()
    {
        var trainings = await _trainingRepository.GetAllAsync();
        return trainings.Select(MapToDto);
    }

    public async Task<IEnumerable<TrainingDto>> GetActiveAsync()
    {
        var trainings = await _trainingRepository.GetActiveAsync();
        return trainings.Select(MapToDto);
    }

    public async Task<IEnumerable<TrainingDto>> GetByTrainerIdAsync(int trainerId)
    {
        var trainings = await _trainingRepository.GetByTrainerIdAsync(trainerId);
        return trainings.Select(MapToDto);
    }

    public async Task<TrainingDto> CreateAsync(CreateTrainingDto dto)
    {
        var training = new Training
        {
            Title = dto.Title,
            Description = dto.Description,
            Location = dto.Location,
            DateTime = dto.DateTime,
            Capacity = dto.Capacity,
            TrainerId = dto.TrainerId,
            IsActive = true
        };
        await _trainingRepository.AddAsync(training);
        return MapToDto(training);
    }

    public async Task UpdateAsync(UpdateTrainingDto dto)
    {
        var training = await _trainingRepository.GetByIdAsync(dto.Id)
            ?? throw new InvalidOperationException($"Training {dto.Id} not found.");
        training.Title = dto.Title;
        training.Description = dto.Description;
        training.Location = dto.Location;
        training.DateTime = dto.DateTime;
        training.Capacity = dto.Capacity;
        training.IsActive = dto.IsActive;
        await _trainingRepository.UpdateAsync(training);
    }

    public async Task DeleteAsync(int id)
    {
        await _trainingRepository.DeleteAsync(id);
    }

    private static TrainingDto MapToDto(Training t) => new()
    {
        Id = t.Id,
        Title = t.Title,
        Description = t.Description,
        Location = t.Location,
        DateTime = t.DateTime,
        Capacity = t.Capacity,
        IsActive = t.IsActive,
        TrainerId = t.TrainerId,
        TrainerName = t.Trainer?.Name ?? string.Empty,
        RegisteredCount = t.Registrations?.Count(r => r.Status == Domain.Enums.RegistrationStatus.Registered) ?? 0
    };
}
