# DatingBot — Главный устав и точка входа управления (Standing Orders)

## 1. Назначение

Этот файл (`GEMINI.md`) — **главная точка входа управления проектом (Уровень 1)**. Он находится в корне проекта (`BASE_DIR`).
Любой ИИ-агент обязан читать его первым при старте каждой рабочей сессии.

Каталог `docs/` — **постоянная память** для кодовых агентов, работающих над этим проектом:
- `docs/progress.md` — текущий статус, задачи и инвариант `state_version`.
- `docs/control/agent_rules.md` — правила выполнения (Must / Must-Not, инженерные суперсилы TDD, Debugging, Verification).
- `docs/control/git_workflow.md` — регламент ветвления, Conventional Commits и Git-протокол.
- `docs/architecture/architecture.md` — главное архитектурное описание системы.
- `docs/architecture/decisions.md` — журнал принятых архитектурных решений (ADR).
- `docs/architecture/specs/` — модульные спецификации и контракты подсистем (SDD).

Полную раскладку структуры проекта см. в разделе **Карта проекта** ниже — это единственное дерево.

---

## 2. Иерархия управления и приоритет источника истины

| Уровень | Файл / Каталог | Роль и область ответственности |
|:---:|:---|:---|
| **1** | `GEMINI.md` | Главная точка входа управления, протокол сессии, иерархия истины, единая карта проекта |
| **2** | `docs/control/agent_rules.md`, `git_workflow.md` | Устав агента: правила выполнения, C# 13/.NET 9, TDD, Git-регламент, запреты |
| **3** | `docs/architecture/architecture.md` | Главный источник истины по архитектуре системы и границам слоев |
| **3** | `docs/architecture/decisions.md` | Журнал ADR: история принятых решений, контекст и компромиссы (append-only) |
| **4** | `docs/architecture/specs/*.md` | Модульные спецификации и контракты подсистем (Spec-Driven Development) |
| **5** | `src/` | Исходный код реализации |

> [!IMPORTANT]
> **Принцип разрешения конфликтов:** При любых расхождениях побеждает более высокий уровень. Нижние уровни не должны нарушать верхние. Код реализации (`src/`) обязан соответствовать спецификациям (`specs/`) и архитектуре (`architecture.md`), а не наоборот.

---

## 3. Единая карта проекта (Project Map)

У каждого факта ровно один дом. Карта ниже — единственное дерево навигации по проекту:

