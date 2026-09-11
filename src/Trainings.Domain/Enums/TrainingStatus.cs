namespace Trainings.Domain.Enums;

public enum TrainingStatus
{
    /// <summary>Created by a GroupAdmin, not yet assigned to a trainer.</summary>
    New = 0,

    /// <summary>A trainer has taken the training and is still preparing it.</summary>
    InPlanning = 1,

    /// <summary>The assigned trainer confirmed the training is ready.</summary>
    Planned = 2,

    /// <summary>The assigned trainer manually started the training; registration is closed.</summary>
    InProgress = 3,

    /// <summary>Attendance has been finalized and locked; only non-attendance fields remain editable.</summary>
    Done = 4
}
