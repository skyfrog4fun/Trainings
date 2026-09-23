using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Trainings.Application.Constants;
using Trainings.Application.DTOs;
using Trainings.Infrastructure.Services;

namespace Trainings.Application.Tests.Services;

public class TagAdminServiceTests
{
    [Fact]
    public async Task GetActiveForSelectionAsyncReturnsLocalizedOrderedTags()
    {
        using var cultureScope = new TagCultureScope("de-DE");
        await using var scope = await CreateScopeAsync();

        await TrainingBlockTestData.AddTagAsync(scope.Context, scope.TranslationService, "other", "Other", "Sonstiges", TrainingBlockCatalog.ColorTokens.Tradition60, 3, ct: TestContext.Current.CancellationToken);
        await TrainingBlockTestData.AddTagAsync(scope.Context, scope.TranslationService, "warm-up", "Warm-Up", "Warm-Up", TrainingBlockCatalog.ColorTokens.Community, 1, ct: TestContext.Current.CancellationToken);
        await TrainingBlockTestData.AddTagAsync(scope.Context, scope.TranslationService, "inactive", "Inactive", "Inaktiv", TrainingBlockCatalog.ColorTokens.Accent, 2, isActive: false, ct: TestContext.Current.CancellationToken);

        var result = await scope.Service.GetActiveForSelectionAsync(ct: TestContext.Current.CancellationToken);

        result.Select(tag => tag.Name).Should().Equal("Warm-Up", "Sonstiges");
        result.Should().OnlyContain(tag => tag.IsActive);
    }

    [Fact]
    public async Task UpdateAsyncPersistsTranslationsAndMetadata()
    {
        await using var scope = await CreateScopeAsync();
        var tag = await TrainingBlockTestData.AddWarmUpTagAsync(scope.Context, scope.TranslationService, ct: TestContext.Current.CancellationToken);

        await scope.Service.UpdateAsync(new TagAdminDto
        {
            Id = tag.Id,
            Key = "warm-up",
            EnglishText = "Prep",
            GermanText = "Vorbereitung",
            ColorToken = TrainingBlockCatalog.ColorTokens.Accent40,
            DisplayOrder = 7,
            IsActive = false
        }, ct: TestContext.Current.CancellationToken);

        var storedTag = await scope.Context.Tags.SingleAsync(cancellationToken: TestContext.Current.CancellationToken);
        var translations = await scope.Context.Translations.Where(t => t.EntityId == tag.Id).ToListAsync(cancellationToken: TestContext.Current.CancellationToken);

        storedTag.ColorToken.Should().Be(TrainingBlockCatalog.ColorTokens.Accent40);
        storedTag.DisplayOrder.Should().Be(7);
        storedTag.IsActive.Should().BeFalse();
        translations.Should().Contain(t => t.Culture == "en" && t.Text == "Prep");
        translations.Should().Contain(t => t.Culture == "de" && t.Text == "Vorbereitung");
    }

    [Fact]
    public async Task CreateAndToggleAsyncValidatesAllowedColorTokenAndActiveState()
    {
        await using var scope = await CreateScopeAsync();

        var act = async () => await scope.Service.CreateAsync(new TagAdminDto
        {
            Key = "bad",
            EnglishText = "Bad",
            GermanText = "Schlecht",
            ColorToken = "--invalid-token",
            DisplayOrder = 1,
            IsActive = true
        });

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*not allowed*");

        var created = await scope.Service.CreateAsync(new TagAdminDto
        {
            Key = "custom",
            EnglishText = "Custom",
            GermanText = "Benutzerdefiniert",
            ColorToken = TrainingBlockCatalog.ColorTokens.Accent,
            DisplayOrder = 4,
            IsActive = true
        }, ct: TestContext.Current.CancellationToken);

        await scope.Service.DeactivateAsync(created.Id, ct: TestContext.Current.CancellationToken);
        (await scope.Context.Tags.SingleAsync(tag => tag.Id == created.Id, cancellationToken: TestContext.Current.CancellationToken)).IsActive.Should().BeFalse();

        await scope.Service.ReactivateAsync(created.Id, ct: TestContext.Current.CancellationToken);
        (await scope.Context.Tags.SingleAsync(tag => tag.Id == created.Id, cancellationToken: TestContext.Current.CancellationToken)).IsActive.Should().BeTrue();
    }

    private static async Task<TagAdminServiceScope> CreateScopeAsync()
    {
        var (connection, context) = TrainingBlockTestData.CreateContext();
        var translationService = TrainingBlockTestData.CreateTranslationService(context);
        var runtimeModeMock = TrainingBlockTestData.CreateRuntimeModeMock();
        var service = new TagAdminService(context, translationService, runtimeModeMock.Object);

        await Task.CompletedTask;
        return new TagAdminServiceScope(connection, context, translationService, service);
    }

    private sealed class TagAdminServiceScope(
        Microsoft.Data.Sqlite.SqliteConnection connection,
        Trainings.Infrastructure.Data.ApplicationDbContext context,
        TranslationService translationService,
        TagAdminService service) : IAsyncDisposable
    {
        public Microsoft.Data.Sqlite.SqliteConnection Connection { get; } = connection;
        public Trainings.Infrastructure.Data.ApplicationDbContext Context { get; } = context;
        public TranslationService TranslationService { get; } = translationService;
        public TagAdminService Service { get; } = service;

        public async ValueTask DisposeAsync()
        {
            await Context.DisposeAsync();
            await Connection.DisposeAsync();
        }
    }

    private sealed class TagCultureScope : IDisposable
    {
        private readonly System.Globalization.CultureInfo _originalCulture = System.Globalization.CultureInfo.CurrentCulture;
        private readonly System.Globalization.CultureInfo _originalUiCulture = System.Globalization.CultureInfo.CurrentUICulture;

        public TagCultureScope(string cultureName)
        {
            var culture = System.Globalization.CultureInfo.GetCultureInfo(cultureName);
            System.Globalization.CultureInfo.CurrentCulture = culture;
            System.Globalization.CultureInfo.CurrentUICulture = culture;
        }

        public void Dispose()
        {
            System.Globalization.CultureInfo.CurrentCulture = _originalCulture;
            System.Globalization.CultureInfo.CurrentUICulture = _originalUiCulture;
        }
    }
}
