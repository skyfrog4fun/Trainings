using Microsoft.EntityFrameworkCore;
using Trainings.Domain.Entities;

namespace Trainings.Infrastructure.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Training> Trainings => Set<Training>();
    public DbSet<Registration> Registrations => Set<Registration>();
    public DbSet<Attendance> Attendances => Set<Attendance>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.Id);
            entity.HasIndex(u => u.Email).IsUnique();
            entity.Property(u => u.Email).IsRequired().HasMaxLength(256);
            entity.Property(u => u.Name).IsRequired().HasMaxLength(200);
            entity.Property(u => u.PasswordHash).IsRequired();
        });

        modelBuilder.Entity<Training>(entity =>
        {
            entity.HasKey(t => t.Id);
            entity.Property(t => t.Title).IsRequired().HasMaxLength(200);
            entity.HasOne(t => t.Trainer)
                .WithMany(u => u.TrainingsAsTrainer)
                .HasForeignKey(t => t.TrainerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Registration>(entity =>
        {
            entity.HasKey(r => r.Id);
            entity.HasIndex(r => new { r.UserId, r.TrainingId }).IsUnique();
            entity.HasOne(r => r.User)
                .WithMany(u => u.Registrations)
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(r => r.Training)
                .WithMany(t => t.Registrations)
                .HasForeignKey(r => r.TrainingId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Attendance>(entity =>
        {
            entity.HasKey(a => a.Id);
            entity.HasIndex(a => new { a.UserId, a.TrainingId }).IsUnique();
            entity.HasOne(a => a.User)
                .WithMany(u => u.Attendances)
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(a => a.Training)
                .WithMany(t => t.Attendances)
                .HasForeignKey(a => a.TrainingId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
