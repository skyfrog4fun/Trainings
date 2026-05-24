using Trainings.Application.DTOs;

namespace Trainings.Application.Interfaces;

public interface ICountryService
{
    Task<IEnumerable<CountryDto>> GetAllAsync(CancellationToken ct = default);
}
