using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Trainings.Application.Interfaces;
using Trainings.Domain.Entities;
using Trainings.Domain.Enums;

namespace Trainings.Infrastructure.Data;

public class DbSeeder
{
    private readonly ApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IConfiguration _configuration;

    public DbSeeder(ApplicationDbContext context, IPasswordHasher passwordHasher, IConfiguration configuration)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _configuration = configuration;
    }

    public async Task SeedAsync()
    {
        await _context.Database.MigrateAsync();

        if (!_context.Users.Any())
        {
            var email = _configuration["Seed:Email"] ?? "superadmin@trainings.app";
            var password = _configuration["Seed:Password"] ?? "Admin123!";

            var superAdmin = new User
            {
                FirstName = "Super",
                LastName = "Admin",
                Email = email,
                PasswordHash = _passwordHasher.Hash(password),
                Role = UserRole.SuperAdmin,
                Gender = Gender.Other,
                IsActive = true,
                EmailConfirmedAt = DateTime.UtcNow,
                CreationDate = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };
            _context.Users.Add(superAdmin);
            await _context.SaveChangesAsync();
        }
    }
}
