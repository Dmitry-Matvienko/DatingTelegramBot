# Устав и правила выполнения для агентов (Agent Rules)

## 1. Базовые принципы работы

### 1.1. Think Before Coding (Сначала думай, потом кодируй)
- Не делайте скрытых предположений. Четко формулируйте компромиссы.
- Если требований недостаточно или есть неоднозначность — остановитесь и задайте вопрос.
- Всегда выбирайте простейшее и надежное решение без лишней сложности (принцип Оккама / YAGNI).

### 1.2. Surgical Changes (Хирургическая точность правок)
- Меняйте только то, что строго необходимо для решения задачи.
- Не производите рефакторинг соседнего работающего кода без прямого запроса.
- Сохраняйте единый стиль кодовой базы (.NET 9 / C# 13).

### 1.3. Goal-Driven Execution (Работа на измеримый результат)
- Каждая задача должна иметь четкие критерии успеха.
- Любое изменение бизнес-логики подкрепляется модульным тестом (`dotnet test`).
- Цикл работы: Спецификация → TDD → Реализация → Верификация → Код-ревью → Фиксация.

---

## 2. Инженерные суперсилы и железные законы (Superpowers)

### 2.1. Железный закон TDD (Test-Driven Development)
```
NO PRODUCTION CODE WITHOUT A FAILING TEST FIRST
```
- Сначала пишется падающий тест `xUnit`, проверяется его падение по ожидаемой причине (`Red`), затем пишется минимальный код (`Green`), затем чистка (`Refactor`). См. `skills/test-driven-development/SKILL.md`.

### 2.2. Железный закон верификации (Verification Before Completion)
```
NO COMPLETION CLAIMS WITHOUT FRESH VERIFICATION EVIDENCE
```
- Запрещено утверждать, что задача завершена, пока прямо в текущем шаге не выполнены команды проверки (`dotnet test DatingBot.sln` и `dotnet build DatingBot.sln`) и не подтвержден статус `0 failures`. См. `skills/verification-before-completion/SKILL.md`.

### 2.3. Железный закон отладки (Systematic Debugging)
```
NO FIXES WITHOUT ROOT CAUSE INVESTIGATION FIRST
```
- При любых сбоях строго следовать 4 фазам: Поиск первопричины → Анализ паттерна → Гипотеза и тест → Точечное исправление. Запрещено «тыкать пальцем в небо». См. `skills/systematic-debugging/SKILL.md`.

### 2.4. Культура Код-ревью (Code Review Protocol)
- Перед слиянием в `main` запрашивать ревью у `code-reviewer-agent` (`skills/requesting-code-review/SKILL.md`).
- При получении замечаний следовать `skills/receiving-code-review/SKILL.md`: критический анализ, проверка по архитектуре и тестам, аргументированный диалог, запрет слепого поддакивания (no sycophancy).

---

## 3. Обязательно к исполнению (Must)

1. **Следование протоколу сессии**:
   - Перед началом любой работы прочитать `GEMINI.md` и `docs/progress.md`, зафиксировать `state_version`.
   - Прочитать соответствующие спецификации в `docs/architecture/specs/` перед редактированием подсистем.
   - По завершении работы обновить `docs/progress.md` и увеличить `state_version` на 1.
2. **Соблюдение Clean Architecture**:
   - `Domain`: 0 внешних зависимостей. Только чистые C#-типы, сущности, энумы, события.
   - `Application`: зависит только от `Domain`. Никакого EF Core или Telegram Bot API.
   - `Infrastructure`: зависит от `Application` и `Domain`. Конфигурации таблиц строго через Fluent API (`IEntityTypeConfiguration<T>`).
   - `Bot`: зависит от `Application` и `Infrastructure`.
3. **Стандарты C# 13 и .NET 9**:
   - Строгий `Nullable` (`<Nullable>enable</Nullable>`). Запрещен необоснованный оператор `!`.
   - Все методы I/O строго асинхронные (`async Task` / `async Task<T>`) с обязательной передачей `CancellationToken cancellationToken = default`.
   - Запрещены блокирующие вызовы `.Result`, `.Wait()`.
4. **Стандарты Telegram UX**:
   - Все переходы между состояниями пользователя должны идти через FSM (`UserState`).
   - Каждый Callback-запрос от инлайн-кнопки **обязан** подтверждаться вызовом `botClient.AnswerCallbackQueryAsync(...)`.
   - Сообщения и кнопки должны быть локализованы через `ILocalizationService` для всех 6 языков.
5. **Соблюдение 3-уровневого Git Workflow**:
   - Следовать правилам ветвления и Conventional Commits из `docs/control/git_workflow.md`.
   - Ветка `gemini` — финальная стабильная ветка агента. Задачи разрабатываются в ветках `feature/*`, `fix/*`, `docs/*` и интегрируются в `gemini`.
   - Пушить готовую работу через `git push origin gemini` и ожидать инспекции пользователя.
6. **Единственный владелец истины**:
   - При добавлении/удалении/перемещении файлов обновить единую Карту проекта в `GEMINI.md`.
   - При изменении архитектуры добавить запись в `docs/architecture/decisions.md` (ADR) и обновить `docs/architecture/architecture.md`.

---

## 4. Строго запрещено (Must-Not)

1. Оставлять несогласованные или выполненные договоренности только в контексте чата (все фиксируется в файлах `docs/`).
2. Менять контракты и структуру данных в коде в обход спецификаций (`docs/architecture/specs/`).
3. Записывать обновления в `docs/progress.md` без инкремента `state_version`.
4. Выполнять `git merge`, `git commit` или `git push` напрямую в ветку `main` (слияние в `main` производит исключительно пользователь).
5. Добавлять атрибуты EF Core (`[Table]`, `[Key]`, `[ForeignKey]`) в сущности слоя `Domain`.
6. Использовать хардкод строк на русском/английском в UI и сообщениях Telegram в обход словаря `LocalizationService`.
7. Нарушать приоритет источников истины (см. Иерархию управления в `GEMINI.md`).
8. Утверждать о завершении задачи без свежего запуска `dotnet test`.
9. Фиксировать в Git реальные API-токены, пароли и секреты в `appsettings.json` или коде (все локальные секреты строго в `appsettings.Local.json` или переменных окружения).

---

## 5. Конвейер выполнения задачи (Pipeline)

```
[Пользовательский запрос]
           │
           ▼
[1. Архитектура и спецификация] ──► architect-agent
   - Создание/обновление docs/architecture/specs/*.md
   - Фиксация решений в docs/architecture/decisions.md (ADR)
           │
           ▼
[2. Разработка (TDD)] ───────────► dotnet-developer-agent & database-agent
   - Ветка feature/* или fix/* от gemini
   - Создание падающего теста xUnit (Red)
   - Реализация кода Domain/App/Infra/Bot (Green)
   - Рефакторинг (Refactor)
           │
           ▼
[3. Авто-тестирование] ──────────► qa-tester-agent
   - Полный прогон `dotnet test DatingBot.sln`
           │
           ▼
[4. Код-ревью] ──────────────────► code-reviewer-agent
   - Запрос ревью (requesting-code-review)
   - Критическая обработка замечаний (receiving-code-review)
           │
           ▼
[5. Завершение и интеграция] ────► finishing-a-development-branch
   - Проверка verification-before-completion
   - Слияние ветки задачи в gemini
   - Обновление docs/progress.md (state_version++)
   - git push origin gemini
   - Запрос финальной инспекции у пользователя (Human Gatekeeper для main)
```
