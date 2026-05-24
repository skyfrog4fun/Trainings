namespace Trainings.Application.DTOs;

public class CountryDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsRealCountry { get; set; }
}
