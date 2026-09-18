namespace Trainings.Application.DTOs;

public class TagDto
{
    public int Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ColorToken { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; }
}

public class TagAdminDto
{
    public int Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string EnglishText { get; set; } = string.Empty;
    public string GermanText { get; set; } = string.Empty;
    public string ColorToken { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; }
}
