using FluentAssertions;
using Microsoft.Data.Sqlite;
using Moq;
using Trainings.Application.DTOs;
using Trainings.Application.Interfaces;
using Trainings.Domain.Entities;
using Trainings.Domain.Interfaces;
using Trainings.Infrastructure.Data;
using Trainings.Infrastructure.Services;

namespace Trainings.Application.Tests.Services;

public class TrainingServiceTests
{
    [Fact]
    public async Task GetByIdAsyncMapsExecutionBlocksWithResolvedTagAndGameTexts()
    {
        using var cultureScope = new CultureScope("de-DE");
        await using var scope = CreateServiceScope();

        var tag = new Tag { Id = 3, Key = "game", ColorToken = "--brand-innovation", DisplayOrder = 4, IsActive = true };
        var game = new Game { Id = 9, IsActive = true, IsApproved = true, CreatedAt = DateTime.UtcNow };
        var creator = new User { Id = 17, FirstName = "Ava", LastName = "Coach", Email = "ava@example.com", PasswordHash = "hash" };
        var definition = new TrainingBlockDefinition
        {
            Id = 21,
            Title = "Library block",
            DurationMinutes = 15,
            TagId = tag.Id,
            Tag = tag,
            GameId = game.Id,
            Game = game,
            MinParticipants = 5,
            MaxParticipants = 12,
            CreatorId = creator.Id,
            Creator = creator,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        var training = new Training
        {
            Id = 7,
            Title = "Session",
            Description = "Desc",
            DateTime = DateTime.UtcNow,
            Capacity = 20,
            GroupId = 4,
            Group = new Group { Id = 4, Name = "Falcons", Slug = "falcons", Identifier = "falcons" },
            Blocks =
            [
                new TrainingBlock
                {
                    Id = 11,
                    TrainingId = 7,
                    OrderIndex = 1,
                    DefinitionId = definition.Id,
                    Definition = definition,
                    Title = "Adjusted title",
                    Description = "Adjusted description",
                    PlannedDurationMinutes = 18,
                    MinParticipants = 6,
                    MaxParticipants = 10,
                    CreatedAt = DateTime.UtcNow
                }
            ]
        };

        scope.TrainingRepositoryMock.Setup(repository => repository.GetByIdAsync(training.Id)).ReturnsAsync(training);
        scope.TranslationServiceMock
            .Setup(service => service.GetTextLookupAsync(Trainings.Domain.Enums.TranslationEntityType.Tag, It.IsAny<IEnumerable<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<int, TranslationTextsDto>
            {
                [tag.Id] = new() { EnglishText = "Game", GermanText = "Spiel", CurrentText = "Spiel" }
            });
        scope.TranslationServiceMock
            .Setup(service => service.GetTextLookupAsync(Trainings.Domain.Enums.TranslationEntityType.Game, It.IsAny<IEnumerable<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<int, TranslationTextsDto>
            {
                [game.Id] = new() { EnglishText = "Soccer", GermanText = "Fussball", CurrentText = "Fussball" }
            });

        var result = await scope.Service.GetByIdAsync(training.Id);

        result.Should().NotBeNull();
        result!.Blocks.Should().ContainSingle();
        result.Blocks[0].Title.Should().Be("Adjusted title");
        result.Blocks[0].TagDisplayText.Should().Be("Spiel");
        result.Blocks[0].GameDisplayText.Should().Be("Fussball");
        result.Blocks[0].TagColorToken.Should().Be("--brand-innovation");
    }

    [Fact]
    public async Task GetAllAsyncLeavesGameDisplayEmptyForNonGameBlocks()
    {
        await using var scope = CreateServiceScope();

        var tag = new Tag { Id = 1, Key = "warm-up", ColorToken = "--brand-community", DisplayOrder = 1, IsActive = true };
        var creator = new User { Id = 19, FirstName = "Mia", LastName = "Trainer", Email = "mia@example.com", PasswordHash = "hash" };
        var definition = new TrainingBlockDefinition
        {
            Id = 30,
            Title = "Mobility",
            DurationMinutes = 10,
            TagId = tag.Id,
            Tag = tag,
            MinParticipants = 4,
            MaxParticipants = 12,
            CreatorId = creator.Id,
            Creator = creator,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        var training = new Training
        {
            Id = 8,
            Title = "Warm-up session",
            Description = "Desc",
            DateTime = DateTime.UtcNow,
            Capacity = 16,
            GroupId = 5,
            Group = new Group { Id = 5, Name = "Owls", Slug = "owls", Identifier = "owls" },
            Blocks =
            [
                new TrainingBlock
                {
                    Id = 12,
                    TrainingId = 8,
                    OrderIndex = 1,
                    DefinitionId = definition.Id,
                    Definition = definition,
                    Title = "Mobility",
                    PlannedDurationMinutes = 10,
                    MinParticipants = 4,
                    MaxParticipants = 12,
                    CreatedAt = DateTime.UtcNow
                }
            ]
        };

        scope.TrainingRepositoryMock.Setup(repository => repository.GetAllAsync()).ReturnsAsync([training]);
        scope.TranslationServiceMock
            .Setup(service => service.GetTextLookupAsync(Trainings.Domain.Enums.TranslationEntityType.Tag, It.IsAny<IEnumerable<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<int, TranslationTextsDto>
            {
                [tag.Id] = new() { EnglishText = "Warm-Up", GermanText = "Warm-Up", CurrentText = "Warm-Up" }
            });
        scope.TranslationServiceMock
            .Setup(service => service.GetTextLookupAsync(Trainings.Domain.Enums.TranslationEntityType.Game, It.IsAny<IEnumerable<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<int, TranslationTextsDto>());

        var result = (await scope.Service.GetAllAsync()).ToList();

        result.Should().ContainSingle();
        result[0].Blocks.Should().ContainSingle();
        result[0].Blocks[0].GameDisplayText.Should().BeNull();
        result[0].Blocks[0].TagDisplayText.Should().Be("Warm-Up");
    }

    private static TrainingServiceScope CreateServiceScope()
    {
        var (connection, context) = TrainingBlockTestData.CreateContext();
        var trainingRepositoryMock = new Mock<ITrainingRepository>();
        var registrationRepositoryMock = new Mock<IRegistrationRepository>();
        var runtimeModeMock = TrainingBlockTestData.CreateRuntimeModeMock();
        var dateTimeFormatServiceMock = new Mock<IDateTimeFormatService>();
        var translationServiceMock = new Mock<ITranslationService>();

        var service = new TrainingService(
            trainingRepositoryMock.Object,
            registrationRepositoryMock.Object,
            context,
            runtimeModeMock.Object,
            dateTimeFormatServiceMock.Object,
            translationServiceMock.Object);

        return new TrainingServiceScope(connection, context, service, trainingRepositoryMock, translationServiceMock);
    }

    private sealed class TrainingServiceScope(
        SqliteConnection connection,
        ApplicationDbContext context,
        TrainingService service,
        Mock<ITrainingRepository> trainingRepositoryMock,
        Mock<ITranslationService> translationServiceMock) : IAsyncDisposable
    {
        public SqliteConnection Connection { get; } = connection;
        public ApplicationDbContext Context { get; } = context;
        public TrainingService Service { get; } = service;
        public Mock<ITrainingRepository> TrainingRepositoryMock { get; } = trainingRepositoryMock;
        public Mock<ITranslationService> TranslationServiceMock { get; } = translationServiceMock;

        public async ValueTask DisposeAsync()
        {
            await Context.DisposeAsync();
            await Connection.DisposeAsync();
        }
    }

    private sealed class CultureScope : IDisposable
    {
        private readonly System.Globalization.CultureInfo _originalCulture = System.Globalization.CultureInfo.CurrentCulture;
        private readonly System.Globalization.CultureInfo _originalUiCulture = System.Globalization.CultureInfo.CurrentUICulture;

        public CultureScope(string cultureName)
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
