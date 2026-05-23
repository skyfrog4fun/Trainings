using Microsoft.EntityFrameworkCore;
using Trainings.Application.DTOs;
using Trainings.Application.Interfaces;
using Trainings.Domain.Entities;
using Trainings.Infrastructure.Data;

namespace Trainings.Infrastructure.Services;

public class CountryService : ICountryService
{
    private readonly ApplicationDbContext _context;

    public CountryService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<CountryDto>> GetAllAsync(CancellationToken ct = default)
    {
        var countries = await _context.Countries
            .OrderByDescending(c => c.IsRealCountry)
            .ThenBy(c => c.Name)
            .ToListAsync(ct);
        return countries.Select(MapToDto);
    }

    private static CountryDto MapToDto(Country country) => new()
    {
        Id = country.Id,
        Code = country.Code,
        Name = country.Name,
        IsRealCountry = country.IsRealCountry
    };
}
