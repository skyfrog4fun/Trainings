using Microsoft.EntityFrameworkCore;
using Trainings.Application.DTOs;
using Trainings.Application.Interfaces;
using Trainings.Domain.Entities;
using Trainings.Domain.Enums;
using Trainings.Infrastructure.Data;

namespace Trainings.Infrastructure.Services;

public class FeedbackService(ApplicationDbContext context, IAppRuntimeModeService appRuntimeModeService) : IFeedbackService
{
    private readonly ApplicationDbContext _context = context;
    private readonly IAppRuntimeModeService _appRuntimeModeService = appRuntimeModeService;

    public async Task<TrainingFeedbackOverviewDto> GetOverviewAsync(int trainingId, int requestingUserId, bool isStaffViewer, CancellationToken ct = default)
    {
        var participantEntries = await _context.ParticipantFeedbacks
            .AsNoTracking()
            .Where(f => f.TrainingId == trainingId)
            .ToListAsync(ct);

        var overview = new TrainingFeedbackOverviewDto
        {
            TrainingId = trainingId,
            ParticipantAggregate = new ParticipantFeedbackAggregateDto
            {
                Count = participantEntries.Count,
                AverageRating = participantEntries.Any(f => f.Rating.HasValue)
                    ? participantEntries.Where(f => f.Rating.HasValue).Average(f => f.Rating!.Value)
                    : null
            },
            OwnParticipantFeedback = participantEntries
                .Where(f => f.UserId == requestingUserId)
                .Select(f => new OwnParticipantFeedbackDto
                {
                    TrainingId = f.TrainingId,
                    UserId = f.UserId,
                    Comment = f.Comment,
                    Rating = f.Rating,
                    SubmittedAt = f.SubmittedAt
                })
                .FirstOrDefault()
        };

        if (isStaffViewer)
        {
            overview.ParticipantFeedbackEntries = participantEntries
                .OrderBy(f => f.SubmittedAt)
                .Select(f => new ParticipantFeedbackEntryDto
                {
                    Comment = f.Comment,
                    Rating = f.Rating,
                    SubmittedAt = f.SubmittedAt
                })
                .ToList();

            var trainerFeedback = await _context.TrainerFeedbacks
                .AsNoTracking()
                .Include(f => f.Trainer)
                .FirstOrDefaultAsync(f => f.TrainingId == trainingId, ct);

            if (trainerFeedback is not null)
            {
                overview.TrainerFeedback = new TrainerFeedbackDto
                {
                    Id = trainerFeedback.Id,
                    TrainingId = trainerFeedback.TrainingId,
                    TrainerId = trainerFeedback.TrainerId,
                    TrainerName = trainerFeedback.Trainer.DisplayName,
                    Comment = trainerFeedback.Comment,
                    Rating = trainerFeedback.Rating,
                    SubmittedAt = trainerFeedback.SubmittedAt
                };
            }
        }

        return overview;
    }

    public async Task UpsertTrainerFeedbackAsync(int trainerId, UpsertTrainerFeedbackDto dto, CancellationToken ct = default)
    {
        _appRuntimeModeService.EnsureWriteAllowed();

        var training = await _context.Trainings.FirstOrDefaultAsync(t => t.Id == dto.TrainingId, ct)
            ?? throw new InvalidOperationException("Training not found.");

        if (training.Status != TrainingStatus.Done)
        {
            throw new InvalidOperationException("Trainer feedback can only be submitted once the training is done.");
        }

        if (training.TrainerId != trainerId)
        {
            throw new InvalidOperationException("Only the assigned trainer can submit feedback for this training.");
        }

        var existing = await _context.TrainerFeedbacks.FirstOrDefaultAsync(f => f.TrainingId == dto.TrainingId, ct);
        if (existing is null)
        {
            _context.TrainerFeedbacks.Add(new TrainerFeedback
            {
                TrainingId = dto.TrainingId,
                TrainerId = trainerId,
                Comment = dto.Comment,
                Rating = dto.Rating,
                SubmittedAt = DateTime.UtcNow
            });
        }
        else
        {
            existing.Comment = dto.Comment;
            existing.Rating = dto.Rating;
            existing.SubmittedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(ct);
    }

    public async Task UpsertParticipantFeedbackAsync(int userId, UpsertParticipantFeedbackDto dto, CancellationToken ct = default)
    {
        _appRuntimeModeService.EnsureWriteAllowed();

        var training = await _context.Trainings.FirstOrDefaultAsync(t => t.Id == dto.TrainingId, ct)
            ?? throw new InvalidOperationException("Training not found.");

        if (training.Status != TrainingStatus.Done)
        {
            throw new InvalidOperationException("Participant feedback can only be submitted once the training is done.");
        }

        var isRegisteredParticipant = await _context.Registrations
            .AnyAsync(r => r.TrainingId == dto.TrainingId && r.UserId == userId, ct);
        if (!isRegisteredParticipant)
        {
            throw new InvalidOperationException("Only a registered participant of this training can submit feedback.");
        }

        var existing = await _context.ParticipantFeedbacks
            .FirstOrDefaultAsync(f => f.TrainingId == dto.TrainingId && f.UserId == userId, ct);
        if (existing is null)
        {
            _context.ParticipantFeedbacks.Add(new ParticipantFeedback
            {
                TrainingId = dto.TrainingId,
                UserId = userId,
                Comment = dto.Comment,
                Rating = dto.Rating,
                SubmittedAt = DateTime.UtcNow
            });
        }
        else
        {
            existing.Comment = dto.Comment;
            existing.Rating = dto.Rating;
            existing.SubmittedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(ct);
    }
}
