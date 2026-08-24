# Спецификация: Регистрация и FSM (Registration & State Machine)

## 1. Назначение модуля

Модуль управляет диалоговым автоматом (Finite State Machine / FSM) процесса создания и первичного заполнения анкеты пользователя в Telegram.

---

## 2. Контракт сервиса `IRegistrationService`

```csharp
public interface IRegistrationService
{
    Task<User> GetOrCreateUserAsync(long telegramId, string? username, string? firstName, CancellationToken cancellationToken = default);
    Task<Result> SetLanguageAsync(long telegramId, AppLanguage language, CancellationToken cancellationToken = default);
    Task<Result> SetGenderAsync(long telegramId, Gender gender, CancellationToken cancellationToken = default);
    Task<Result> SetTargetGenderAsync(long telegramId, TargetGender targetGender, CancellationToken cancellationToken = default);
    Task<Result> SetNameAsync(long telegramId, string name, CancellationToken cancellationToken = default);
    Task<Result> SetAgeAsync(long telegramId, int age, CancellationToken cancellationToken = default);
    Task<Result> SetCityAsync(long telegramId, string cityName, CancellationToken cancellationToken = default);
    Task<Result> SetHeightAsync(long telegramId, int height, CancellationToken cancellationToken = default);
    Task<Result> SkipHeightAsync(long telegramId, CancellationToken cancellationToken = default);
    Task<Result> SetPhotoAsync(long telegramId, string photoFileId, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<InterestDto>>> ToggleInterestAsync(long telegramId, InterestType interestCode, CancellationToken cancellationToken = default);
    Task<Result> CompleteInterestsAsync(long telegramId, CancellationToken cancellationToken = default);
    Task<Result> SetDatingTargetAsync(long telegramId, DatingTarget target, CancellationToken cancellationToken = default);
    Task<Result> SetAiDescriptionAsync(long telegramId, string description, CancellationToken cancellationToken = default);
    Task<Result<UserProfileDto>> SetSearchDistanceAndCompleteAsync(long telegramId, SearchDistancePreference searchDistance, CancellationToken cancellationToken = default);
    Task<Result<UserProfileDto>> SetAiDescriptionAndCompleteAsync(long telegramId, string description, CancellationToken cancellationToken = default);
    Task<UserProfileDto?> GetProfileDtoAsync(long telegramId, CancellationToken cancellationToken = default);
    Task SaveLastBotMessageIdAsync(long telegramId, int messageId, CancellationToken cancellationToken = default);
}
```

---

## 3. Граф состояний мастера регистрации (FSM Flow)

```
[Start / Language Selection]
            │
            ▼
[Registration_SelectingGender]
            │
            ▼
[Registration_SelectingTargetGender]
            │
            ▼
[Registration_WaitingForName] (Валидация имени: буквы/дефис, 2-50 симв.)
            │
            ▼
[Registration_WaitingForAge] (Валидация возраста: число 10-100)
            │
            ▼
[Registration_WaitingForCity] (Поиск в CityRepository / Подсказки опечаток)
            │
            ▼
[Registration_WaitingForHeight] (Валидация: 100-250 см или кнопка «Пропустить»)
            │
            ▼
[Registration_WaitingForPhoto] (Валидация: Telegram Photo FileId)
            │
            ▼
[Registration_SelectingInterests] (Интерактивный выбор с переключением галочек ✅/❌)
            │
            ▼
[Registration_SelectingTarget] (Валидация: несовершеннолетним запрещен AdultOnly)
            │
            ▼
[Registration_WaitingForAiBio] (Векторизация текста через LocalAiEmbeddingService)
            │
            ▼
[Registration_SelectingSearchDistance] (Выбор дальности поиска: 100км / 500км / страна / без ограничений)
            │
            ▼
        [Active] ──► Готовая анкета и доступ в Главное меню
```

---

## 4. Бизнес-правила и ограничения

1. **Возрастные ограничения**:
   - Пользователям младше 18 лет запрещено выбирать цель `DatingTarget.AdultOnly`. Если возраст редактируется и становится < 18, цель автоматически сбрасывается на `Relationship`.
2. **Безопасность интерфейса**:
   - При каждом шаге текстового ввода сообщение пользователя удаляется (`DeleteMessageSafeAsync`), а предыдущий вопрос бота перезаписывается новым для сохранения чистоты чата.
3. **Автоисправление городов**:
   - Если пользователь ввел город с опечаткой (например, «Москв»), бот предлагает подтвердить найденный город («Возможно, вы имели в виду: Москва?») через инлайн-кнопку.
