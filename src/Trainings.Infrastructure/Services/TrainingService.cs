using Microsoft.EntityFrameworkCore;
using Trainings.Application.DTOs;
using Trainings.Application.Interfaces;
using Trainings.Domain.Entities;
using Trainings.Domain.Interfaces;
using Trainings.Infrastructure.Data;

namespace Trainings.Infrastructure.Services;

public class TrainingService : ITrainingService
{
    private readonly ITrainingRepository _trainingRepository;
    private readonly ApplicationDbContext _context;
    private readonly IAppRuntimeModeService _appRuntimeModeService;

    public TrainingService(ITrainingRepository trainingRepository, ApplicationDbContext context, IAppRuntimeModeService appRuntimeModeService)
    {
        _trainingRepository = trainingRepository;
        _context = context;
        _appRuntimeModeService = appRuntimeModeService;
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
        _appRuntimeModeService.EnsureWriteAllowed();
        if (!dto.GroupId.HasValue)
        {
            throw new InvalidOperationException("A training group must be selected.");
        }

        var training = new Training
        {
            Title = dto.Title,
            Description = dto.Description,
            LocationId = dto.LocationId,
            SpecialLocationDescription = dto.SpecialLocationDescription,
            MeetingPoint = dto.MeetingPoint,
            DateTime = dto.DateTime,
            DurationMinutes = dto.DurationMinutes,
            Capacity = dto.Capacity,
            TrainerId = dto.TrainerId,
            GroupId = dto.GroupId,
            IsActive = true
        };
        await _trainingRepository.AddAsync(training);
        return MapToDto(training);
    }

    public async Task UpdateAsync(UpdateTrainingDto dto)
    {
        _appRuntimeModeService.EnsureWriteAllowed();
        if (!dto.GroupId.HasValue)
        {
            throw new InvalidOperationException("A training group must be selected.");
        }

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
        training.IsActive = dto.IsActive;
        training.TrainerId = dto.TrainerId;
        training.GroupId = dto.GroupId;
        await _trainingRepository.UpdateAsync(training);
    }

    public async Task DeleteAsync(int id)
    {
        _appRuntimeModeService.EnsureWriteAllowed();
        await _trainingRepository.DeleteAsync(id);
    }

    public async Task<IEnumerable<TrainingBlockDto>> GetBlocksAsync(int trainingId, CancellationToken ct = default)
    {
        var blocks = await _context.TrainingBlocks
            .Include(b => b.TrainingBlockTags)
                .ThenInclude(bt => bt.Tag)
            .Where(b => b.TrainingId == trainingId)
            .OrderBy(b => b.OrderIndex)
            .ToListAsync(ct);
        return blocks.Select(MapBlockToDto);
    }

    public async Task<TrainingBlockDto> AddBlockAsync(CreateTrainingBlockDto dto, CancellationToken ct = default)
    {
        _appRuntimeModeService.EnsureWriteAllowed();

        var block = new TrainingBlock
        {
            TrainingId = dto.TrainingId,
            OrderIndex = dto.OrderIndex,
            Title = dto.Title,
            Description = dto.Description,
            PlannedDurationMinutes = dto.PlannedDurationMinutes,
            CreatedAt = DateTime.UtcNow
        };

        foreach (var tagId in dto.TagIds)
        {
            block.TrainingBlockTags.Add(new TrainingBlockTag { TagId = tagId });
        }

        _context.TrainingBlocks.Add(block);
        await _context.SaveChangesAsync(ct);

        // Reload with tags
        await _context.Entry(block)
            .Collection(b => b.TrainingBlockTags)
            .Query()
            .Include(bt => bt.Tag)
            .LoadAsync(ct);

        return MapBlockToDto(block);
    }

    public async Task UpdateBlockAsync(UpdateTrainingBlockDto dto, CancellationToken ct = default)
    {
        _appRuntimeModeService.EnsureWriteAllowed();

        var block = await _context.TrainingBlocks
            .Include(b => b.TrainingBlockTags)
            .FirstOrDefaultAsync(b => b.Id == dto.Id, ct)
            ?? throw new InvalidOperationException($"Block {dto.Id} not found.");

        block.OrderIndex = dto.OrderIndex;
        block.Title = dto.Title;
        block.Description = dto.Description;
        block.PlannedDurationMinutes = dto.PlannedDurationMinutes;
        block.EffectiveDurationMinutes = dto.EffectiveDurationMinutes;
        block.TrainerComment = dto.TrainerComment;

        // Update tags
        block.TrainingBlockTags.Clear();
        foreach (var tagId in dto.TagIds)
        {
            block.TrainingBlockTags.Add(new TrainingBlockTag { TrainingBlockId = block.Id, TagId = tagId });
        }

        await _context.SaveChangesAsync(ct);
    }

    public async Task DeleteBlockAsync(int blockId, CancellationToken ct = default)
    {
        _appRuntimeModeService.EnsureWriteAllowed();

        var block = await _context.TrainingBlocks.FindAsync([blockId], ct);
        if (block != null)
        {
            _context.TrainingBlocks.Remove(block);
            await _context.SaveChangesAsync(ct);
        }
    }

