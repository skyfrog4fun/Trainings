namespace Trainings.Application.DTOs;

public class LocationDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string CityName { get; set; } = string.Empty;
    public bool IsSystemWide { get; set; }
    public bool IsActive { get; set; } = true;
    public int? CountryId { get; set; }
    public string? CountryCode { get; set; }
    public string? CountryName { get; set; }
}

public class CreateLocationDto
{
    public string Name { get; set; } = string.Empty;
    public string CityName { get; set; } = string.Empty;
    public bool IsSystemWide { get; set; }
    public bool IsActive { get; set; } = true;
    public int? CountryId { get; set; }
}

public class UpdateLocationDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string CityName { get; set; } = string.Empty;
    public bool IsSystemWide { get; set; }
    public bool IsActive { get; set; } = true;
    public int? CountryId { get; set; }
}