```
DatingBot/                                # Корень проекта == BASE_DIR
│
├── GEMINI.md                             # [Уровень 1] Главная точка входа управления (этот файл)
├── README.md                             # Общее описание проекта и руководство по запуску
├── DatingBot.sln                         # Файл решения Visual Studio / .NET 9
├── Dockerfile                            # Multi-stage сборка .NET 9 для Render / Docker
├── .dockerignore                         # Исключения для Docker сборки
├── .gitignore                            # Игнорируемые Git файлы (.worktrees, bin, obj)
│
├── docs/                                 # Постоянная память и управляющий слой (Standing Orders)
│   ├── progress.md                       # [Летопись] Текущее состояние, инвариант state_version, фазы
│   │
│   ├── control/
│   │   ├── agent_rules.md                # [Уровень 2: Устав] Правила выполнения, Must/Must-Not, TDD, пайплайн
│   │   └── git_workflow.md               # [Уровень 2: Git] Ветвление, Conventional Commits, Worktrees, откат
│   │
│   └── architecture/
│       ├── architecture.md               # [Уровень 3] Авторитет по устройству системы, слои, потоки данных
│       ├── decisions.md                  # [Уровень 3] Журнал ADR: история принятых решений (append-only)
│       │
│       └── specs/                        # [Уровень 4] Спецификации и контракты модулей (SDD)
│           ├── domain_models.md          # Доменные сущности, энумы, инварианты
│           ├── registration_and_fsm.md   # FSM, шаги мастера регистрации, валидаторы
│           ├── matchmaking_scoring.md    # 4-уровневый каскадный скоринг, формулы гео-расчета
│           ├── search_and_ratings.md     # 10-балльные оценки, взаимные симпатии, жалобы
│           ├── admin_and_moderation.md   # Панель админа, модерация, медиакит-аналитика, рассылки
│           ├── database_persistence.md   # Схема БД, Fluent API, индексы, сидирование городов
│           ├── ai_embeddings.md          # Локальная SIMD-векторизация описаний и косинусное сходство
│           ├── localization_ui.md        # Словарь на 6 языков, грамматические падежи, UI-клавиатуры
│           └── inactivity_reminders.md   # Заманчивые напоминания неактивным пользователям
│
├── src/                                  # [Уровень 5] Исходный код реализации (.NET 9 / C# 13)
│   │
│   ├── DatingBot.Domain/                 # Ядро: 0 внешних зависимостей
│   │   ├── Entities/                     # Сущности (User, UserProfile, City, Interest, Rating, Report, PaymentTransaction)
│   │   ├── Enums/                        # Перечисления (UserState, AppLanguage, Gender, TargetGender, PaymentType...)
│   │   └── Exceptions/                   # Доменные исключения (DomainException)
│   │
│   ├── DatingBot.Application/            # Сценарии использования и бизнес-сервисы
│   │   ├── Common/                       # Result Pattern (Result, Result<T>)
│   │   ├── DTOs/                         # Модели передачи данных (UserProfileDto, MatchCandidateDto, AdminStatsDto...)
│   │   ├── Interfaces/                   # Интерфейсы репозиториев и сервисов
│   │   ├── Services/                     # Бизнес-сервисы (Registration, ProfileEditing, Matchmaking, Search, Loc, Inactivity)
│   │   └── Validators/                   # FluentValidation валидаторы (Name, Age, City, Height, AiBio)
│   │
│   ├── DatingBot.Infrastructure/         # Инфраструктура, MS SQL Server и EF Core
│   │   ├── Data/
│   │   │   ├── AppDbContext.cs           # Контекст EF Core
│   │   │   ├── AppDbContextFactory.cs    # Фабрика для миграций EF Core CLI
│   │   │   ├── Configurations/           # Fluent API конфигурации таблиц и индексов
│   │   │   ├── Datasets/                 # cities_database.json.gz (100k+ сжатых городов)
│   │   │   └── Seeds/                    # CityDatabaseSeeder (распаковка и пакетный импорт)
│   │   ├── Repositories/                 # Реализации репозиториев и UnitOfWork
│   │   └── Services/                     # LocalAiEmbeddingService (SIMD векторизация)
│   │
│   └── DatingBot.Bot/                    # Презентационный слой Telegram (Telegram.Bot)
│       ├── Handlers/                     # TelegramUpdateRouter, FSM хэндлеры сообщений и кнопок
│       ├── Keyboards/                    # Фабрики инлайн- и reply-клавиатур (MainMenu, Profile, Payment, Admin, TelegramUrlHelper...)
│       ├── Services/                     # TelegramBotWorker, BotLifecycleCoordinator, IBotLifecycleCoordinator, AdminBroadcast, BotSetup...
│       ├── Workers/                      # DatabaseBootstrapWorker, TelegramBotWorker, MatchmakingNotificationWorker, InactivityNotificationWorker
│       ├── appsettings.json              # Базовая конфигурация (BotToken, AdminIds, InactivityReminderDays...)
│       └── Program.cs                    # Точка входа DI, Web-хост Kestrel и Keep-Alive/Health эндпоинты
│
├── tests/                                # Модульные и интеграционные тесты (432 теста)
│   ├── DatingBot.UnitTests/              # xUnit модульные тесты сервисов, алгоритмов, воркеров и координатора
│   └── DatingBot.IntegrationTests/       # Интеграционные тесты сидера БД и сценариев
│
└── .agents/                              # Инструменты для ИИ-агентов (скиллы и субагенты)
    ├── subagents/                        # Ролевые профили (dotnet-developer, code-reviewer, qa-tester, architect...)
    └── skills/                           # Инженерные навыки (SOP / ранбуки)
        ├── dotnet-csharp-best-practices/ # C# 13 / .NET 9 архитектурные паттерны
        ├── dotnet-testing/               # xUnit / Moq / FluentAssertions рецепты
        ├── ef-migrations/                # Команды и миграции EF Core 9
        ├── matchmaking-scoring/          # Алгоритмы скоринга и подбора
        ├── mssql-performance/            # Оптимизация SQL и индексов
        ├── systematic-debugging/         # 4-фазная отладка первопричин
        ├── test-driven-development/      # Red-Green-Refactor TDD цикл
        ├── verification-before-completion/ # Железный закон верификации перед сдачей
        ├── using-git-worktrees/          # Изолированные воркспейсы в Git
        ├── finishing-a-development-branch/ # Протокол слияния и завершения веток
        ├── requesting-code-review/       # Протокол запроса независимого код-ревью
        └── receiving-code-review/        # Критический прием и обработка замечаний
```

---

## 4. Протокол работы сессии (Session Protocol)

### 4.1. Начало сессии:
1. Прочитать `GEMINI.md` — зафиксировать карту проекта и иерархию истины.
2. Прочитать `docs/control/agent_rules.md` и `docs/control/git_workflow.md`.
3. Прочитать `docs/progress.md` — зафиксировать текущий `state_version` и статус задач.
4. Прочитать `docs/architecture/architecture.md` и релевантные спецификации `docs/architecture/specs/*.md`.

### 4.2. Завершение сессии:
1. Выполнить верификацию (`dotnet test DatingBot.sln` и `dotnet build DatingBot.sln`).
2. Обновить `docs/progress.md` — зафиксировать сделанные изменения, текущую фазу, следующий шаг и **увеличить `state_version` на 1**.
3. Если изменилась архитектура → обновить `docs/architecture/architecture.md` + добавить ADR в `docs/architecture/decisions.md`.
4. Если изменился контракт модуля → обновить соответствующую спецификацию в `docs/architecture/specs/*.md`.
5. Если файлы/каталоги были добавлены, удалены или перемещены → обновить Карту проекта в `GEMINI.md` (единственное дерево).
6. Зафиксировать изменения коммитом в ветку задачи, слить в `gemini`, выполнить `git push origin gemini` и запросить инспекцию пользователя для слияния в `main` (согласно `docs/control/git_workflow.md`).

---

## 5. Версия состояния (`state_version`)

Файл `docs/progress.md` содержит поле `state_version: N`.
- Увеличивайте `state_version` на 1 при каждой успешной записи в `docs/progress.md`.
- Назначение: обнаружение устаревания контекста между сессиями и контроль за дисциплиной агента.

---

# КОНЕЦ КОРНЕВОГО УПРАВЛЕНИЯ