    public async Task CopyBlockAsync(int sourceBlockId, int targetTrainingId, CancellationToken ct = default)
    {
        _appRuntimeModeService.EnsureWriteAllowed();

        var source = await _context.TrainingBlocks
            .Include(b => b.TrainingBlockTags)
            .FirstOrDefaultAsync(b => b.Id == sourceBlockId, ct)
            ?? throw new InvalidOperationException($"Block {sourceBlockId} not found.");

        var maxOrder = await _context.TrainingBlocks
            .Where(b => b.TrainingId == targetTrainingId)
            .MaxAsync(b => (int?)b.OrderIndex, ct) ?? 0;

        var copy = new TrainingBlock
        {
            TrainingId = targetTrainingId,
            OrderIndex = maxOrder + 1,
            Title = source.Title,
            Description = source.Description,
            PlannedDurationMinutes = source.PlannedDurationMinutes,
            SourceBlockId = sourceBlockId,
            CreatedAt = DateTime.UtcNow
        };

        foreach (var tag in source.TrainingBlockTags)
        {
            copy.TrainingBlockTags.Add(new TrainingBlockTag { TagId = tag.TagId });
        }

        _context.TrainingBlocks.Add(copy);
        await _context.SaveChangesAsync(ct);
    }

    public async Task<IEnumerable<TrainingBlockDto>> GetAllBlocksLibraryAsync(CancellationToken ct = default)
    {
        var blocks = await _context.TrainingBlocks
            .Include(b => b.TrainingBlockTags)
                .ThenInclude(bt => bt.Tag)
            .Include(b => b.Training)
                .ThenInclude(t => t.Trainer)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync(ct);
        return blocks.Select(MapBlockToDto);
    }

    public async Task LockAttendanceAsync(int trainingId, CancellationToken ct = default)
    {
        _appRuntimeModeService.EnsureWriteAllowed();

        var training = await _context.Trainings.FindAsync([trainingId], ct)
            ?? throw new InvalidOperationException($"Training {trainingId} not found.");
        training.AttendanceLocked = true;
        training.AttendanceLockedAt = DateTime.UtcNow;
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
        for (var week = 0; week < maxWeeksAhead; week++, candidate = candidate.AddDays(7))
        {
            if (!occupiedDates.Contains(candidate.Date))
                return candidate;
        }

        throw new InvalidOperationException(
            $"No available training date found for group {groupId} within {maxWeeksAhead} weeks.");
    }

    private static DateTime GetNextWeekday(DateTime startDate, DayOfWeek day)
    {
        var offset = ((int)day - (int)startDate.DayOfWeek + 7) % 7;
        if (offset == 0)
        {
            offset = 7;
        }

        return startDate.AddDays(offset);
    }

    private static TrainingDto MapToDto(Training t) => new()
    {
        Id = t.Id,
        Title = t.Title,
        Description = t.Description,
        LocationId = t.LocationId,
        LocationName = t.Location?.Name,
        SpecialLocationDescription = t.SpecialLocationDescription,
        MeetingPoint = t.MeetingPoint,
        DateTime = t.DateTime,
        DurationMinutes = t.DurationMinutes,
        Capacity = t.Capacity,
        IsActive = t.IsActive,
        TrainerId = t.TrainerId,
        TrainerName = t.Trainer?.DisplayName ?? string.Empty,
        RegisteredCount = t.Registrations?.Count(r => r.Status == Domain.Enums.RegistrationStatus.Registered) ?? 0,
        GroupId = t.GroupId,
        GroupName = t.Group?.Name,
        GroupCountry = t.Group?.Country?.Code,
        AttendanceLocked = t.AttendanceLocked,
        AttendanceLockedAt = t.AttendanceLockedAt,
        Blocks = t.Blocks?
            .OrderBy(b => b.OrderIndex)
            .Select(MapBlockToDto)
            .ToList() ?? new List<TrainingBlockDto>()
    };

    private static TrainingBlockDto MapBlockToDto(TrainingBlock b) => new()
    {
        Id = b.Id,
        TrainingId = b.TrainingId,
        OrderIndex = b.OrderIndex,
        Title = b.Title,
        Description = b.Description,
        PlannedDurationMinutes = b.PlannedDurationMinutes,
        EffectiveDurationMinutes = b.EffectiveDurationMinutes,
        TrainerComment = b.TrainerComment,
        SourceBlockId = b.SourceBlockId,
        CreatedAt = b.CreatedAt,
        TrainerId = b.Training?.TrainerId ?? 0,
        TrainerName = b.Training?.Trainer?.DisplayName ?? string.Empty,
        Tags = b.TrainingBlockTags
            .Where(bt => bt.Tag is not null)
            .Select(bt => new TagDto
            {
                Id = bt.Tag.Id,
                Name = bt.Tag.Name,
                GroupId = bt.Tag.GroupId
            })
            .ToList()
    };
}
