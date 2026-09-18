namespace Trainings.Application.Constants;

public static class TrainingBlockCatalog
{
    public static class TagKeys
    {
        public const string WarmUp = "WarmUp";
        public const string Fitness = "Fitness";
        public const string Technique = "Technique";
        public const string Game = "Game";
        public const string CoolDown = "CoolDown";
        public const string Other = "Other";
    }

    public static class ColorTokens
    {
        public const string Community = "--brand-community";
        public const string Accent = "--brand-accent";
        public const string Accent40 = "--brand-accent-40";
        public const string Innovation = "--brand-innovation";
        public const string AppreciationDark = "--brand-appreciation-dark";
        public const string Tradition60 = "--brand-tradition-60";

        public static readonly IReadOnlyList<string> Allowed =
        [
            Community,
            Accent,
            Accent40,
            Innovation,
            AppreciationDark,
            Tradition60
        ];
    }
}
