# 🤖 DatingBot (.NET 9 / C# 13 / MS SQL / EF Core)

**DatingBot** — Telegram-бот для знакомств с умным алгоритмом подбора пар (Matchmaking), 10-балльной системой рейтинга, выявлением взаимных симпатий, поддержкой 6 языков и географическим поиском по базе из 100 000+ городов мира.

Проект построен по принципам **Clean Architecture** и управляется по современному стандарту **Standing Orders / Spec-Driven Development (SDD)**.

---

## 📁 Структура проекта (Clean Architecture & Standing Orders)

```
DatingBot/
├── GEMINI.md                             # [Уровень 1] Главная точка входа управления проектом
├── README.md                             # Этот файл
├── DatingBot.sln                         # Файл решения .NET 9
│
├── docs/                                 # Постоянная память и управляющий слой проекта
│   ├── progress.md                       # [Летопись] Текущее состояние, инвариант state_version, фазы
│   ├── control/
│   │   ├── agent_rules.md                # [Уровень 2: Устав] Правила выполнения, Must / Must-Not
│   │   └── git_workflow.md               # [Уровень 2: Git] Ветвление, Conventional Commits
│   └── architecture/
│       ├── architecture.md               # [Уровень 3] Главный авторитет по устройству системы
│       ├── decisions.md                  # [Уровень 3] Журнал ADR: история принятых решений
│       └── specs/                        # [Уровень 4] Спецификации модулей (SDD)
│           ├── domain_models.md          # Доменные сущности, энумы, инварианты
│           ├── registration_and_fsm.md   # FSM, шаги мастера регистрации, валидация
│           ├── matchmaking_scoring.md    # 4-уровневый каскадный скоринг, гео-расчеты
│           ├── search_and_ratings.md     # 10-балльные оценки, взаимные симпатии, жалобы
│           ├── admin_and_moderation.md   # Панель админа, модерация, медиакит-аналитика, рассылки
│           ├── database_persistence.md   # Схема БД, Fluent API, индексы, сидирование
│           ├── ai_embeddings.md          # Локальная SIMD-векторизация описаний
│           └── localization_ui.md        # Словарь на 6 языков, UI-клавиатуры
│
├── src/                                  # [Уровень 5] Исходный код реализации (.NET 9 / C# 13)
│   ├── DatingBot.Domain/                 # Доменное ядро (0 внешних зависимостей)
│   ├── DatingBot.Application/            # Бизнес-сервисы, DTO, валидация, интерфейсы
│   ├── DatingBot.Infrastructure/         # MS SQL Server, EF Core 9, репозитории, AI-векторизатор
│   └── DatingBot.Bot/                    # Презентационный слой Telegram (Telegram.Bot, FSM)
│
├── tests/                                # Тестовый пакет (221 тест)
│   ├── DatingBot.UnitTests/              # Модульные тесты xUnit / Moq / FluentAssertions
│   └── DatingBot.IntegrationTests/       # Интеграционные тесты сидера БД и компонентов
│
└── .agents/                              # Инструменты для ИИ-агентов (скиллы и субагенты)
```

---

## 🧠 Система управления Standing Orders (SDD)

Проект использует многоуровневую систему управления постоянной памятью для предотвращения деградации контекста ИИ-агентов:

1. **`GEMINI.md` (Уровень 1)** — Главная точка входа, определяющая порядок источников истины, единую карту проекта и протокол работы сессии.
2. **`docs/control/agent_rules.md` (Уровень 2)** — Устав агента: обязательные требования (Must), запреты (Must-Not), стандарты чистоты кода и FSM.
3. **`docs/control/git_workflow.md` (Уровень 2)** — Регламент ветвления, Conventional Commits и Git-протокол.
4. **`docs/architecture/architecture.md` (Уровень 3)** — Полное описание архитектуры, слоев Clean Architecture и жизненного цикла запроса.
5. **`docs/architecture/decisions.md` (Уровень 3)** — Журнал ADR (Architecture Decision Records) со всеми принятыми решениями.
6. **`docs/architecture/specs/*.md` (Уровень 4)** — Модульные спецификации подсистем (Spec-Driven Development).
7. **`docs/progress.md` (Летопись)** — Отслеживание фаз проекта с глобальным инвариантом `state_version`.

---

## 🛠 Полезные команды

```powershell
# Сборка решения
dotnet build DatingBot.sln

# Запуск полного пакета тестов (221 тест)
dotnet test DatingBot.sln

# Запуск Telegram-бота
dotnet run --project src/DatingBot.Bot/DatingBot.Bot.csproj
```
