---
name: mssql-performance
description: >-
  Best practices for MS SQL Server schema design, indexing strategies, spatial queries, and EF Core query performance in DatingBot.
  Use when designing entity relationships, creating indexes, writing complex LINQ queries, or diagnosing database operations.
---

# MS SQL Server Performance & Indexing Strategy (DatingBot)

## 1. Схема индексов и Fluent API конфигурации

- **`Users` (`UserConfiguration`)**:
  - `IX_Users_TelegramId` (Unique) — мгновенная идентификация пользователя Telegram при каждом входящем Update.
  - Каскадное удаление профиля (`DeleteBehavior.Cascade`).

- **`UserProfiles` (`UserProfileConfiguration`)**:
  - `IX_UserProfiles_CityId` — ускорение локальных выборок по городу.
  - Фильтрованный индекс `IX_UserProfiles_IsCompleted` (`WHERE IsCompleted = 1`) — отсечение незавершенных регистраций из выдачи.
  - Поле `AiVector` хранится как `varbinary(1536)` (384 float $\times$ 4 байта).

- **`Cities` (`CityConfiguration`)**:
  - `IX_Cities_Name` — поиск городов по префиксу, подстроке и синонимам.
  - Колонки `Latitude` и `Longitude` для быстрого вычисления дистанций по формуле гаверсинусов.

- **`ProfileRatings` (`ProfileRatingConfiguration`)**:
  - Композитный уникальный индекс `IX_ProfileRatings_FromUserId_ToUserId` — гарантия исключения дубликатов оценок.
  - Композитный индекс `IX_ProfileRatings_ToUserId_Score` — быстрая выборка входящих симпатий ($\ge 6$).

- **`ProfileReports` (`ProfileReportConfiguration`)**:
  - `IX_ProfileReports_ReporterId` — оперативное исключение нарушителей из выдачи заявителя.
  - Фильтрованный индекс по `IsResolved` для очереди модерации администратора.

## 2. Оптимизация LINQ & EF Core 9

- **`AsNoTracking()`**: Всегда использовать `.AsNoTracking()` для всех операций чтения анкет, каталога городов и статистики.
- **Проекции DTO**: Использовать явные проекции `.Select(p => new UserProfileDto(...))` во избежание загрузки избыточных полей.
- **Предотвращение N+1**: Явный `.Include(p => p.Interests).ThenInclude(pi => pi.Interest)` для загрузки связей или точечные проекции.
- **SIMD In-Memory Scoring**: Вычисление косинусного сходства векторов `AiVector` выполняется локально в памяти на процессоре с SIMD-инструкциями (`System.Numerics.Vector<float>`), минуя тяжелые строковые/SQL операции.
- **Пакетная вставка**: Инициализация 100k+ городов через `CityDatabaseSeeder` пакетами по 5 000 записей через `AddRangeAsync` в рамках одной транзакции.
