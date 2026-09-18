using Microsoft.EntityFrameworkCore;
using Trainings.Application.DTOs;
using Trainings.Application.Interfaces;
using Trainings.Domain.Entities;
using Trainings.Domain.Enums;
using Trainings.Domain.Interfaces;
using Trainings.Infrastructure.Data;

namespace Trainings.Infrastructure.Services;

public class TrainingService(
    ITrainingRepository trainingRepository,
    IRegistrationRepository registrationRepository,
    ApplicationDbContext context,
    IAppRuntimeModeService appRuntimeModeService,
    IDateTimeFormatService dateTimeFormatService,
    ITranslationService translationService) : ITrainingService
{
    private readonly ITrainingRepository _trainingRepository = trainingRepository;
    private readonly IRegistrationRepository _registrationRepository = registrationRepository;
    private readonly ApplicationDbContext _context = context;
    private readonly IAppRuntimeModeService _appRuntimeModeService = appRuntimeModeService;
    private readonly IDateTimeFormatService _dateTimeFormatService = dateTimeFormatService;
    private readonly ITranslationService _translationService = translationService;

    public async Task<TrainingDto?> GetByIdAsync(int id)
    {
        var training = await _trainingRepository.GetByIdAsync(id);
        return training == null ? null : await MapToDtoAsync(training);
    }

    public async Task<IEnumerable<TrainingDto>> GetAllAsync()
    {
        var trainings = await _trainingRepository.GetAllAsync();
        return await MapTrainingsAsync(trainings);
    }

    public async Task<IEnumerable<TrainingDto>> GetActiveAsync()
    {
        var trainings = await _trainingRepository.GetActiveAsync();
        return await MapTrainingsAsync(trainings);
    }

    public async Task<IEnumerable<TrainingDto>> GetByTrainerIdAsync(int trainerId)
    {
        var trainings = await _trainingRepository.GetByTrainerIdAsync(trainerId);
        return await MapTrainingsAsync(trainings);
    }

    public async Task<TrainingDto> CreateAsync(CreateTrainingDto dto)
    {
        _appRuntimeModeService.EnsureWriteAllowed();

        string title = dto.Title;
        if (string.IsNullOrWhiteSpace(title))
        {
            var group = await _context.Groups
                .Include(g => g.Country)
                .FirstOrDefaultAsync(g => g.Id == dto.GroupId);
            var culture = _dateTimeFormatService.GetCultureForCountry(group?.Country?.Code);
            title = $"Training of {dto.DateTime.ToString("d", culture)}";
        }

        var training = new Training
        {
            Title = title,
            Description = dto.Description,
            LocationId = dto.LocationId,
            SpecialLocationDescription = dto.SpecialLocationDescription,
            MeetingPoint = dto.MeetingPoint,
            DateTime = dto.DateTime,
            DurationMinutes = dto.DurationMinutes,
            Capacity = dto.Capacity,
            TrainerId = dto.TrainerId,
            GroupId = dto.GroupId,
            Status = dto.TrainerId.HasValue ? TrainingStatus.InPlanning : TrainingStatus.New
        };

        await _trainingRepository.AddAsync(training);
        return await MapToDtoAsync(training);
    }

    public async Task UpdateAsync(UpdateTrainingDto dto)
    {
        _appRuntimeModeService.EnsureWriteAllowed();

        var training = await _trainingRepository.GetByIdAsync(dto.Id)
            ?? throw new InvalidOperationException($"Training {dto.Id} not found.");

        training.Title = dto.Title;
        training.Description = dto.Description;
        training.LocationId = dto.LocationId;
        training.SpecialLocationDescription = dto.SpecialLocationDescription;
        training.MeetingPoint = dto.MeetingPoint;
        training.DateTime = dto.DateTime;
        training.DurationMinutes = dto.DurationMinutes;
        training.Capacity = dto.Capacity;
        training.Status = dto.Status;
        training.TrainerId = dto.TrainerId;
        training.GroupId = dto.GroupId;

        await _trainingRepository.UpdateAsync(training);
    }

    public async Task DeleteAsync(int id)
    {
        _appRuntimeModeService.EnsureWriteAllowed();
        await _trainingRepository.DeleteAsync(id);
    }

    public async Task SetStatusAsync(int trainingId, TrainingStatus status, CancellationToken ct = default)
    {
        _appRuntimeModeService.EnsureWriteAllowed();
        var training = await _context.Trainings.FindAsync([trainingId], ct)
            ?? throw new InvalidOperationException($"Training {trainingId} not found.");
        training.Status = status;
        await _context.SaveChangesAsync(ct);
    }

    public async Task LockAttendanceAsync(int trainingId, CancellationToken ct = default)
    {
        _appRuntimeModeService.EnsureWriteAllowed();

        var training = await _context.Trainings.FindAsync([trainingId], ct)
            ?? throw new InvalidOperationException($"Training {trainingId} not found.");
        training.AttendanceLocked = true;
        training.AttendanceLockedAt = DateTime.UtcNow;
        training.Status = TrainingStatus.Done;
        await _context.SaveChangesAsync(ct);
    }

    public async Task<TrainingDto> TakeAsync(int trainingId, int trainerId, CancellationToken ct = default)
    {
        _appRuntimeModeService.EnsureWriteAllowed();

        var training = await _trainingRepository.GetByIdAsync(trainingId)
            ?? throw new InvalidOperationException($"Training {trainingId} not found.");

        if (training.TrainerId.HasValue)
        {
            throw new InvalidOperationException("This training already has a trainer assigned.");
        }

        if (training.Status != TrainingStatus.New)
        {
            throw new InvalidOperationException("Only an unassigned training can be taken.");
        }

        training.TrainerId = trainerId;
        training.Status = TrainingStatus.InPlanning;
        await _trainingRepository.UpdateAsync(training);

        var existingRegistration = await _registrationRepository.GetByUserAndTrainingAsync(trainerId, trainingId);
        if (existingRegistration == null)
        {
            await _registrationRepository.AddAsync(new Registration
            {
                UserId = trainerId,
                TrainingId = trainingId,
                RegisteredAt = DateTime.UtcNow,
                Status = RegistrationStatus.Registered
            });
        }
        else if (existingRegistration.Status != RegistrationStatus.Registered)
        {
            existingRegistration.Status = RegistrationStatus.Registered;
            existingRegistration.RegisteredAt = DateTime.UtcNow;
            await _registrationRepository.UpdateAsync(existingRegistration);
        }

        return await MapToDtoAsync(training);
    }

    public async Task ReleaseTrainerAsync(int trainingId, int requestingUserId, CancellationToken ct = default)
    {
        _appRuntimeModeService.EnsureWriteAllowed();

        var training = await _context.Trainings.FindAsync([trainingId], ct)
            ?? throw new InvalidOperationException($"Training {trainingId} not found.");

        if (training.TrainerId != requestingUserId)
        {
            throw new InvalidOperationException("Only the assigned trainer can release this training.");
        }

        if (training.Status is TrainingStatus.InProgress or TrainingStatus.Done)
        {
            throw new InvalidOperationException("A training that is in progress or already done cannot be released.");
        }

        training.TrainerId = null;
        training.Status = TrainingStatus.New;
        await _context.SaveChangesAsync(ct);
    }

    public async Task ReassignTrainerAsync(int trainingId, int? newTrainerId, CancellationToken ct = default)
    {
        _appRuntimeModeService.EnsureWriteAllowed();

        var training = await _context.Trainings.FindAsync([trainingId], ct)
            ?? throw new InvalidOperationException($"Training {trainingId} not found.");

        if (training.Status is TrainingStatus.InProgress or TrainingStatus.Done)
        {
            throw new InvalidOperationException("The trainer cannot be changed once the training has started.");
        }

        training.TrainerId = newTrainerId;
        training.Status = newTrainerId.HasValue
            ? (training.Status == TrainingStatus.New ? TrainingStatus.InPlanning : training.Status)
            : TrainingStatus.New;
        await _context.SaveChangesAsync(ct);
    }

    public async Task ConfirmPlannedAsync(int trainingId, CancellationToken ct = default)
    {
        _appRuntimeModeService.EnsureWriteAllowed();

        var training = await _context.Trainings.FindAsync([trainingId], ct)
            ?? throw new InvalidOperationException($"Training {trainingId} not found.");

        if (!training.TrainerId.HasValue)
        {
            throw new InvalidOperationException("A trainer must be assigned before the training can be confirmed as planned.");
        }

        if (training.Status != TrainingStatus.InPlanning)
        {
            throw new InvalidOperationException("Only a training that is in planning can be confirmed as planned.");
        }

        training.Status = TrainingStatus.Planned;
        await _context.SaveChangesAsync(ct);
    }

    public async Task StartAsync(int trainingId, int requestingTrainerId, CancellationToken ct = default)
    {
        _appRuntimeModeService.EnsureWriteAllowed();

        var training = await _context.Trainings.FindAsync([trainingId], ct)
            ?? throw new InvalidOperationException($"Training {trainingId} not found.");

        if (training.TrainerId != requestingTrainerId)
        {
            throw new InvalidOperationException("Only the assigned trainer can start this training.");
        }

        if (training.Status != TrainingStatus.Planned)
        {
            throw new InvalidOperationException("Only a planned training can be started.");
        }

        training.Status = TrainingStatus.InProgress;
        await _context.SaveChangesAsync(ct);
    }

    public async Task<DateTime> GetNextAvailableDateForGroupAsync(int groupId, DayOfWeek weekday, CancellationToken ct = default)
    {
        var occupiedDates = (await _context.Trainings
            .Where(t => t.GroupId == groupId)
            .Select(t => t.DateTime.Date)
            .ToListAsync(ct))
            .ToHashSet();

        const int maxWeeksAhead = 52;
        var candidate = GetNextWeekday(DateTime.Today, weekday);
        for (int week = 0; week < maxWeeksAhead; week++, candidate = candidate.AddDays(7))
        {
            if (!occupiedDates.Contains(candidate.Date))
            {
                return candidate;
            }
        }

        throw new InvalidOperationException($"No available training date found for group {groupId} within {maxWeeksAhead} weeks.");
    }

    private async Task<IEnumerable<TrainingDto>> MapTrainingsAsync(IEnumerable<Training> trainings)
    {
        var trainingList = trainings.ToList();
        if (trainingList.Count == 0)
        {
            return [];
        }

        var blocks = trainingList.SelectMany(t => t.Blocks).ToList();
        var tagTexts = await _translationService.GetTextLookupAsync(TranslationEntityType.Tag, blocks.Select(b => b.Definition.TagId));
        var gameTexts = await _translationService.GetTextLookupAsync(
            TranslationEntityType.Game,
            blocks.Where(b => b.Definition.GameId.HasValue).Select(b => b.Definition.GameId!.Value));

        return trainingList.Select(training => MapTraining(training, tagTexts, gameTexts)).ToList();
    }

    private async Task<TrainingDto> MapToDtoAsync(Training training)
    {
        var blocks = training.Blocks.ToList();
        var tagTexts = await _translationService.GetTextLookupAsync(TranslationEntityType.Tag, blocks.Select(b => b.Definition.TagId));
        var gameTexts = await _translationService.GetTextLookupAsync(
            TranslationEntityType.Game,
            blocks.Where(b => b.Definition.GameId.HasValue).Select(b => b.Definition.GameId!.Value));

        return MapTraining(training, tagTexts, gameTexts);
    }

    private static TrainingDto MapBaseTraining(Training training) => new()
    {
        Id = training.Id,
        Title = training.Title,
        Description = training.Description,
        LocationId = training.LocationId,
        LocationName = training.Location?.Name,
        LocationCity = training.Location?.CityName,
        SpecialLocationDescription = training.SpecialLocationDescription,
        MeetingPoint = training.MeetingPoint,
        DateTime = training.DateTime,
        DurationMinutes = training.DurationMinutes,
        Capacity = training.Capacity,
        Status = training.Status,
        TrainerId = training.TrainerId,
        TrainerName = training.Trainer?.DisplayName ?? string.Empty,
        RegisteredCount = training.Registrations?.Count(r => r.Status == RegistrationStatus.Registered) ?? 0,
        GroupId = training.GroupId,
        GroupName = training.Group?.Name,
        GroupSlug = training.Group?.Slug,
        GroupCountry = training.Group?.Country?.Code,
        AttendanceLocked = training.AttendanceLocked,
        AttendanceLockedAt = training.AttendanceLockedAt
    };

    private static TrainingDto MapTraining(
        Training training,
        IReadOnlyDictionary<int, TranslationTextsDto> tagTexts,
        IReadOnlyDictionary<int, TranslationTextsDto> gameTexts)
    {
        var dto = MapBaseTraining(training);
        dto.Blocks = [.. training.Blocks
            .OrderBy(b => b.OrderIndex)
            .Select(block => TrainingBlockMappingHelper.MapExecution(block, tagTexts, gameTexts))];
        return dto;
    }

    private static DateTime GetNextWeekday(DateTime startDate, DayOfWeek day)
    {
        int offset = ((int)day - (int)startDate.DayOfWeek + 7) % 7;
        if (offset == 0)
        {
            offset = 7;
        }

        return startDate.AddDays(offset);
    }
}
