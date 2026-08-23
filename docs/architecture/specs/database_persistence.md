# Спецификация: База данных и Хранилище (Database & Persistence)

## 1. Назначение модуля

Слой `DatingBot.Infrastructure` обеспечивает доступ к данным через Entity Framework Core 9 и Microsoft SQL Server.
Модуль содержит контекст БД (`AppDbContext`), конфигурации таблиц Fluent API, репозитории, транзакционный Unit of Work и механизм начальной инициализации (сидирования).

---

## 2. Схема базы данных и связи

```
       ┌──────────────────────┐
       │        Users         │
       │ (PK: Id, TelegramId) │
       └──────────┬───────────┘
                  │ 1
                  │
                  │ 1 (Опциональная связь)
                  ▼
       ┌──────────────────────┐               ┌──────────────────────┐
       │     UserProfiles     │ 1           * │ UserProfileInterests │
       │ (PK: Id, FK: UserId) ├───────────────┤ (PK: ProfileId, Int) │
       └──────────┬───────────┘               └──────────┬───────────┘
                  │                                      │ *
                  │ *                                    │ 1
                  ▼ 1                                    ▼
       ┌──────────────────────┐               ┌──────────────────────┐
       │        Cities        │               │      Interests       │
       │ (PK: Id, Lat, Lon)   │               │ (PK: Id, Code)       │
       └──────────────────────┘               └──────────────────────┘

       ┌──────────────────────┐               ┌──────────────────────┐
       │    ProfileRatings    │               │    ProfileReports    │
       │ (FromUserId,ToUserId)│               │ (ReporterId,Reported)│
       └──────────────────────┘               └──────────────────────┘
```

---

## 3. Конфигурации Fluent API и индексы

Все маппинги вынесены в `src/DatingBot.Infrastructure/Data/Configurations/`:

1. **`UserConfiguration`**:
   - `TelegramId` — уникальный индекс `IX_Users_TelegramId`.
   - Связь 1-к-1 с `UserProfile` через `DeleteBehavior.Cascade`.
2. **`UserProfileConfiguration`**:
   - Индекс `IX_UserProfiles_CityId` для ускорения выборки по городу.
   - Фильтрованный индекс `IX_UserProfiles_IsCompleted` (`WHERE IsCompleted = 1`).
   - Поле `AiVector` хранится как `varbinary(1536)` (384 float-числа по 4 байта).
3. **`CityConfiguration`**:
   - Индекс `IX_Cities_Name` для поиска городов по префиксу и синонимам.
   - Координаты `Latitude` и `Longitude` для расчета дистанций.
4. **`ProfileRatingConfiguration`**:
   - Композитный уникальный индекс `IX_ProfileRatings_FromUserId_ToUserId` (предотвращает повторные оценки).
   - Индекс `IX_ProfileRatings_ToUserId_Score` для быстрой выборки входящих оценок \(\ge 6\).
5. **`ProfileReportConfiguration`**:
   - Индекс `IX_ProfileReports_ReporterId` для отсечения нарушителей из выдачи.

---

## 4. Сидирование городов (`CityDatabaseSeeder`)

- База из более чем **100 000 городов мира** упакована в `cities_database.json.gz` (размер ~2.5 МБ).
- При первом запуске приложения `CityDatabaseSeeder.SeedAsync()`:
  1. Проверяет наличие записей в таблице `Cities`.
  2. Если таблица пуста, распаковывает `GZipStream` в памяти.
  3. Десериализует JSON потоком и пакетами по 5000 записей через `AddRangeAsync` выполняет `SqlBulkCopy` / пакетную вставку.
- Также доступен аварийный fallback `CitySeedData.cs` со статическим набором крупнейших городов для тестовых сред (InMemory DB).

---

## 5. Паттерны Unit of Work и Repositories

- Все операции чтения, не требующие отслеживания изменений, используют `.AsNoTracking()`.
- Репозитории:
  - `UserRepository`, `UserProfileRepository`, `CityRepository`, `InterestRepository`, `ProfileRatingRepository`, `ProfileReportRepository`.
- `UnitOfWork` инкапсулирует `SaveChangesAsync(cancellationToken)`.
