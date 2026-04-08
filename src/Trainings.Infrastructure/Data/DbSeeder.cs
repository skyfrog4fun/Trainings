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
            var superAdmin = new User
            {
                FirstName = "Super",
                LastName = "Admin",
                Email = "superadmin@trainings.app",
                PasswordHash = _passwordHasher.Hash("Admin123!"),
                Role = UserRole.SuperAdmin,
                Gender = Gender.Other,
                IsActive = true,
                EmailConfirmedAt = DateTime.UtcNow,
                CreationDate = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };
            _context.Users.Add(superAdmin);

            var admin = new User
            {
                FirstName = "Administrator",
                LastName = string.Empty,
                Email = "admin@trainings.local",
                PasswordHash = _passwordHasher.Hash("Admin123!"),
                Role = UserRole.Admin,
                Gender = Gender.Other,
                IsActive = true,
                EmailConfirmedAt = DateTime.UtcNow,
                CreationDate = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };
            _context.Users.Add(admin);

            var trainer = new User
            {
                FirstName = "Demo",
                LastName = "Trainer",
                Email = "trainer@trainings.local",
                PasswordHash = _passwordHasher.Hash("Trainer123!"),
                Role = UserRole.Trainer,
                Gender = Gender.Male,
                IsActive = true,
                EmailConfirmedAt = DateTime.UtcNow,
                CreationDate = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };
            _context.Users.Add(trainer);

            var participant = new User
            {
                FirstName = "Demo",
                LastName = "Participant",
                Email = "participant@trainings.local",
                PasswordHash = _passwordHasher.Hash("Part123!"),
                Role = UserRole.Participant,
                Gender = Gender.Female,
                IsActive = true,
                EmailConfirmedAt = DateTime.UtcNow,
                CreationDate = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };
            _context.Users.Add(participant);

            await _context.SaveChangesAsync();
        }

        if (!_context.Groups.Any())
        {
            var group = new Group
            {
                Name = "General",
                Description = "Default training group",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            _context.Groups.Add(group);
            await _context.SaveChangesAsync();
        }
    }
}
