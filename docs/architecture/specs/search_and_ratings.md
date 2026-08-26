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
    Task<Result> ClearCurrentCandidateAsync(long telegramId, CancellationToken cancellationToken = default);
}
```

---

## 3. Система 10-балльного рейтинга и повторное оценивание

- Пользователь ставит оценку от **1 до 10** через Reply-клавиатуру: `[1] [2] [3] [4] [5] [6] [7] [8] [9] [10]`.
- **Первичное оценивание анкеты**:
  \[
  \text{NewAverage} = \frac{\text{OldAverage} \times \text{OldCount} + \text{Score}}{\text{OldCount} + 1}, \quad \text{NewCount} = \text{OldCount} + 1
  \]
- **Повторное оценивание при циклическом поиске**:
  - Если анкета уже оценивалась пользователем в предыдущем цикле, оценка обновляется на новое значение, а средний балл пересчитывается без изменения общего счетчика:
  \[
  \text{NewAverage} = \frac{\text{OldAverage} \times \text{Count} - \text{OldScore} + \text{NewScore}}{\text{Count}}
  \]
  - **Уведомление о недавней оценке (< 24 часов)**: Если анкета попадается повторно и оценивается в течение 24 часов с момента предыдущей оценки, бот отправляет оценивающему пользователю сервисное уведомление: `Notification_AlreadyRatedRecently` (*«ℹ️ Вы уже недавно оценивали этого пользователя.»*) на выбранном языке интерфейса. При этом уведомление о высокой оценке (6+) оцениваемому пользователю **не отправляется**, чтобы исключить дублирование пушей.
- **Запрет самооценивания**: Нельзя оценить собственную анкету.
- **В рамках одного цикла**: Анкета показывается ровно 1 раз до завершения круга всех доступных анкет категории.

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

## 5. Обработка очереди входящих оценок (Incoming Ratings Queue)

1. **Уведомление с количеством людей в очереди**:
   - При выставлении пользователю оценки \(\ge 6\) баллов отправляется уведомление с Inline-кнопкой, отображающей общее количество непросмотренных оценок в очереди: **«👀 Показать кто оценил ({count})»** (`view_rater:{ratingId}`).
2. **Просмотр и автоматическое исключение из очереди (`IsViewed`)**:
   - При переходе к просмотру входящей оценки (`GetNextIncomingRatingAsync` / `GetIncomingRatingByIdAsync`) оценка автоматически помечается как просмотренная (`IsViewed = true`), что исключает повторный показ той же анкеты при повторных кликах.
   - Пользователю отправляется карточка анкеты с сообщением-подсказкой и Inline-кнопкой «💬 Написать».
3. **Reply-клавиатура и кнопка «➡️ Далее» (`GetIncomingRatingReplyKeyboard`)**:
   - Если в очереди после текущей анкеты остается хотя бы один человек (`RemainingQueueCount > 0`), в нижнюю строку Reply-клавиатуры добавляется кнопка **«➡️ Далее»**:
     `[ 🚨 Пожаловаться ] [ ➡️ Далее ] [ 🏠 Главное меню ]`.
   - Если анкета последняя в очереди (`RemainingQueueCount == 0`), кнопка «Далее» не отображается:
     `[ 🚨 Пожаловаться ] [ 🏠 Главное меню ]`.
   - При нажатии **«➡️ Далее»** бот переключает пользователя на следующего человека из очереди входящих симпатий без необходимости ставить оценку.
4. **Оценивание входящей карточки в ответ**:
   - При взаимной оценке (\(\ge 6\)) обоим участникам отправляются карточки взаимного мэтча с кнопкой «💬 Написать», поиск останавливается, состояние пользователя сбрасывается в `UserState.Active`, а нижняя клавиатура переключается на **Главное меню** (`[🔍 Искать анкеты] [👤 Мой профиль]`).
   - При оценке \(< 6\) пользователь либо переходит к следующей входящей оценке (если очередь не пуста), либо возвращается к общему пулу поиска кандидатов.

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

---

## 7. Динамические подсказки в анкетах поиска (Search Profile Tips)

- При показе анкеты кандидата в поиске (`SendMatchCandidateCardAsync`, `SendCandidateCardAsync`) в конец текста карточки добавляется разделитель `——` и случайная динамическая подсказка:
  ```
  ——
  💡 Если оценка 6+, человек сможет написать вам в лс
  ```
- **Пул подсказок**: 15 локализованных шаблонов (`Search_Tip_1` .. `Search_Tip_15`), охватывающих правила взаимной связи, официальные каналы/поддержку (@TheBestDating, @KimeLowe65), безопасность и защиту от мошенников, подсказки по AI-описанию, дальности поиска, приветствию, интересам и качеству фото.
- **Производительность**: Выборка осуществляется в оперативной памяти ($O(1)$ In-Memory lookup через `LocalizationService` и `Random.Shared`) без обращений к базе данных и внешним сервисам.
- **Мультиязычность**: Все 15 подсказок переведены на 6 языков интерфейса (RU, UK, EN, HI, PT, ID).


