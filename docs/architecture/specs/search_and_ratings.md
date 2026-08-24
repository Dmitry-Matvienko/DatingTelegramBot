# Спецификация: Оценивание, Симпатии и Жалобы (Search & Ratings)

## 1. Назначение модуля

Модуль `SearchService` обеспечивает процесс просмотра анкет, выставления оценок (1–10 ⭐), детекции взаимных симпатий (Mutual Match), обработки очереди входящих лайков и подачи жалоб модераторам.

---

## 2. Контракт сервиса `ISearchService`

```csharp
public interface ISearchService
{
    Task<UserProfileDto?> GetNextCandidateAsync(long telegramId, CancellationToken cancellationToken = default);
    Task<MatchCandidateDto?> GetNextMatchCandidateAsync(long telegramId, CancellationToken cancellationToken = default);
    Task<Result<RatingResult>> RateCandidateAsync(long fromTelegramId, Guid targetProfileId, int score, CancellationToken cancellationToken = default);
    Task<IncomingRatingDto?> GetNextIncomingRatingAsync(long telegramId, CancellationToken cancellationToken = default);
    Task<IncomingRatingDto?> GetIncomingRatingByIdAsync(long telegramId, Guid ratingId, CancellationToken cancellationToken = default);
    Task<Result<ReportInfo>> ReportCandidateAsync(long reporterTelegramId, Guid targetProfileId, ReportReason reason, string? details, CancellationToken cancellationToken = default);
    Task<Result> SetReportingStateAsync(long telegramId, Guid targetProfileId, CancellationToken cancellationToken = default);
    Task<Result> ResetHistoryForCityAsync(long telegramId, CancellationToken cancellationToken = default);
    Task ClearCurrentCandidateAsync(long telegramId, CancellationToken cancellationToken = default);
}
```

---

## 3. Система 10-балльного рейтинга

- Пользователь ставит оценку от **1 до 10** через Reply-клавиатуру: `[1] [2] [3] [4] [5] [6] [7] [8] [9] [10]`.
- Пересчет среднего рейтинга анкеты:
  \[
  \text{NewAverage} = \frac{\text{OldAverage} \times \text{OldCount} + \text{Score}}{\text{OldCount} + 1}
  \]
- **Запрет самооценивания**: Нельзя оценить собственную анкету.
- **Запрет дубликатов**: Нельзя оценить одну анкету дважды в рамках одного цикла поиска.

---

## 4. Логика взаимной симпатии (Mutual Match) и уведомлений

```
[Пользователь А оценивает Пользователя Б на Score >= 6]
                            │
                            ▼
[Проверка: оценивал ли Б пользователя А ранее на Score >= 6?]
            │                                  │
      (ДА - Взаимно)                     (НЕТ - Односторонне)
            │                                  │
            ▼                                  ▼
[🎉 Взаимная симпатия!]            [Отправка уведомления пользователю Б:
 Обоим отправляются карточки       "💌 Кто-то оценил вас на {Score}/10 ⭐!"
 с Inline-кнопкой "💬 Написать"     с кнопкой "👀 Показать кто оценил"]
 (https://t.me/ или tg://user?id=)
```

---

## 5. Обработка входящих оценок (Incoming Ratings Queue)

1. Если у пользователя есть непросмотренные входящие оценки \(\ge 6\) баллов, при переходе в поиск ему **в первую очередь** показываются карточки оценивших его людей (`SendRaterCardAsync`):
   - Карточка анкеты отправляется с **нижней Reply-клавиатурой оценивания входящей симпатии** (`GetIncomingRatingReplyKeyboard`: `1️⃣`..`🔟`, «🚨 Пожаловаться», «🏠 Главное меню» — без кнопки «Искать снова»).
   - Следом за карточкой отправляется сообщение-подсказка (*«💬 Вы можете написать этому человеку:»*) с прикрепленной **Inline URL-кнопкой «💬 Написать»** (`GetRaterCardKeyboard`), открывающей прямой диалог в Telegram.
2. Оценив входящую карточку в ответ:
   - При взаимной оценке (\(\ge 6\)) обоим участникам отправляются карточки взаимного мэтча с кнопкой «💬 Написать», поиск останавливается, состояние пользователя сбрасывается в `UserState.Active`, а нижняя клавиатура переключается на **Главное меню** (`[🔍 Искать анкеты] [👤 Мой профиль]`).
   - При оценке \(< 6\) пользователь либо переходит к следующей входящей оценке, либо возвращается к общему пулу поиска.

---

## 6. Модерация, жалобы и обработка администраторами (`ReportCandidateAsync` & `IModerationService`)

- Причины жалобы (`ReportReason`):
  1. `InappropriateContent` — 18+ / непристойный контент.
  2. `IncorrectProfile` — Фейк / некорректная анкета.
  3. `Other` — Другое (с возможностью ввода текстового комментария).
- Запись создается в таблице `ProfileReports`.
- Анкета нарушителя немедленно исключается из дальнейшей поисковой выдачи заявителя.
- Администраторам (`BotConfiguration:AdminIds` из `appsettings.json`) мгновенно отправляется:
  1. Полная анкета нарушителя с фотографией.
  2. Карточка жалобы с данными заявителя (`ReporterId`, `ReporterUsername`), нарушителя, причиной и инлайн-кнопками:
     - 🚫 **«Заблокировать пользователя»** (`adm_ban:{reportId}`)
     - 🗑 **«Удалить анкету»** (`adm_del:{reportId}`)
     - 👁 **«Проигнорировать»** (`adm_ign:{reportId}`)
- **Действия модерации (`IModerationService`)**:
  - `BanUserByReportAsync`: выставляет `UserState.Banned`, деактивирует анкету (`IsCompleted = false`), отправляет заявителю благодарность на его языке, а нарушителю — сообщение о блокировке.
  - `DeleteProfileByReportAsync`: сбрасывает анкету нарушителя (`IsCompleted = false`, поля очищаются), переводит в статус `UserState.None` / `Registration_SelectingLanguage`, отправляет заявителю благодарность на его языке, а нарушителю — предупреждение об удалении анкеты.
  - `IgnoreReportAsync`: отклоняет жалобу без применения санкций и без уведомления заявителя.
- Сообщение у администратора обновляется: инлайн-кнопки удаляются, отображается статус принятого решения.

