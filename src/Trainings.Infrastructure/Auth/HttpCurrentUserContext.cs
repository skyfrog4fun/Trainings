using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Trainings.Application.Interfaces;

namespace Trainings.Infrastructure.Auth;

public class HttpCurrentUserContext(IHttpContextAccessor httpContextAccessor) : ICurrentUserContext
{
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

    public int? GetCurrentUserId()
    {
        string? userIdClaim = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out int userId) && userId > 0 ? userId : null;
    }
}
