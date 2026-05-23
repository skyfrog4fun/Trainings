using Microsoft.EntityFrameworkCore;
using Trainings.Application.DTOs;
using Trainings.Application.Interfaces;
using Trainings.Domain.Entities;
using Trainings.Infrastructure.Data;

namespace Trainings.Infrastructure.Services;

public class LocationService : ILocationService
{
    private readonly ApplicationDbContext _context;
    private readonly IAppRuntimeModeService _appRuntimeModeService;

    public LocationService(ApplicationDbContext context, IAppRuntimeModeService appRuntimeModeService)
    {
        _context = context;
        _appRuntimeModeService = appRuntimeModeService;
    }

    public async Task<IEnumerable<LocationDto>> GetAllAsync(CancellationToken ct = default)
    {
        var locations = await _context.Locations
            .Include(l => l.Country)
            .OrderBy(l => l.IsSystemWide ? 0 : 1)
            .ThenBy(l => l.Name)
            .ToListAsync(ct);
        return locations.Select(MapToDto);
    }

    public async Task<IEnumerable<LocationDto>> GetByGroupIdAsync(int groupId, CancellationToken ct = default)
    {
        var locations = await _context.Locations
            .Include(l => l.Country)
            .Where(l => l.IsSystemWide || l.AllowedForGroups.Any(gl => gl.GroupId == groupId))
            .OrderBy(l => l.IsSystemWide ? 0 : 1)
            .ThenBy(l => l.Name)
            .ToListAsync(ct);
        return locations.Select(MapToDto);
    }

    public async Task<LocationDto> CreateAsync(CreateLocationDto dto, CancellationToken ct = default)
    {
        _appRuntimeModeService.EnsureWriteAllowed();

        var location = new Location
        {
            Name = dto.Name.Trim(),
            CityName = dto.CityName.Trim(),
            IsSystemWide = dto.IsSystemWide,
            IsActive = dto.IsActive,
            CountryId = dto.CountryId
        };

        _context.Locations.Add(location);
        await _context.SaveChangesAsync(ct);
        await _context.Entry(location).Reference(l => l.Country).LoadAsync(ct);
        return MapToDto(location);
    }

    public async Task UpdateAsync(UpdateLocationDto dto, CancellationToken ct = default)
    {
        _appRuntimeModeService.EnsureWriteAllowed();

        var location = await _context.Locations.FindAsync([dto.Id], ct)
            ?? throw new InvalidOperationException($"Location {dto.Id} not found.");

        location.Name = dto.Name.Trim();
        location.CityName = dto.CityName.Trim();
        location.IsSystemWide = dto.IsSystemWide;
        location.IsActive = dto.IsActive;
        location.CountryId = dto.CountryId;

        await _context.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        _appRuntimeModeService.EnsureWriteAllowed();

        var location = await _context.Locations.FindAsync([id], ct);
        if (location is null || location.IsSystemWide)
        {
            return;
        }

        _context.Locations.Remove(location);
        await _context.SaveChangesAsync(ct);
    }

    private static LocationDto MapToDto(Location location) => new()
    {
        Id = location.Id,
        Name = location.Name,
        CityName = location.CityName,
        IsSystemWide = location.IsSystemWide,
        IsActive = location.IsActive,
        CountryId = location.CountryId,
        CountryCode = location.Country?.Code,
        CountryName = location.Country?.Name
    };
}
