# Летопись прогресса проекта DatingBot

state_version: 24
updated: 2026-08-24

---

## Сейчас
- **Фаза**: Реализация **отказоустойчивой системы инициализации и автоматического самовосстановления (Self-Healing Architecture)** для бота:
  1. **Координатор жизненного цикла (`IBotLifecycleCoordinator` / `BotLifecycleCoordinator`)**:
     - Потокобезопасный `Singleton`, отслеживающий состояние готовности базы данных (`IsDatabaseReady`), активность опроса Telegram Long Polling (`IsTelegramPollingActive`), количество попыток подключения к БД (`DatabaseRetryCount`), количество перезапусков Telegram (`TelegramRestartCount`), время запуска, аптайм и текст последних ошибок.
     - Асинхронное ожидание готовности базы (`WaitForDatabaseReadyAsync`) через `TaskCompletionSource` для зависимых служб.
  2. **Неблокирующая фоновая инициализация БД (`DatabaseBootstrapWorker`)**:
     - Миграции EF Core и сидирование 100k+ городов вынесены из блокирующего `Program.cs` в фоновый воркер (`BackgroundService`).
     - При холодном старте, задержках удаленной СУБД (SmarterASP.NET / Azure SQL) или сетевых сбоях веб-сервер Kestrel остается онлайн, отвечает на Keep-Alive пинги (`/ping`, `/health`), а воркер повторяет попытки подключения с экспоненциальным backoff (3s -> 5s -> 10s -> 30s) до успешного завершения.
  3. **Супервизорный цикл опроса и изоляция ошибок (`TelegramBotWorker`)**:
     - Запуск опроса ожидает готовности БД, после чего запускает `botClient.ReceiveAsync` внутри защищенного супервизорного цикла `while (!stoppingToken.IsCancellationRequested)`.
     - При любых сбоях сети, таймаутах или ошибках Telegram API воркер автоматически перезапускает сессию опроса с backoff-задержкой.
     - Метод `ProcessUpdateSafeAsync` изолирует ошибки обработки входящих апдейтов отдельных пользователей — сбой на одном сообщении никогда не ломает сессию бота для остальных пользователей.
  4. **Защита хоста от падений воркеров**:
     - `HostOptions.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.Ignore` в `Program.cs` исключает аварийную остановку всего хоста .NET при непредвиденных исключениях в фоновых задачах.
     - Добавлены глобальные обработчики `AppDomain.CurrentDomain.UnhandledException` и `TaskScheduler.UnobservedTaskException`.
  5. **Координация фоновых сервисов уведомлений**:
     - `MatchmakingNotificationWorker` и `InactivityNotificationWorker` безопасно ожидают готовности базы через `WaitForDatabaseReadyAsync` перед выполнением периодических запросов.
  6. **Расширенная телеметрия здоровья и статуса**:
     - `GET /` — детальный JSON-статус с аптаймом, `status` ("running" / "bootstrapping"), `isDatabaseReady`, `isTelegramPollingActive`, `databaseRetryCount`, `telegramRestartCount`, `lastDatabaseError`, `lastTelegramError`.
     - `GET /health` — `{"status": "Healthy" / "Degraded", "isDatabaseReady": ..., "isTelegramPollingActive": ...}`.
     - `GET /ping` — `pong` (Keep-Alive для cron-job.org / Render).
  7. **Тесты и верификация**:
     - Добавлены `BotLifecycleCoordinatorTests`, `DatabaseBootstrapWorkerTests`, `SelfHealingTelegramBotWorkerTests`.
     - Все 337 тестов успешно пройдены (100% green, 0 warnings).
- **Далее**: Деплой на Render с указанием `DEFAULT_CONNECTION` в Environment Variables.

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

---

## Бэклог и открытые вопросы
- _Формируется исключительно на основе требований и задач от пользователя._
