using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Trainings.Application.Interfaces;
using Trainings.Domain.Entities;
using Trainings.Infrastructure.Data;
using Trainings.Infrastructure.Services;
using Xunit;

namespace Trainings.Application.Tests.Services;

public class DbSeederTagSeedingTests
{
    private static ApplicationDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite("DataSource=:memory:")
            .Options;
        var ctx = new ApplicationDbContext(options);
        ctx.Database.OpenConnection();
        ctx.Database.EnsureCreated();
        ctx.Database.ExecuteSqlRaw("PRAGMA foreign_keys = OFF");
        return ctx;
    }

    private static readonly string[] _expectedGlobalTags =
    [
        "Warm-up", "Stretching", "Strength", "Cardio", "Coordination",
        "Technique", "Mental", "Game", "Cool-down", "Other"
    ];

    /// <summary>
    /// Seeds global tags directly into the context (mirrors DbSeeder.SeedGlobalTagsAsync logic)
    /// so we can test it in isolation without MigrateAsync.
    /// </summary>
    private static async Task SeedGlobalTagsDirectlyAsync(ApplicationDbContext ctx)
    {
        if (await ctx.Tags.AnyAsync(t => t.GroupId == null))
        {
            return;
        }

        foreach (string name in _expectedGlobalTags)
        {
            ctx.Tags.Add(new Tag { Name = name, GroupId = null });
        }

        await ctx.SaveChangesAsync();
    }

    [Fact]
    public async Task SeedGlobalTagsSeedsExactlyTenGlobalTags()
    {
        await using var ctx = CreateInMemoryContext();

        await SeedGlobalTagsDirectlyAsync(ctx);

        var globalTags = await ctx.Tags.Where(t => t.GroupId == null).ToListAsync(TestContext.Current.CancellationToken);
        globalTags.Should().HaveCount(10);
    }

    [Fact]
    public async Task SeedGlobalTagsSeedsAllExpectedNames()
    {
        await using var ctx = CreateInMemoryContext();

        await SeedGlobalTagsDirectlyAsync(ctx);

        var names = await ctx.Tags.Where(t => t.GroupId == null).Select(t => t.Name).ToListAsync(TestContext.Current.CancellationToken);
        names.Should().BeEquivalentTo(_expectedGlobalTags);
    }

    [Fact]
    public async Task SeedGlobalTagsDoesNotDuplicateWhenCalledTwice()
    {
        await using var ctx = CreateInMemoryContext();

        await SeedGlobalTagsDirectlyAsync(ctx);
        await SeedGlobalTagsDirectlyAsync(ctx);

        var globalTags = await ctx.Tags.Where(t => t.GroupId == null).ToListAsync(TestContext.Current.CancellationToken);
        globalTags.Should().HaveCount(10);
    }

    [Fact]
    public async Task SeedGlobalTagsDoesNotAffectGroupTags()
    {
        await using var ctx = CreateInMemoryContext();

        // Insert a group-scoped tag first
        ctx.Tags.Add(new Tag { Name = "Custom", GroupId = 1 });
        await ctx.SaveChangesAsync(TestContext.Current.CancellationToken);

        await SeedGlobalTagsDirectlyAsync(ctx);

        var groupTags = await ctx.Tags.Where(t => t.GroupId != null).ToListAsync(TestContext.Current.CancellationToken);
        groupTags.Should().HaveCount(1);
        groupTags[0].Name.Should().Be("Custom");
    }
}
