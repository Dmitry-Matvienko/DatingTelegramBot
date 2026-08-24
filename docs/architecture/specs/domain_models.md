# Спецификация: Доменные модели и сущности (Domain Models)

## 1. Назначение модуля

Слой `DatingBot.Domain` представляет собой ядро системы (Enterprise Domain Layer).
Модуль инкапсулирует сущности, перечисления, доменные события и правила целостности данных.

---

## 2. Сущности (Entities)

### 2.1. `User`
Представляет учетную запись Telegram-пользователя.
- `Id` (`Guid`): Первичный ключ.
- `TelegramId` (`long`): Уникальный идентификатор чата Telegram.
- `Username` (`string?`): Юзернейм пользователя без `@`.
- `FirstName` (`string?`): Имя в Telegram.
- `Language` (`AppLanguage`): Выбранный язык интерфейса (по умолчанию `Russian`).
- `State` (`UserState`): Текущее состояние конечного автомата FSM (по умолчанию `None`).
- `LastBotMessageId` (`int?`): Идентификатор последнего отправленного ботом сообщения (для безопасной очистки чата).
- `CurrentCandidateProfileId` (`Guid?`): Идентификатор анкеты, которая сейчас отображается пользователю в поиске.
- `Profile` (`UserProfile?`): Связанная анкета знакомств (1-к-1).

### 2.2. `UserProfile`
Основная анкета пользователя для знакомств.
- `Id` (`Guid`): Первичный ключ.
- `UserId` (`Guid`): Внешний ключ на `User`.
- `Name` (`string`): Отображаемое имя (от 2 до 50 символов).
- `Age` (`int`): Возраст (от 10 до 100 лет).
- `Gender` (`Gender`): Пол (`Male` / `Female`).
- `TargetGender` (`TargetGender`): Предпочитаемый пол для знакомств (`Male` / `Female` / `All`).
- `City` (`string`): Название города проживания.
- `CityId` (`int?`): Внешний ключ на связанный город `City`.
- `Height` (`int?`): Рост в сантиметрах (от 100 до 250 см, опционально).
- `PhotoFileId` (`string?`): Telegram File ID фотографии профиля.
- `DatingTarget` (`DatingTarget`): Цель знакомства (`Friends` / `Relationship` / `AdultOnly`).
- `AiDescription` (`string?`): Скрытое текстовое описание о себе для AI-анализа (до 1000 символов).
- `AiVector` (`byte[]?`): Сериализованный вектор эмбеддинга (384 float-значения).
- `Greeting` (`string?`): Публичное приветствие/статус в анкете (до 300 символов, отображается всем).
- `AgeFilters` (`AgeCategoryFilter`): Битовые флаги категорий возраста для фильтрации в поиске.
- `SearchMinAge` (`int?`): Ручной минимальный возраст поиска (10–100).
- `SearchMaxAge` (`int?`): Ручной максимальный возраст поиска (10–100).
- `SearchDistance` (`SearchDistancePreference`): Выбранный радиус / область поиска кандидатов (`UpTo100Km`, `UpTo500Km`, `SameCountry`, `Anywhere`).
- `RatingCount` (`int`): Количество полученных оценок.
- `AverageRating` (`double`): Текущий средний балл (от 1.0 до 10.0).
- `IsCompleted` (`bool`): Флаг завершенности заполнения анкеты.

### 2.3. `City`
Географический справочник городов мира.
- `Id` (`int`): Первичный ключ.
- `Name` (`string`): Название города (индексировано для быстрого поиска).
- `Country` (`string`): Название страны.
- `Latitude` (`double`): Географическая широта.
- `Longitude` (`double`): Географическая долгота.

### 2.4. `Interest`
Справочник категорий интересов и хобби.
- `Id` (`int`): Первичный ключ.
- `Code` (`InterestType`): Уникальный код интереса (enum).
- `Title` (`string`): Базовое название.
- `Icon` (`string`): Эмодзи-иконка.

### 2.5. `UserProfileInterest`
Связующая сущность многие-ко-многим между `UserProfile` и `Interest`.
- `UserProfileId` (`Guid`): Внешний ключ на профиль.
- `InterestId` (`int`): Внешний ключ на интерес.

### 2.6. `ProfileRating`
Оценка, выставленная одной анкетой другой анкете.
- `Id` (`Guid`): Первичный ключ.
- `FromUserId` (`Guid`): Кто оценил.
- `ToUserId` (`Guid`): Кого оценили.
- `Score` (`int`): Балл от 1 до 10.
- `CreatedAt` (`DateTime`): Дата и время оценки (UTC).

### 2.7. `ProfileReport`
Жалоба на нарушение правил сервиса.
- `Id` (`Guid`): Первичный ключ.
- `ReporterId` (`Guid`): Кто подал жалобу.
- `ReportedUserId` (`Guid`): На кого подана жалоба.
- `Reason` (`ReportReason`): Причина (`InappropriateContent` / `IncorrectProfile` / `Other`).
- `Details` (`string?`): Текстовый комментарий заявителя.
- `IsResolved` (`bool`): Флаг рассмотрения жалобы модератором (по умолчанию `false`).
- `ResolvedAt` (`DateTime?`): Дата и время рассмотрения жалобы (UTC).
- `CreatedAt` (`DateTime`): Дата и время создания жалобы (UTC).

---

## 3. Перечисления (Enums)

- `UserState`: Состояния конечного автомата FSM:
  - Базовые: `None` (0), `Active` (100), `Paused` (101), `Banned` (999).
  - Регистрация: `Registration_SelectingGender` (1), `Registration_SelectingTargetGender` (2), `Registration_WaitingForName` (3), `Registration_WaitingForAge` (4), `Registration_WaitingForCity` (5), `Registration_WaitingForHeight` (6), `Registration_WaitingForPhoto` (7), `Registration_SelectingInterests` (8), `Registration_SelectingTarget` (9), `Registration_WaitingForAiBio` (10), `Registration_SelectingLanguage` (11), `Registration_SelectingSearchDistance` (12).
  - Редактирование: `Editing_Name` (201) .. `Editing_Greeting` (215), `Editing_SearchDistance` (216).
  - Поиск и жалобы: `Searching` (300), `Reporting_WaitingForDetails` (301).
  - Администратор: `Admin_Panel` (400), `Admin_Stats_WaitingForCity` (401), `Admin_Broadcasting_WaitingForContent` (402), `Admin_Broadcasting_WaitingForButton` (403), `Admin_Broadcasting_WaitingForCity` (404), `Admin_BrowsingProfiles` (405).
- `AppLanguage`: `Russian`, `Ukrainian`, `English`, `Hindi`, `Portuguese`, `Indonesian`.
- `Gender`: `Male`, `Female`.
- `TargetGender`: `Male`, `Female`, `All`.
- `DatingTarget`: `Friends`, `Relationship`, `AdultOnly`.
- `SearchDistancePreference`: `UpTo100Km` (1), `UpTo500Km` (2), `SameCountry` (3), `Anywhere` (4).
- `InterestType`: `Gaming`, `Music`, `Cinema`, `Sports`, `Travel`, `Art`, `Cooking`, `Science`, `Literature`, `Nature`, `Fashion`, `Tech`.
- `MatchTier`: `AiCompatibility`, `CommonInterests`, `SameCity`, `NearbyCity`.
- `ReportReason`: `InappropriateContent`, `IncorrectProfile`, `Other`.
- `AgeCategoryFilter` (`[Flags]`): `None`, `Under18`, `Age18To25`, `Age25To30`, `Age30To40`, `Age40Plus`.
