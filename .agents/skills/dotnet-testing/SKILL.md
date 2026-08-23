---
name: dotnet-testing
description: >-
  Templates and standards for writing unit and integration tests using xUnit, Moq, and FluentAssertions in .NET 9.
  Use when creating test suites, writing unit tests for matchmaking/services, or setting up test fixtures.
---

# .NET Testing Guidelines (xUnit + Moq + FluentAssertions)

## 1. Структура теста (Arrange-Act-Assert)
Каждый тест должен следовать паттерну AAA:

```csharp
[Fact]
public async Task Should_DetectMutualMatch_When_BothUsersRatedSixOrHigher()
{
    // Arrange
    const long user1TelegramId = 11111;
    var user2ProfileId = Guid.NewGuid();
    
    // ... setup mocks for IProfileRatingRepository, IUserRepository, etc.

    // Act
    var result = await searchService.RateCandidateAsync(user1TelegramId, user2ProfileId, 8, CancellationToken.None);

    // Assert
    result.IsSuccess.Should().BeTrue();
    result.Value.IsMutualMatch.Should().BeTrue();
}
```

## 2. Именование тестов
Используйте соглашение `Should_ExpectedBehavior_When_StateUnderTest`:
- `Should_ReturnValidationError_When_AgeIsBelow10`
- `Should_RankTier1AiCompatibilityHigher_When_MultipleCandidatesExist`
- `Should_AnswerCallbackQuery_When_InlineButtonClicked`
- `Should_ExcludeReportedProfiles_When_SearchingCandidates`

## 3. Команды запуска тестов
```powershell
# Запуск всех тестов в решении (221 тест)
dotnet test DatingBot.sln

# Запуск только Unit тестов
dotnet test tests/DatingBot.UnitTests/DatingBot.UnitTests.csproj

# Запуск только Integration тестов
dotnet test tests/DatingBot.IntegrationTests/DatingBot.IntegrationTests.csproj
```

