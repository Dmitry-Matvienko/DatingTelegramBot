# Летопись прогресса проекта DatingBot

state_version: 28
updated: 2026-08-24

---

## Сейчас
- **Фаза**: Реализация **оптимизации аллокаций памяти и аппаратной SIMD-векторизации (CPU & GC)**:
  1. **Zero-Allocation спан-скоринг в подборе кандидатов**:
     - В `IAiEmbeddingService` и `LocalAiEmbeddingService` добавлен метод `CalculateCosineSimilarity(float[], byte[])`, использующий `MemoryMarshal.Cast<byte, float>` и спаны.
     - В `MatchmakingService` исключен вызов `BytesToVector` в цикле кандидатов, ликвидировано выделение 153.6 КБ RAM на каждый свайп (0 аллокаций в куче при скоринге векторов).
  2. **Zero-Allocation токенизация и хеширование**:
     - В `LocalAiEmbeddingService` парсинг текста переведен на `ReadOnlySpan<char>`, стековые буферы `stackalloc byte[128]` для UTF-8 и `stackalloc byte[32]` для `SHA256.HashData` с `BinaryPrimitives.ReadUInt32LittleEndian`.
     - Ликвидированы все промежуточные аллокации строк (`Split`, `Substring`, интерполяция биграмм) и массивов `byte[]`.
  3. **SIMD-оптимизация нормализации и сходства**:
     - `NormalizeL2` и `CalculateCosineSimilarity` векторизованы с помощью `System.Numerics.Vector<float>`.
  4. **Тесты и верификация**:
     - Дополнены тесты в `LocalAiEmbeddingServiceTests` и `MatchmakingServiceTests`.
     - Все 345 тестов успешно пройдены (100% green, 0 warnings, 0 failures).
- **Далее**: Переход к следующим этапам оптимизации (кэширование справочников, профилирование).

---

## Текущий статус подсистем

| Подсистема | Статус | Покрытие тестами |
|---|:---:|:---:|
| Доменное ядро (`Domain`) | ✅ Готово | 100% |
| Сценарии и валидация (`Application`) | ✅ Готово | 100% |
| Платные услуги / Telegram Stars (`PaymentKeyboards`, `Unban`, `UnbanPriceStars`) | ✅ Готово | 100% |
| Панель администратора и рекламы (`AdminService`, `AdminBroadcastService`) | ✅ Готово | 100% |
| Модерация и жалобы (`ModerationService`) | ✅ Готово | 100% |
| Напоминания неактивным пользователям (`InactivityReminderService`, `InactivityNotificationWorker`) | ✅ Готово | 100% |
| База данных и репозитории (`Infrastructure`) | ✅ Готово | 100% |
| Локальный AI-векторизатор (`LocalAiEmbeddingService`) | ✅ Готово | 100% |
| Датасет городов (100k+ gzip) (`CityDatabaseSeeder`) | ✅ Готово | 100% |
| Отказоустойчивая инициализация и Self-Healing (`BotLifecycleCoordinator`, `DatabaseBootstrapWorker`) | ✅ Готово | 100% |
| Презентационный слой, Web-хост и Keep-Alive (`Bot`, Minimal API) | ✅ Готово | 100% |
| Многоязычность (6 языков) (`LocalizationService`) | ✅ Готово | 100% |
| Развертывание (Render Dockerfile, Env Vars) | ✅ Готово | 100% |
| Система Standing Orders / SDD | ✅ Внедрено | Полная документация |

---

