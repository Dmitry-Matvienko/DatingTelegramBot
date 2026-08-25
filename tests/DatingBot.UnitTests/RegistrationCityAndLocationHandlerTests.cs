using DatingBot.Application.Common;
using DatingBot.Application.DTOs;
using DatingBot.Application.Interfaces;
using DatingBot.Application.Services;
using DatingBot.Bot.Handlers;
using DatingBot.Bot.Services;
using DatingBot.Domain.Entities;
using DatingBot.Domain.Enums;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Telegram.Bot;
using Telegram.Bot.Requests;
using Telegram.Bot.Requests.Abstractions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using Xunit;
using DbUser = DatingBot.Domain.Entities.User;

namespace DatingBot.UnitTests;

public class RegistrationCityAndLocationHandlerTests
{
    private readonly Mock<ITelegramBotClient> _botClientMock = new();
    private readonly Mock<IRegistrationService> _registrationServiceMock = new();
    private readonly Mock<IProfileEditingService> _editingServiceMock = new();
    private readonly Mock<ICityRepository> _cityRepositoryMock = new();
    private readonly Mock<IGeocodingService> _geocodingServiceMock = new();
    private readonly Mock<IUserRepository> _userRepositoryMock = new();
    private readonly ILocalizationService _loc = new LocalizationService();

    private readonly RegistrationPromptService _promptService;
    private readonly ProfilePromptService _profilePromptService;
    private readonly RegistrationMessageHandler _regHandler;
    private readonly ProfileEditMessageHandler _editHandler;

