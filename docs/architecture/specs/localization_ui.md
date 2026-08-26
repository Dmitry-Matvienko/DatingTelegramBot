# Спецификация: Локализация и Telegram UI (Localization & UI)

## 1. Назначение модуля

Модуль `LocalizationService` обеспечивает полную мультиязычность для 6 поддерживаемых языков, локализацию справочников, корректные грамматические падежи и шаблоны клавиатур Telegram.

---

## 2. Поддерживаемые языки (`AppLanguage`)

| Код | Язык | Флаг / Отображение в меню |
|:---:|---|---|
| `Russian` | Русский | 🇷🇺 Русский |
| `Ukrainian` | Украинский | 🇺🇦 Українська |
| `English` | Английский | 🇬🇧 English |
| `Hindi` | Хинди | 🇮🇳 हिन्दी |
| `Portuguese` | Португальский | 🇧🇷 Português |
| `Indonesian` | Индонезийский | 🇮🇩 Bahasa Indonesia |

---

## 3. Контракт сервиса `ILocalizationService`

```csharp
public interface ILocalizationService
{
    string Get(AppLanguage language, string key);
    string Get(AppLanguage language, string key, params object[] args);
    string GetGenderText(AppLanguage language, Gender? gender);
    string GetTargetGenderText(AppLanguage language, TargetGender? targetGender);
    string GetDatingTargetText(AppLanguage language, DatingTarget? target);
    string GetInterestTitle(AppLanguage language, string key, string fallbackTitle);
    string FormatCommonInterestsBadge(AppLanguage language, int count);
}
```

---

## 4. Грамматические правила и падежные формы

1. **Грамматический род пола vs целевой пол (`Gender` vs `TargetGender`)**:
   - `Gender`: Именительный падеж («Парень 👦», «Девушка 👧»).
   - `TargetGender`: Винительный падеж («Парня 👦», «Девушку 👧», «Всех 👥»).
2. **Склонение числительных в бейдже общих интересов (`FormatCommonInterestsBadge`)**:
   - Русский: *1 общий интерес*, *2–4 общих интереса*, *5+ общих интересов*.
   - Украинский: *1 спільний інтерес*, *2–4 спільні інтереси*, *5+ спільних інтересів*.
   - Английский: *1 common interest*, *2+ common interests*.
   - Португальский: *1 interesse em comum*, *2+ interesses em comum*.

---

## 5. Стандарты Telegram UI и клавиатур

1. **Инлайн-клавиатуры (`InlineKeyboardMarkup`)**:
   - Каждое действие анкеты (лайк, дизлайк, редактирование, жалобы, переход к диалогу «💬 Написать») формируется фабриками в `src/DatingBot.Bot/Keyboards/`.
   - Все callback-запросы **обязательно** подтверждаются вызовом `botClient.AnswerCallbackQuery(...)`.
   - При перерисовке разметки на месте (`EditMessageReplyMarkup`) строго передается текущий язык пользователя `lang`.
   - Кнопка «💬 Написать» (`Btn_SendMessage`) формируется через `TelegramUrlHelper.GetUserProfileUrl` (`https://t.me/` или `tg://user?id=`) и крепится под карточками симпатий, взаимных мэтчей и поиска анкет в админке.
2. **Reply-клавиатуры (`ReplyKeyboardMarkup`)**:
   - Главное меню: 1-я строка: `[🔍 Искать анкеты]`, `[👤 Мой профиль]`; 2-я строка: `[🎁 Реферальная программа]`, `[📖 Руководство бота]`.
   - Панель оценивания анкеты: кнопки `[1]` .. `[10]` и `[🚨 Пожаловаться]`, `[🏠 Главное меню]`.
3. **Клавиатуры реферальной программы (`ReferralKeyboards`)**:
   - Inline-клавиатура: `[📋 Мои реферальные ссылки]` (`ref_my_links`), `[➕ Создать ссылку]` (`ref_create_link`).
3. **Форматирование карточек**:
   - Используется `ParseMode.Html` с безопасным экранированием пользовательского ввода.
   - Ссылка на профиль формируется как `<a href="tg://user?id={TelegramId}">{Name}</a>` или `@username`.
