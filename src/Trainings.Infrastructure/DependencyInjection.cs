using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Trainings.Application.Interfaces;
using Trainings.Domain.Interfaces;
using Trainings.Infrastructure.Auth;
using Trainings.Infrastructure.Data;
using Trainings.Infrastructure.Repositories;

namespace Trainings.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlite(configuration.GetConnectionString("DefaultConnection")
                ?? "Data Source=trainings.db"));

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ITrainingRepository, TrainingRepository>();
        services.AddScoped<IRegistrationRepository, RegistrationRepository>();
        services.AddScoped<IAttendanceRepository, AttendanceRepository>();
        services.AddScoped<IPasswordHasher, BcryptPasswordHasher>();
        services.AddScoped<DbSeeder>();

        return services;
    }
}
