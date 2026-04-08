using Microsoft.EntityFrameworkCore;
using Trainings.Application.DTOs;
using Trainings.Application.Interfaces;
using Trainings.Domain.Entities;
using Trainings.Infrastructure.Data;

namespace Trainings.Infrastructure.Services;

public class TagService : ITagService
{
    private readonly ApplicationDbContext _context;

    public TagService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<TagDto>> GetAllAsync(CancellationToken ct = default)
    {
        var tags = await _context.Tags.OrderBy(t => t.Name).ToListAsync(ct);
        return tags.Select(MapToDto);
    }

    public async Task<IEnumerable<TagDto>> GetByGroupAsync(int? groupId, CancellationToken ct = default)
    {
        var query = _context.Tags.AsQueryable();
        query = groupId.HasValue
            ? query.Where(t => t.GroupId == groupId)
            : query.Where(t => t.GroupId == null);
        var tags = await query.OrderBy(t => t.Name).ToListAsync(ct);
        return tags.Select(MapToDto);
    }

    public async Task<TagDto> CreateAsync(CreateTagDto dto, CancellationToken ct = default)
    {
        var tag = new Tag
        {
            Name = dto.Name,
            GroupId = dto.GroupId
        };
        _context.Tags.Add(tag);
        await _context.SaveChangesAsync(ct);
        return MapToDto(tag);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var tag = await _context.Tags.FindAsync([id], ct);
        if (tag != null)
        {
            _context.Tags.Remove(tag);
            await _context.SaveChangesAsync(ct);
        }
    }

    private static TagDto MapToDto(Tag tag) => new()
    {
        Id = tag.Id,
        Name = tag.Name,
        GroupId = tag.GroupId
    };
}
