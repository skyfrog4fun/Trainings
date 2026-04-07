using Trainings.Application.Interfaces;
using Trainings.Domain.Entities;
using Trainings.Domain.Enums;

namespace Trainings.Infrastructure.Data;

public class DbSeeder
{
    private readonly ApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;

    public DbSeeder(ApplicationDbContext context, IPasswordHasher passwordHasher)
    {
        _context = context;
        _passwordHasher = passwordHasher;
    }

    public async Task SeedAsync()
    {
        await _context.Database.EnsureCreatedAsync();

        if (!_context.Users.Any())
        {
            var admin = new User
            {
                Name = "Administrator",
                Email = "admin@trainings.local",
                PasswordHash = _passwordHasher.Hash("Admin123!"),
                Role = UserRole.Admin,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            _context.Users.Add(admin);

            var trainer = new User
            {
                Name = "Demo Trainer",
                Email = "trainer@trainings.local",
                PasswordHash = _passwordHasher.Hash("Trainer123!"),
                Role = UserRole.Trainer,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            _context.Users.Add(trainer);

            var participant = new User
            {
                Name = "Demo Participant",
                Email = "participant@trainings.local",
                PasswordHash = _passwordHasher.Hash("Part123!"),
                Role = UserRole.Participant,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            _context.Users.Add(participant);

            await _context.SaveChangesAsync();
        }
    }
}
