using Microsoft.Extensions.DependencyInjection;
using Trainings.Application.Interfaces;
using Trainings.Application.Services;

namespace Trainings.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IRegistrationService, RegistrationService>();
        services.AddScoped<IAttendanceService, AttendanceService>();
        return services;
    }
}
