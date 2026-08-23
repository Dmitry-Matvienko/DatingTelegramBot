# Спецификация: Механизм заманчивых напоминаний для неактивных пользователей (Inactivity Reminders)

## 1. Назначение и контекст

Для повышения возвращаемости (retention) и активности пользователей в **DatingBot** внедряется автоматическая система заманчивых push-уведомлений.
Если пользователь не проявлял активности в боте заданное количество дней (`InactivityReminderDays`), бот отправляет ему одно из 10 привлекательных персональных сообщений со ссылкой/кнопкой для мгновенного перехода в режим поиска анкет.

---

## 2. Бизнес-требования и инварианты

1. **Критерии отбора получателей**:
   - Пользователь зарегистрирован и завершил анкету (`UserProfile.IsCompleted == true`).
   - Пользователь не заблокирован (`User.State != UserState.Banned`).
   - С момента последней активности пользователя (`User.LastActiveAt`) прошло не менее `N` дней (`InactivityReminderDays`).
   - Напоминание либо ещё не отправлялось (`User.LastInactivityReminderSentAt == null`), либо с момента предыдущей отправки прошло не менее `N` дней (`LastInactivityReminderSentAt <= DateTime.UtcNow.AddDays(-N)`).

2. **Пул напоминаний (10 вариантов)**:
   - 10 разнообразных, позитивных и интригующих шаблонов с эмодзи.
   - Каждый шаблон полностью переведён на 6 поддерживаемых языков: Русский (`RU`), Украинский (`UK`), Английский (`EN`), Хинди (`HI`), Португальский (`PT`), Индонезийский (`ID`).
   - При каждой отправке шаблон выбирается случайно и равновероятно (1 из 10).

3. **Интерактивность**:
   - К каждому напоминанию прикрепляется инлайн-кнопка `[🔍 Начать поиск]` (`callback_data: "inactivity_search"`).
   - При нажатии бот моментально переводит пользователя в режим поиска анкет (`SearchService`).

4. **Конфигурируемость**:
   - `BotConfiguration:InactivityReminderDays` (int, default: 3) — порог дней неактивности.
   - `BotConfiguration:InactivityCheckIntervalMinutes` (int, default: 60) — интервал периодической проверки воркером.

---

## 3. Модели данных и персистентность

### 3.1. Сущность `User`
```csharp
public class User
{
    // ... существующие поля ...
    public DateTime LastActiveAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastInactivityReminderSentAt { get; set; }
}
```

### 3.2. Конфигурация EF Core и индексы
```csharp
builder.Property(u => u.LastActiveAt)
    .IsRequired()
    .HasDefaultValueSql("GETUTCDATE()");

builder.Property(u => u.LastInactivityReminderSentAt)
    .IsRequired(false);

builder.HasIndex(u => new { u.LastActiveAt, u.LastInactivityReminderSentAt })
    .HasDatabaseName("IX_Users_LastActive_LastReminder");
```

---

## 4. Контракты сервисов

### 4.1. `IInactivityReminderService` (Application)
```csharp
public interface IInactivityReminderService
{
    Task<IReadOnlyList<User>> GetUsersForInactivityReminderAsync(int inactivityDays, int limit = 100, CancellationToken cancellationToken = default);
    Task MarkReminderSentAsync(Guid userId, DateTime sentAt, CancellationToken cancellationToken = default);
    string GetRandomInactivityReminderKey();
}
```

### 4.2. `IUserRepository` (Application / Infrastructure)
```csharp
public interface IUserRepository
{
    // ...
    Task<IReadOnlyList<User>> GetInactiveUsersAsync(DateTime cutoffDate, int limit = 100, CancellationToken cancellationToken = default);
    Task MarkInactivityReminderSentAsync(Guid userId, DateTime sentAt, CancellationToken cancellationToken = default);
    Task UpdateLastActiveAtAsync(long telegramId, DateTime activeAt, CancellationToken cancellationToken = default);
}
```

---

## 5. Словарь шаблонов напоминаний

| Ключ | RU Текст |
|---|---|
| `Notification_Inactivity_1` | 🔥 **Кто-то прямо сейчас просматривает анкеты в твоем городе!** Загляни в бот, возможно, тебя уже кто-то ждет! |
| `Notification_Inactivity_2` | ❤️ **Найди свою любовь!** Новые анкеты уже ждут твоей оценки. Сделай первый шаг! |
| `Notification_Inactivity_3` | 👥 **Ищешь новых друзей и интересное общение?** Тысячи людей вокруг готовы познакомиться! |
| `Notification_Inactivity_4` | 💌 **С тобой хотят познакомиться!** Не упусти возможность завести приятное знакомство прямо сейчас. |
| `Notification_Inactivity_5` | ✨ **Твоя идеальная пара может быть совсем рядом!** Наш ИИ подобрал для тебя новые классные анкеты. |
| `Notification_Inactivity_6` | 💬 **Тебе скучно?** Открой поиск анкет и начни увлекательный диалог прямо сегодня! |
| `Notification_Inactivity_7` | 🎯 **Твоя судьба в твоих руках!** Зайди в бот и оцени свежие профили рядом с тобой. |
| `Notification_Inactivity_8` | 🌟 **Кто-то ждет именно тебя!** Поставь лайк и узнай, совпали ли ваши симпатии. |
| `Notification_Inactivity_9` | 🚀 **Свежие анкеты уже в поиске!** Посмотри, кто недавно присоединился к DatingBot. |
| `Notification_Inactivity_10` | 💖 **Любовь не ждет!** Загляни в бот и найди того, с кем захочется пойти на свидание. |
| `Btn_Inactivity_StartSearch` | 🔍 Начать поиск |
