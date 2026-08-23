# Летопись прогресса проекта DatingBot

state_version: 23
updated: 2026-08-24

---

## Сейчас
- **Фаза**: Подготовка к облачному развертыванию на **Render** с поддержкой **HTTP Keep-Alive (cron-job.org)** и безопасной конфигурации через **Environment Variables**:
  1. **Web Host на ASP.NET Core (`Microsoft.NET.Sdk.Web`)**: Проект `DatingBot.Bot` переведен на Web-хост с Kestrel Minimal API. Бот продолжает работать в режиме Long Polling через `IHostedService` (`TelegramBotWorker`, `MatchmakingNotificationWorker`, `InactivityNotificationWorker`).
  2. **Keep-Alive & Health Check эндпоинты**:
     - `GET /` — JSON-статус сервиса (`{"service": "DatingBot", "status": "running", "serverTimeUtc": "..."}`).
     - `GET /ping` — текстовый ответ `pong` с минимальным оверхедом (идеально для пинга с cron-job.org каждые 5–10 мин против засыпания бесплатного инстанса Render).
     - `GET /health` и `GET /healthz` — health check эндпоинты со статусом `{"status": "Healthy"}`.
  3. **Исправление сбоя FileSystemWatcher (status 139 / inotify) в Linux-контейнерах**:
     - В `Program.cs` и `Dockerfile` добавлен `DOTNET_USE_POLLING_FILE_WATCHER=true` и `DOTNET_EnableDiagnostics=0`.
     - Все `reloadOnChange` переведены в `false` для статической контейнерной среды.
  4. **Надежное определение строки подключения (`DependencyInjection.ResolveConnectionString`)**:
     - Приоритет переменных окружения: `DEFAULT_CONNECTION` -> `DATABASE_URL` -> `ConnectionStrings:DefaultConnection`.
     - `appsettings.json` очищен от хардкода `(localdb)`, исключено маскирование переменных окружения.
     - Добавлена валидация платформы: при запуске на Linux без удаленной БД выдается понятное сообщение с инструкцией по настройке переменных в Render вместо сбоя LocalDB (`PlatformNotSupportedException`).
  5. **Поддержка динамического порта Render**: Автоматическое считывание переменной `PORT` (`http://0.0.0.0:${PORT}`).
  6. **Универсальные Environment Variables**:
     - `BOT_TOKEN` или `BotConfiguration__BotToken` (токен бота).
     - `DEFAULT_CONNECTION` или `ConnectionStrings__DefaultConnection` (строка подключения к SmarterASP.NET MS SQL).
     - `ADMIN_IDS` или `BotConfiguration__AdminIds` (список ID админов, поддерживает как массив, так и разделение через запятую: `"123456, 789012"`).
     - `BotConfiguration__UnbanPriceStars` (цена платного разбана).
     - `BotConfiguration__InactivityReminderDays` (порог неактивности).
  7. **Production Dockerfile и .dockerignore**: Создан легковесный multi-stage Dockerfile для сборки и запуска .NET 9 на Render.
  8. **Тесты и верификация**: Добавлены тесты `ConnectionStringResolutionTests`, `HttpKeepAliveEndpointTests`, `AdminSettingsTests` и `BotSetupTests`. Все 326 тестов пройдены (100% green).
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
- [x] Покрытие тестами (322 теста успешно пройдены).
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

---

## Бэклог и открытые вопросы
- _Формируется исключительно на основе требований и задач от пользователя._
