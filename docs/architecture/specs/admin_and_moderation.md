# Спецификация: Панель администратора и Модерация (Admin & Moderation)

## 1. Назначение модуля

Модуль обеспечивает функциональность оперативного администрирования бота, модерации контента по жалобам пользователей, сквозного мониторинга базы анкет, аналитики для продажи рекламы (медиакит) и проведения таргетированных рассылок.

---

## 2. Контракты сервисов

### 2.1. `IModerationService`

```csharp
public record ModerationActionResult(
    Guid ReportId,
    long ReporterTelegramId,
    AppLanguage ReporterLanguage,
    long ReportedTelegramId,
    AppLanguage ReportedLanguage,
    string? ReportedName,
    bool ShouldNotifyReporter
);

public record UnbanActionResult(
    long TelegramId,
    AppLanguage Language,
    bool HasCompletedProfile
);

public interface IModerationService
{
    Task<Result<ModerationActionResult>> BanUserByReportAsync(Guid reportId, CancellationToken cancellationToken = default);
    Task<Result<ModerationActionResult>> DeleteProfileByReportAsync(Guid reportId, CancellationToken cancellationToken = default);
    Task<Result> IgnoreReportAsync(Guid reportId, CancellationToken cancellationToken = default);
    Task<Result<UnbanActionResult>> UnbanUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<Result<UnbanActionResult>> UnbanUserByTelegramIdAsync(long telegramId, CancellationToken cancellationToken = default);
}
```

### 2.2. `IAdminService`

```csharp
public interface IAdminService
{
    bool IsAdmin(long telegramId);
    Task<AdminStatsDto> GetOverallStatsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AdminCityStatsDto>> GetTopCitiesStatsAsync(int count = 10, CancellationToken cancellationToken = default);
    Task<AdminCityStatsDto?> GetCityStatsAsync(string cityName, CancellationToken cancellationToken = default);
    Task<int> GetBroadcastAudienceCountAsync(AdminBroadcastFilterDto filter, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<long>> GetBroadcastRecipientTelegramIdsAsync(AdminBroadcastFilterDto filter, CancellationToken cancellationToken = default);
    Task<int> GetPendingReportsCountAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AdminPendingReportDto>> GetPendingReportsAsync(int skip = 0, int take = 10, CancellationToken cancellationToken = default);
    Task<(UserProfileDto? Profile, int TotalCount, int CurrentIndex)> GetAdminProfileByGenderAsync(Gender gender, int offset, CancellationToken cancellationToken = default);
    Task<Result<AdminModerationActionResult>> BanUserDirectlyAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<Result<AdminModerationActionResult>> DeleteUserProfileDirectlyAsync(Guid userId, CancellationToken cancellationToken = default);
}
```

---

## 3. Функциональные возможности

### 3.1. Интерактивная модерация жалоб в 1 клик
- При поступлении жалобы (`ProfileReport`) администраторы получают уведомление с карточкой нарушителя и инлайн-кнопками:
  - 🚫 **«Заблокировать пользователя»** (`adm_ban:{reportId}`): Перевод в `UserState.Banned`, деактивация анкеты (`IsCompleted = false`), уведомление заявителя и нарушителя.
  - 🗑 **«Удалить анкету»** (`adm_del:{reportId}`): Сброс полей анкеты, перевод в `UserState.None` / `Registration_SelectingLanguage`, предупреждение нарушителю.
  - 👁 **«Проигнорировать»** (`adm_ign:{reportId}`): Закрытие жалобы (`IsResolved = true`, `ResolvedAt = DateTime.UtcNow`) без санкций.
- Сообщение администратора автоматически редактируется для фиксации решения.

### 3.2. Аналитика и медиакит для рекламодателей
- **Общая динамика аудитории**:
  - Всего зарегистрированных пользователей.
  - Прирост за 24 часа, 7 дней, 30 дней.
  - Активные анкеты (`IsCompleted = 1`).
- **Демографические срезы**:
  - Соотношение по полу (Парни / Девушки) в абсолютном и процентном выражении.
  - Распределение по возрастным категориям (<18, 18–25, 25–30, 30–40, 40+).
  - Распределение по целям знакомства (`Friends`, `Relationship`, `AdultOnly`).
  - Топ-10 городов и топ стран.
- **Точечная аналитика по городу**:
  - Расчет объема аудитории и демографии для конкретного запрашиваемого города.

### 3.3. Конструктор таргетированных рассылок (`AdminBroadcastService`)
- Таргетинг по полу (`GenderFilter`), городу проживания и языку интерфейса (`AppLanguage`).
- Поддержка форматирования сообщений (текст, фото, опциональная инлайн URL-кнопка).
- Пакетная отправка с регулируемым троттлингом (до 25–30 сообщений/сек) для предотвращения блокировок Telegram Rate Limit (`429 Too Many Requests`).

### 3.4. Сквозной просмотр базы анкет
- Непрерывный просмотр всех анкет базы выбранного пола (без ограничений категорий целей и скоринга).
- Отображение скрытого AI-описания (`AiDescription`), публичного приветствия (`Greeting`), рейтинга и прямой ссылки на Telegram-аккаунт.
- Прямые кнопки бана и удаления анкеты с экрана просмотра.

### 3.5. Платная система автоматического разбана (Telegram Stars)
- К каждому уведомлению о блокировке нарушителя (`Notification_ViolatorBanned`) и экрану заблокированного пользователя прикрепляется инлайн-кнопка **«⭐ Разблокировать за 100 звёзд»** (`pay_unban`).
- При нажатии бот отправляет нативный инвойс Telegram Stars (`SendInvoice`):
  - Валюта: `XTR` (Telegram Stars).
  - Стоимость: `100` звёзд.
  - Защищенный payload: `unban:{userId}`.
- Обработка жизненного цикла платежа:
  - `UpdateType.PreCheckoutQuery`: бот валидирует payload и подтверждает заказ (`AnswerPreCheckoutQueryAsync(ok: true)`).
  - `SuccessfulPayment`: бот вызывает `IModerationService.UnbanUserAsync(userId)`, возвращает пользователя в `UserState.Active` (с восстановлением `IsCompleted = true`), отправляет уведомление `Notification_UnbanSuccessful` и reply-клавиатуру главного меню.

---

## 4. Безопасность и разграничение доступа

- Доступ к панели администратора предоставляется строго по списку идентификаторов Telegram из конфигурации `BotConfiguration:AdminIds`.
- Все админ-экраны и уведомления локализованы на 6 поддерживаемых языков через `LocalizationService`.
