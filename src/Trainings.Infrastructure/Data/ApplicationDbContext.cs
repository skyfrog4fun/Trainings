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
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();
    public DbSet<EmailConfirmationToken> EmailConfirmationTokens => Set<EmailConfirmationToken>();
    public DbSet<Group> Groups => Set<Group>();
    public DbSet<GroupMembership> GroupMemberships => Set<GroupMembership>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<TrainingBlock> TrainingBlocks => Set<TrainingBlock>();
    public DbSet<TrainingBlockTag> TrainingBlockTags => Set<TrainingBlockTag>();
    public DbSet<MailConfiguration> MailConfigurations => Set<MailConfiguration>();
    public DbSet<GroupMailConfiguration> GroupMailConfigurations => Set<GroupMailConfiguration>();
    public DbSet<NotificationLog> NotificationLogs => Set<NotificationLog>();
    public DbSet<SlugRedirect> SlugRedirects => Set<SlugRedirect>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.Id);
            entity.HasIndex(u => u.Email).IsUnique();
            entity.Property(u => u.Email).IsRequired().HasMaxLength(256);
            entity.Property(u => u.FirstName).IsRequired().HasMaxLength(100);
            entity.Property(u => u.LastName).IsRequired().HasMaxLength(100);
            entity.Property(u => u.PasswordHash).IsRequired();
            entity.Property(u => u.Mobile).HasMaxLength(50);
            entity.Property(u => u.City).HasMaxLength(100);
            entity.Property(u => u.WelcomeMessage).HasMaxLength(500);
            entity.Ignore(u => u.DisplayName);
        });

        modelBuilder.Entity<Training>(entity =>
        {
            entity.HasKey(t => t.Id);
            entity.Property(t => t.Title).IsRequired().HasMaxLength(200);
            entity.HasOne(t => t.Trainer)
                .WithMany(u => u.TrainingsAsTrainer)
                .HasForeignKey(t => t.TrainerId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(t => t.Group)
                .WithMany(g => g.Trainings)
                .HasForeignKey(t => t.GroupId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);
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

        modelBuilder.Entity<PasswordResetToken>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Token).IsRequired().HasMaxLength(256);
            entity.HasOne(p => p.User)
                .WithMany(u => u.PasswordResetTokens)
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<EmailConfirmationToken>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Token).IsRequired().HasMaxLength(256);
            entity.HasOne(e => e.User)
                .WithMany(u => u.EmailConfirmationTokens)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Group>(entity =>
        {
            entity.HasKey(g => g.Id);
            entity.Property(g => g.Name).IsRequired().HasMaxLength(200);
            entity.Property(g => g.Slug).IsRequired().HasMaxLength(200);
            entity.HasIndex(g => g.Slug).IsUnique();
            entity.Property(g => g.Identifier).IsRequired().HasMaxLength(50);
            entity.HasIndex(g => g.Identifier).IsUnique();
            entity.Property(g => g.Description).HasMaxLength(500);
        });

        modelBuilder.Entity<GroupMembership>(entity =>
        {
            entity.HasKey(gm => gm.Id);
            entity.HasOne(gm => gm.User)
                .WithMany(u => u.GroupMemberships)
                .HasForeignKey(gm => gm.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(gm => gm.Group)
                .WithMany(g => g.Memberships)
                .HasForeignKey(gm => gm.GroupId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Tag>(entity =>
        {
            entity.HasKey(t => t.Id);
            entity.Property(t => t.Name).IsRequired().HasMaxLength(100);
            entity.HasOne(t => t.Group)
                .WithMany()
                .HasForeignKey(t => t.GroupId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<TrainingBlock>(entity =>
        {
            entity.HasKey(b => b.Id);
            entity.Property(b => b.Title).IsRequired().HasMaxLength(200);
            entity.HasOne(b => b.Training)
                .WithMany(t => t.Blocks)
                .HasForeignKey(b => b.TrainingId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(b => b.SourceBlock)
                .WithMany()
                .HasForeignKey(b => b.SourceBlockId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<TrainingBlockTag>(entity =>
        {
            entity.HasKey(bt => new { bt.TrainingBlockId, bt.TagId });
            entity.HasOne(bt => bt.TrainingBlock)
                .WithMany(b => b.TrainingBlockTags)
                .HasForeignKey(bt => bt.TrainingBlockId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(bt => bt.Tag)
                .WithMany(t => t.TrainingBlockTags)
                .HasForeignKey(bt => bt.TagId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<MailConfiguration>(entity =>
        {
            entity.HasKey(mc => mc.Id);
            entity.Property(mc => mc.Name).IsRequired().HasMaxLength(200);
            entity.Property(mc => mc.Host).IsRequired().HasMaxLength(200);
            entity.Property(mc => mc.Username).IsRequired().HasMaxLength(200);
            entity.Property(mc => mc.Password).IsRequired().HasMaxLength(500);
            entity.Property(mc => mc.FromAddress).IsRequired().HasMaxLength(256);
            entity.HasIndex(mc => mc.Priority).IsUnique();
        });

        modelBuilder.Entity<GroupMailConfiguration>(entity =>
        {
            entity.HasKey(gmc => gmc.Id);
            entity.HasOne(gmc => gmc.Group)
                .WithMany(g => g.MailConfigurations)
                .HasForeignKey(gmc => gmc.GroupId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(gmc => gmc.MailConfiguration)
                .WithMany(mc => mc.GroupMailConfigurations)
                .HasForeignKey(gmc => gmc.MailConfigurationId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(gmc => new { gmc.GroupId, gmc.Priority }).IsUnique();
        });

        modelBuilder.Entity<NotificationLog>(entity =>
        {
            entity.HasKey(nl => nl.Id);
            entity.Property(nl => nl.AttemptId).HasColumnType("TEXT");
            entity.Property(nl => nl.RecipientEmail).IsRequired().HasMaxLength(256);
            entity.Property(nl => nl.ErrorMessage).HasMaxLength(2000);
            entity.HasOne(nl => nl.User)
                .WithMany()
                .HasForeignKey(nl => nl.UserId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(nl => nl.MailConfiguration)
                .WithMany(mc => mc.NotificationLogs)
                .HasForeignKey(nl => nl.MailConfigurationId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(nl => nl.Group)
                .WithMany()
                .HasForeignKey(nl => nl.GroupId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<SlugRedirect>(entity =>
        {
            entity.HasKey(sr => sr.Id);
            entity.Property(sr => sr.OldSlug).IsRequired().HasMaxLength(200);
            entity.Property(sr => sr.NewSlug).IsRequired().HasMaxLength(200);
            entity.Property(sr => sr.EntityType).IsRequired().HasMaxLength(100);
            entity.HasIndex(sr => new { sr.EntityType, sr.OldSlug });
        });
    }
}