    public RegistrationCityAndLocationHandlerTests()
    {
        _promptService = new RegistrationPromptService(
            _botClientMock.Object,
            _registrationServiceMock.Object,
            _userRepositoryMock.Object,
            _loc,
            NullLogger<RegistrationPromptService>.Instance
        );

        _profilePromptService = new ProfilePromptService(
            _botClientMock.Object,
            _registrationServiceMock.Object,
            _userRepositoryMock.Object,
            _loc,
            _promptService,
            NullLogger<ProfilePromptService>.Instance
        );

        _regHandler = new RegistrationMessageHandler(
            _botClientMock.Object,
            _registrationServiceMock.Object,
            _cityRepositoryMock.Object,
            _geocodingServiceMock.Object,
            _promptService,
            _loc
        );

        _editHandler = new ProfileEditMessageHandler(
            _botClientMock.Object,
            _editingServiceMock.Object,
            _registrationServiceMock.Object,
            _cityRepositoryMock.Object,
            _geocodingServiceMock.Object,
            _profilePromptService,
            _promptService,
            _loc
        );

        _botClientMock
            .Setup(c => c.SendRequest(It.IsAny<SendMessageRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Message { Id = 999 });

        _botClientMock
            .Setup(c => c.SendRequest(It.IsAny<DeleteMessageRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
    }

    [Fact]
    public async Task HandleMessageAsync_CityTypo_ShouldSendSuggestionsAndFollowUpLocationPrompt()
    {
        // Arrange
        var user = new DbUser { TelegramId = 12345, State = UserState.Registration_WaitingForCity, Language = AppLanguage.Russian };
        var message = new Message
        {
            Id = 10,
            Chat = new Chat { Id = 12345 },
            Text = "Масква"
        };

        _cityRepositoryMock
            .Setup(r => r.FindExactByNameAsync("Масква", It.IsAny<CancellationToken>()))
            .ReturnsAsync((City?)null);

        var suggestions = new List<City>
        {
            new() { Id = 1, Name = "Москва", Region = "Московская область", Country = "Россия" },
            new() { Id = 2, Name = "Моздок", Region = "Северная Осетия", Country = "Россия" }
        };

        _cityRepositoryMock
            .Setup(r => r.SearchSuggestionsAsync("Масква", 3, It.IsAny<CancellationToken>()))
            .ReturnsAsync(suggestions);

        var sentMessages = new List<SendMessageRequest>();
        _botClientMock
            .Setup(c => c.SendRequest(It.IsAny<IRequest<Message>>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<Message>, CancellationToken>((req, _) =>
            {
                if (req is SendMessageRequest smr)
                {
                    sentMessages.Add(smr);
                }
            })
            .ReturnsAsync(new Message { Id = 999 });

        // Act
        await _regHandler.HandleMessageAsync(user, message);

        // Assert:
        // 1. First message should be suggestions with inline keyboard
        sentMessages.Should().HaveCount(2);
        sentMessages[0].ReplyMarkup.Should().BeOfType<InlineKeyboardMarkup>();
        var inlineMarkup = (InlineKeyboardMarkup)sentMessages[0].ReplyMarkup!;
        inlineMarkup.InlineKeyboard.Should().HaveCount(3); // 2 cities + 1 cancel

        // 2. Second message should be notice with ReplyKeyboardMarkup requesting location
        sentMessages[1].ReplyMarkup.Should().BeOfType<ReplyKeyboardMarkup>();
        var replyMarkup = (ReplyKeyboardMarkup)sentMessages[1].ReplyMarkup!;
        replyMarkup.Keyboard.First().First().RequestLocation.Should().BeTrue();
    }

    [Fact]
    public async Task HandleMessageAsync_CityNotFound_ShouldSendNoticeWithLocationPrompt()
    {
        // Arrange
        var user = new DbUser { TelegramId = 12345, State = UserState.Registration_WaitingForCity, Language = AppLanguage.Russian };
        var message = new Message
        {
            Id = 10,
            Chat = new Chat { Id = 12345 },
            Text = "НеизвестныйГород123"
        };

        _cityRepositoryMock
            .Setup(r => r.FindExactByNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((City?)null);

        _cityRepositoryMock
            .Setup(r => r.SearchSuggestionsAsync(It.IsAny<string>(), 3, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<City>());

        var sentMessages = new List<SendMessageRequest>();
        _botClientMock
            .Setup(c => c.SendRequest(It.IsAny<IRequest<Message>>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<Message>, CancellationToken>((req, _) =>
            {
                if (req is SendMessageRequest smr)
                {
                    sentMessages.Add(smr);
                }
            })
            .ReturnsAsync(new Message { Id = 999 });

        // Act
        await _regHandler.HandleMessageAsync(user, message);

        // Assert
        sentMessages.Should().HaveCount(1);
        sentMessages[0].ReplyMarkup.Should().BeOfType<ReplyKeyboardMarkup>();
        var replyMarkup = (ReplyKeyboardMarkup)sentMessages[0].ReplyMarkup!;
        replyMarkup.Keyboard.First().First().RequestLocation.Should().BeTrue();
    }

    [Fact]
    public async Task HandleMessageAsync_LocationSent_WhenCityNotInDb_ShouldSaveCityAndProceed()
    {
        // Arrange
        var user = new DbUser { TelegramId = 12345, State = UserState.Registration_WaitingForCity, Language = AppLanguage.Russian };
        var message = new Message
        {
            Id = 10,
            Chat = new Chat { Id = 12345 },
            Location = new Location { Latitude = 55.7558, Longitude = 37.6173 }
        };

        _geocodingServiceMock
            .Setup(g => g.ReverseGeocodeAsync(55.7558, 37.6173, AppLanguage.Russian, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GeocodingLocation("Новокосино", "Московская область", "Россия", 55.7558, 37.6173));

        _cityRepositoryMock
            .Setup(r => r.FindExactByNameAsync("Новокосино", It.IsAny<CancellationToken>()))
            .ReturnsAsync((City?)null);

        _cityRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<City>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new City { Id = 50, Name = "Новокосино" });

        _registrationServiceMock
            .Setup(s => s.SetCityAsync(12345, "Новокосино", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        // Act
        await _regHandler.HandleMessageAsync(user, message);

        // Assert
        _cityRepositoryMock.Verify(r => r.AddAsync(It.Is<City>(c => c.Name == "Новокосино"), It.IsAny<CancellationToken>()), Times.Once);
        _registrationServiceMock.Verify(s => s.SetCityAsync(12345, "Новокосино", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleMessageAsync_LocationSent_WhenCityAlreadyInDb_ShouldNotAddDuplicate()
    {
        // Arrange
        var user = new DbUser { TelegramId = 12345, State = UserState.Registration_WaitingForCity, Language = AppLanguage.Russian };
        var message = new Message
        {
            Id = 10,
            Chat = new Chat { Id = 12345 },
            Location = new Location { Latitude = 55.7558, Longitude = 37.6173 }
        };

        _geocodingServiceMock
            .Setup(g => g.ReverseGeocodeAsync(55.7558, 37.6173, AppLanguage.Russian, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GeocodingLocation("Москва", "Москва", "Россия", 55.7558, 37.6173));

        _cityRepositoryMock
            .Setup(r => r.FindExactByNameAsync("Москва", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new City { Id = 1, Name = "Москва" });

        _registrationServiceMock
            .Setup(s => s.SetCityAsync(12345, "Москва", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        // Act
        await _regHandler.HandleMessageAsync(user, message);

        // Assert
        _cityRepositoryMock.Verify(r => r.AddAsync(It.IsAny<City>(), It.IsAny<CancellationToken>()), Times.Never);
        _registrationServiceMock.Verify(s => s.SetCityAsync(12345, "Москва", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleEditMessageAsync_LocationSent_ShouldUpdateProfileCity()
    {
        // Arrange
        var user = new DbUser { TelegramId = 12345, State = UserState.Editing_City, Language = AppLanguage.Russian };
        var message = new Message
        {
            Id = 10,
            Chat = new Chat { Id = 12345 },
            Location = new Location { Latitude = 59.9343, Longitude = 30.3351 }
        };

        _geocodingServiceMock
            .Setup(g => g.ReverseGeocodeAsync(59.9343, 30.3351, AppLanguage.Russian, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GeocodingLocation("Санкт-Петербург", "Санкт-Петербург", "Россия", 59.9343, 30.3351));

        _cityRepositoryMock
            .Setup(r => r.FindExactByNameAsync("Санкт-Петербург", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new City { Id = 2, Name = "Санкт-Петербург" });

        _editingServiceMock
            .Setup(s => s.UpdateCityAsync(12345, "Санкт-Петербург", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        _registrationServiceMock
            .Setup(s => s.GetProfileDtoAsync(12345, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserProfileDto(
                Id: Guid.NewGuid(),
                TelegramId: 12345,
                Username: "ivan",
                Gender: Gender.Male,
                TargetGender: TargetGender.Female,
                Name: "Иван",
                Age: 25,
                City: "Санкт-Петербург",
                Height: 180,
                PhotoFileId: "photo_123",
                DatingTarget: DatingTarget.Relationship,
                AiDescription: "AI bio",
                SelectedInterests: [],
                IsCompleted: true
            ));

        // Act
        var handled = await _editHandler.HandleEditMessageAsync(user, message);

        // Assert
        handled.Should().BeTrue();
        _editingServiceMock.Verify(s => s.UpdateCityAsync(12345, "Санкт-Петербург", It.IsAny<CancellationToken>()), Times.Once);
    }
}