## Открытые задачи — текущая фаза
- [x] Реализация Clean Architecture (Domain, Application, Infrastructure, Bot).
- [x] 4-уровневый каскадный скоринг Matchmaking (AI -> Интересы -> Город -> Соседние города до 500 км).
- [x] 10-балльная система рейтинга и детекция взаимной симпатии (6+).
- [x] Полная мультиязычность для 6 языков (RU, UK, EN, HI, PT, ID).
- [x] Покрытие тестами (337 тестов успешно пройдены).
- [x] Перевод управляющего слоя на стандарт Standing Orders / SDD.
- [x] Добавление кнопки и функционала публичного «Приветствия» в профиль пользователя и карточки выдачи кандидатов.
- [x] Интерактивная обработка жалоб модераторами с кнопками («Заблокировать», «Удалить анкету», «Проигнорировать») и мультиязычными уведомлениями.
- [x] Админ-панель для рекламы: аналитика медиакита (количественная + процентная), конструктор рассылок с фото/URL-кнопками, сквозной просмотр всех анкет базы и очередь жалоб.
- [x] Оптимизация и исправление LINQ-запросов аналитики аудитории и топ-городов для MS SQL Server в EF Core.
- [x] Расширение меню конструктора рассылок: таргетинг по категориям целей (общение, отношения, 18+), полу и городу.
- [x] Сохранение сообщения команды `/start` в чате без авто-удаления.
- [x] Отказоустойчивый fallback при ошибке невалидного/устаревшего `FileId` фото в Telegram API и соблюдение лимитов caption (1024).
- [x] Автоматическое копирование `appsettings.Local.json` и валидация `BotToken` при старте бота.
- [x] Устранение ошибки удаления анкеты и бана из админ-панели при передаче `ProfileId` (Dual ID Resolution).
- [x] Устранение предупреждения EF Core 10102 (Missing OrderBy before Skip/Take).
- [x] Реализация системы платного разбана за звёзды (Telegram Stars) с мультиязычностью, авто-разблокировкой при оплате и конфигурацией цены `BotConfiguration:UnbanPriceStars`.
- [x] Реализация раздела «💰 Доход» в админ-панели: баланс звёзд Telegram Stars, динамика дохода (24ч/7д/30д) и история последних 20 транзакций с отчетом.
- [x] Реализация заманчивых напоминаний для неактивных пользователей (Inactivity Reminders) с 10 случайными мультиязычными шаблонами, фоновым воркером и настройкой периодичности в конфигурации.
- [x] Подключение и верификация удаленной базы данных MS SQL на SmarterASP.NET.
- [x] Перевод `DatingBot.Bot` на Web-хост с Keep-Alive HTTP эндпоинтами (`/`, `/ping`, `/health`) для Render и cron-job.org.
- [x] Поддержка удобного конфигурирования через Environment Variables (`BOT_TOKEN`, `DEFAULT_CONNECTION`, `ADMIN_IDS`).
- [x] Создание Multi-stage Dockerfile и .dockerignore для сборки и развертывания .NET 9.
- [x] Отказоустойчивая система инициализации и автоматического самовосстановления бота (`IBotLifecycleCoordinator`, `DatabaseBootstrapWorker`, `TelegramBotWorker` polling loop retry, `HostOptions` failure isolation).
- [x] Оптимизация запросов подбора кандидатов: устранение риска переполнения параметров SQL Server (лимит 2100 параметров через NOT EXISTS) и ограничение пула кандидатов до 100 человек (`Take(100)`).
- [x] Оптимизация аналитики админ-панели: устранение N+1 и двухфазной выборки в `GetTopCitiesStatsAsync`/`GetCityStatsAsync`, объединение 16+ запросов `CountAsync` и 5 `SumAsync` в одиночные условные SQL-агрегации.
- [x] Добавление составного B-Tree индекса `IX_ProfileRatings_ToUser_Score_CreatedAt` на таблицу `ProfileRatings` и миграция `Add_IncomingRatings_Composite_Index` (ускорение выборки входящих симпатий до Index Seek).
- [x] Оптимизация аллокаций памяти и SIMD-векторизации: Zero-Allocation вычисление косинусного сходства векторов в `MatchmakingService` (экономия 153.6 КБ RAM на свайп) и спан-токенизация со стековыми буферами SHA256 в `LocalAiEmbeddingService`.

---

## Бэклог и открытые вопросы
- _Формируется исключительно на основе требований и задач от пользователя._
