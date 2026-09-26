namespace Trainings.Application.DTOs;

public class TrainingBlockDefinitionDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DurationMinutes { get; set; }
    public int TagId { get; set; }
    public string TagKey { get; set; } = string.Empty;
    public string TagDisplayText { get; set; } = string.Empty;
    public string TagColorToken { get; set; } = string.Empty;
    public int? GameId { get; set; }
    public string? GameDisplayText { get; set; }
    public int MinParticipants { get; set; }
    public int MaxParticipants { get; set; }
    public int? GroupId { get; set; }
    public bool IsGlobal { get; set; }
    public int CreatorId { get; set; }
    public string CreatorName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; }
}

public class CreateTrainingBlockDefinitionDto
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DurationMinutes { get; set; }
    public int TagId { get; set; }
    public int? GameId { get; set; }
    public int MinParticipants { get; set; }
    public int MaxParticipants { get; set; }
    public int? GroupId { get; set; }
    public bool IsGlobal { get; set; }
}

public class UpdateTrainingBlockDefinitionDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DurationMinutes { get; set; }
    public int TagId { get; set; }
    public int? GameId { get; set; }
    public int MinParticipants { get; set; }
    public int MaxParticipants { get; set; }
    public bool IsActive { get; set; }
}

public class TrainingBlockDto
{
    public int Id { get; set; }
    public int TrainingId { get; set; }
    public int OrderIndex { get; set; }
    public int DefinitionId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int PlannedDurationMinutes { get; set; }
    public int MinParticipants { get; set; }
    public int MaxParticipants { get; set; }
    public int TagId { get; set; }
    public string TagKey { get; set; } = string.Empty;
    public string TagDisplayText { get; set; } = string.Empty;
    public string TagColorToken { get; set; } = string.Empty;
    public int? GameId { get; set; }
    public string? GameDisplayText { get; set; }
    public bool DefinitionIsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateTrainingBlockDto
{
    public int TrainingId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DurationMinutes { get; set; }
    public int TagId { get; set; }
    public int? GameId { get; set; }
    public string? NewGameName { get; set; }
    public int MinParticipants { get; set; }
    public int MaxParticipants { get; set; }
    public bool CreateGlobalDefinition { get; set; }
}

public class AddTrainingBlockFromDefinitionDto
{
    public int TrainingId { get; set; }
    public int DefinitionId { get; set; }
}

public class UpdateTrainingBlockExecutionDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int PlannedDurationMinutes { get; set; }
    public int MinParticipants { get; set; }
    public int MaxParticipants { get; set; }
}

public class TrainingBlockLibrarySearchDto
{
    public int? TagId { get; set; }
    public int? CreatorId { get; set; }
    public int? MinDurationMinutes { get; set; }
    public int? MaxDurationMinutes { get; set; }
    public string? SearchText { get; set; }
    public int? GroupId { get; set; }
    public int Skip { get; set; }
    public int Take { get; set; } = 10;
}

public class TrainingBlockLibraryCreatorDto
{
    public int CreatorId { get; set; }
    public string CreatorName { get; set; } = string.Empty;
}

public class TrainingBlockLibrarySearchResultDto
{
    public IReadOnlyList<TrainingBlockDefinitionDto> Items { get; set; } = [];
    public IReadOnlyList<TrainingBlockLibraryCreatorDto> CreatorOptions { get; set; } = [];
    public bool HasMore { get; set; }
}
