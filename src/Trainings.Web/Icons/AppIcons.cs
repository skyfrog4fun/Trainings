namespace Trainings.Web.Icons;

public static class AppIcons
{
    public const string Register = "fa-solid fa-circle-check";
    public const string Unregister = "fa-solid fa-circle-minus";
    public const string Location = "fa-solid fa-location-dot";
    public const string Trainer = "fa-solid fa-person-chalkboard";
    public const string Participants = "fa-solid fa-users";
    public const string Calendar = "fa-solid fa-calendar";
    public const string Trash = "fa-solid fa-trash";
    public const string Back = "fa-solid fa-arrow-left";
    public const string Bell = "fa-solid fa-bell";
    public const string Bolt = "fa-solid fa-bolt";
    public const string BookOpen = "fa-solid fa-book-open";
    public const string CalendarCheck = "fa-solid fa-calendar-check";
    public const string ChartSimple = "fa-solid fa-chart-simple";
    public const string Check = "fa-solid fa-check";
    public const string CircleInfo = "fa-solid fa-circle-info";
    public const string ClipboardCheck = "fa-solid fa-clipboard-check";
    public const string ClipboardList = "fa-solid fa-clipboard-list";
    public const string Envelope = "fa-solid fa-envelope";
    public const string Filter = "fa-solid fa-filter";
    public const string FilterClear = "fa-solid fa-filter-circle-xmark";
    public const string Fire = "fa-solid fa-fire";
    public const string FloppyDisk = "fa-solid fa-floppy-disk";
    public const string Gear = "fa-solid fa-gear";
    public const string Group = "fa-solid fa-object-group";
    public const string Home = "fa-solid fa-house";
    public const string ListCheck = "fa-solid fa-list-check";
    public const string LocationCrosshairs = "fa-solid fa-location-crosshairs";
    public const string MapPin = "fa-solid fa-map-pin";
    public const string Edit = "fa-solid fa-pen-to-square";
    public const string Plus = "fa-solid fa-plus";
    public const string Logout = "fa-solid fa-right-from-bracket";
    public const string Refresh = "fa-solid fa-rotate-right";
    public const string Stopwatch = "fa-solid fa-stopwatch";
    public const string Tag = "fa-solid fa-tag";
    public const string User = "fa-solid fa-user";
    public const string UserCheck = "fa-solid fa-user-check";
    public const string UserClock = "fa-solid fa-user-clock";
    public const string UserGroup = "fa-solid fa-user-group";
    public const string UserMinus = "fa-solid fa-user-minus";
    public const string UserSlash = "fa-solid fa-user-slash";
    public const string Users = "fa-solid fa-users-gear";
    public const string XMark = "fa-solid fa-xmark";
    public const string ChevronUp = "fa-solid fa-chevron-up";
    public const string ChevronDown = "fa-solid fa-chevron-down";

    public static IReadOnlyList<AppIconDefinition> All { get; } =
    [
        new("Register", Register),
        new("Unregister", Unregister),
        new("Location", Location),
        new("Trainer", Trainer),
        new("Participants", Participants),
        new("Calendar", Calendar),
        new("Trash/Delete", Trash),
        new("Back", Back),
        new("Notifications", Bell),
        new("Brand", Bolt),
        new("Blocks", BookOpen),
        new("Calendar Check", CalendarCheck),
        new("Statistics", ChartSimple),
        new("Check", Check),
        new("Info", CircleInfo),
        new("Registered", ClipboardCheck),
        new("Registrations", ClipboardList),
        new("Email", Envelope),
        new("Filter", Filter),
        new("Clear Filter", FilterClear),
        new("Training", Fire),
        new("Save", FloppyDisk),
        new("Settings", Gear),
        new("Group", Group),
        new("Home", Home),
        new("Attendance", ListCheck),
        new("Special Location", LocationCrosshairs),
        new("Meeting Point", MapPin),
        new("Edit", Edit),
        new("Add", Plus),
        new("Logout", Logout),
        new("Refresh", Refresh),
        new("Duration", Stopwatch),
        new("Version", Tag),
        new("User", User),
        new("Approve User", UserCheck),
        new("Pending User", UserClock),
        new("Groups", UserGroup),
        new("Reject User", UserMinus),
        new("Disable User", UserSlash),
        new("User Management", Users),
        new("Close/Cancel", XMark),
        new("Expand", ChevronDown),
        new("Collapse", ChevronUp)
    ];
}

public sealed record AppIconDefinition(string FriendlyName, string CssClass);
