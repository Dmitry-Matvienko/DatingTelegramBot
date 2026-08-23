# Архитектура системы DatingBot

## 1. Обзор системы

**DatingBot** — высокопроизводительный Telegram-бот для знакомств на базе платформы **.NET 9 (C# 13)** и **Microsoft SQL Server**.
Бот обеспечивает полный цикл взаимодействия пользователей:
- Интерактивную пошаговую регистрацию через Telegram FSM.
- Умный каскадный алгоритм подбора кандидатов (AI-семантика, пересечение интересов, локация по городу и гео-дистанция до 500 км).
- Оценивание анкет по 10-балльной шкале с выявлением взаимных симпатий (Mutual Match).
- Модерацию и обработку жалоб с оперативным уведомлением администраторов.
- Поддержку 6 языков (Русский, Украинский, Английский, Хинди, Португальский, Индонезийский) с учетом грамматических форм.

---

## 2. Технологический стек

- **Платформа:** .NET 9.0 (C# 13)
- **СУБД:** Microsoft SQL Server 2022 / Azure SQL
- **ORM:** Entity Framework Core 9 (`Microsoft.EntityFrameworkCore.SqlServer`)
- **Telegram Bot API:** `Telegram.Bot` v22.x
- **Валидация данных:** `FluentValidation` v11.x
- **Векторизация и AI:** Встроенный векторный движок с SIMD/AVX аппаратным ускорением (`System.Numerics.Vector<float>`)
- **Тестирование:** `xUnit`, `FluentAssertions`, `Moq`
- **Контейнеризация и хостинг:** Generic Host, Docker, Microsoft.Extensions.Hosting

---

## 3. Слои Clean Architecture и границы модулей

```
+-------------------------------------------------------------------+
|                        DatingBot.Bot                              |
|   (Telegram Update Router, FSM Handlers, Keyboards, Prompts)      |
+-------------------------------------------------------------------+
                                  │
                                  ▼
+-------------------------------------------------------------------+
|                     DatingBot.Application                         |
|  (Services, Repositories Interfaces, DTOs, Validators, Result<T>) |
+-------------------------------------------------------------------+
          ▲                                                 ▲
          │                                                 │
+-----------------------+                         +-----------------------+
|   DatingBot.Domain    |                         | DatingBot.Infrastructure|
| (Entities, Enums,     |                         | (AppDbContext, EF     |
|  Exceptions, Events)  |                         |  Configurations, Repos|
| [0 Dependencies]      |                         |  Seeder, AI Vectors)  |
+-----------------------+                         +-----------------------+
```

### 3.1. `DatingBot.Domain` (Ядро)
- Содержит чистые модели данных (`User`, `UserProfile`, `City`, `Interest`, `ProfileRating`, `ProfileReport`).
- Перечисления бизнес-домена (`UserState`, `AppLanguage`, `Gender`, `TargetGender`, `DatingTarget`, `InterestType`, `MatchTier`, `ReportReason`, `AgeCategoryFilter`).
- Доменные исключения (`DomainException`).
- **Зависимости:** 0 внешних пакетов.

### 3.2. `DatingBot.Application` (Бизнес-сценарии)
- Абстракции хранилищ (`IUserRepository`, `IUserProfileRepository`, `ICityRepository`, `IInterestRepository`, `IProfileRatingRepository`, `IProfileReportRepository`, `IPaymentTransactionRepository`, `IUnitOfWork`).
- Бизнес-сервисы (`IRegistrationService`, `IProfileEditingService`, `IMatchmakingService`, `ISearchService`, `ILocalizationService`, `IAiEmbeddingService`, `IAdminService`, `IModerationService`, `IInactivityReminderService`).
- DTO (Data Transfer Objects) и маппинг.
- Валидаторы входных данных на FluentValidation.
- Шаблон возврата результатов `Result` / `Result<T>`.
- **Зависимости:** Только `Domain`, `FluentValidation`.

### 3.3. `DatingBot.Infrastructure` (Инфраструктура и БД)
- `AppDbContext` (EF Core) и фабрика контекста `AppDbContextFactory` для миграций.
- Fluent API конфигурации таблиц (`IEntityTypeConfiguration<T>`).
- Реализации репозиториев с оптимизацией AsNoTracking, индексами и гео-вычислениями по формуле гаверсинусов.
- Сидер базы данных городов (`CityDatabaseSeeder`) с авто-распаковкой встроенного Gzip-датасета 100k+ городов.
- Локальный сервис AI-векторизации (`LocalAiEmbeddingService`) на базе n-граммного хеширования и SIMD-косинусного сходства.
- **Зависимости:** `Application`, `Domain`, `Microsoft.EntityFrameworkCore.SqlServer`.

### 3.4. `DatingBot.Bot` (Презентационный слой Telegram)
- Точка входа `Program.cs`, конфигурация DI и Generic Host.
- `TelegramUpdateRouter` — диспетчер сообщений и callback-запросов.
- Обработчики FSM (`RegistrationMessageHandler`, `RegistrationCallbackHandler`, `ProfileEditMessageHandler`, `ProfileEditCallbackHandler`, `SearchCallbackHandler`, `AdminCallbackHandler`, `AdminMessageHandler`, `AdminModerationCallbackHandler`).
- Фабрики инлайн- и reply-клавиатур (`LanguageKeyboards`, `MainMenuKeyboards`, `ProfileKeyboards`, `RegistrationKeyboards`, `SearchKeyboards`, `AdminKeyboards`, `PaymentKeyboards`).
- Сервисы формирования визуальных карточек (`ProfilePromptService`, `SearchPromptService`, `RegistrationPromptService`, `AdminPromptService`, `AdminBroadcastService`).
- Фоновые воркеры (`TelegramBotWorker`, `MatchmakingNotificationWorker`, `InactivityNotificationWorker`).
- **Зависимости:** `Application`, `Infrastructure`, `Telegram.Bot`.

---

## 4. Жизненный цикл обработки входящего Update

1. **Telegram Update** поступает в `TelegramBotWorker` через Long Polling.
2. Создается `IServiceScope`.
3. Запрос передается в `TelegramUpdateRouter`.
4. Маршрутизатор идентифицирует пользователя (`TelegramId`), подгружает его профиль и текущее состояние (`UserState`).
5. В зависимости от типа апдейта (`Message` / `CallbackQuery` / `Command`) управление передается соответствующему хэндлеру:
   - Если пользователь в состоянии регистрации → `RegistrationMessageHandler` / `RegistrationCallbackHandler`.
   - Если пользователь в режиме редактирования → `ProfileEditMessageHandler` / `ProfileEditCallbackHandler`.
   - Если пользователь в поиске/оценке/жалобе → `SearchCallbackHandler`.
6. Хэндлер вызывает методы слоя `Application` (`SearchService`, `RegistrationService`, `ProfileEditingService`).
7. Ответ форматируется с учетом языка пользователя `AppLanguage` через `ILocalizationService` и отправляется обратно в чат.
8. Старое временное сервисное сообщение при необходимости удаляется (`DeleteMessageSafeAsync`) для поддержания чистоты чата.

---

## 5. Стратегия обработки ошибок

- **Валидация входных данных**: Перехватывается на уровне слоя `Application` валидаторами FluentValidation и возвращается в виде `Result.Failure(errorMessage)`. Пользователю выводится понятное сообщение об ошибке на его языке без изменения состояния FSM.
- **Сбои внешних вызовов**: Логируются через `ILogger<T>`. Сетевые исключения Telegram API изолируются в `TelegramBotWorker`.
- **База данных**: Все модифицирующие операции группируются через репозитории и `IUnitOfWork.SaveChangesAsync()`.
