using DatingBot.Application.DTOs;
using DatingBot.Application.Interfaces;
using DatingBot.Application.Services;
using DatingBot.Domain.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace DatingBot.Bot.Keyboards;

public static class RegistrationKeyboards
{
    private static readonly ILocalizationService Loc = new LocalizationService();

    public static InlineKeyboardMarkup GetGenderKeyboard(AppLanguage language = AppLanguage.Russian)
    {
        return new InlineKeyboardMarkup([
            [
                InlineKeyboardButton.WithCallbackData(Loc.Get(language, "Gender_Male"), "gender_set:1"),
                InlineKeyboardButton.WithCallbackData(Loc.Get(language, "Gender_Female"), "gender_set:2")
            ]
        ]);
    }

    public static InlineKeyboardMarkup GetTargetGenderKeyboard(AppLanguage language = AppLanguage.Russian)
    {
        return new InlineKeyboardMarkup([
            [
                InlineKeyboardButton.WithCallbackData(Loc.Get(language, "TargetGender_Male"), "target_gender_set:1"),
                InlineKeyboardButton.WithCallbackData(Loc.Get(language, "TargetGender_Female"), "target_gender_set:2")
            ],
            [
                InlineKeyboardButton.WithCallbackData(Loc.Get(language, "TargetGender_All"), "target_gender_set:3")
            ]
        ]);
    }

    public static InlineKeyboardMarkup GetHeightKeyboard(AppLanguage language = AppLanguage.Russian)
    {
        return new InlineKeyboardMarkup([
            [
                InlineKeyboardButton.WithCallbackData(Loc.Get(language, "Btn_Skip"), "height_skip")
            ]
        ]);
    }

    public static InlineKeyboardMarkup GetInterestsKeyboard(IReadOnlyList<InterestDto> interests, AppLanguage language = AppLanguage.Russian)
    {
        var rows = new List<List<InlineKeyboardButton>>();

        for (var i = 0; i < interests.Count; i += 2)
        {
            var row = new List<InlineKeyboardButton>();

            var item1 = interests[i];
            var prefix1 = item1.IsSelected ? "✅" : "❌";
            var title1 = Loc.GetInterestTitle(language, item1.Code.ToString().ToLowerInvariant(), item1.Title);
            row.Add(InlineKeyboardButton.WithCallbackData($"{prefix1} {item1.Icon} {title1}", $"interest_toggle:{(int)item1.Code}"));

            if (i + 1 < interests.Count)
            {
                var item2 = interests[i + 1];
                var prefix2 = item2.IsSelected ? "✅" : "❌";
                var title2 = Loc.GetInterestTitle(language, item2.Code.ToString().ToLowerInvariant(), item2.Title);
                row.Add(InlineKeyboardButton.WithCallbackData($"{prefix2} {item2.Icon} {title2}", $"interest_toggle:{(int)item2.Code}"));
            }

            rows.Add(row);
        }

        rows.Add([
            InlineKeyboardButton.WithCallbackData(Loc.Get(language, "Btn_Done"), "interest_done")
        ]);

        return new InlineKeyboardMarkup(rows);
    }

    public static InlineKeyboardMarkup GetDatingTargetKeyboard(AppLanguage language = AppLanguage.Russian)
    {
        return new InlineKeyboardMarkup([
            [InlineKeyboardButton.WithCallbackData(Loc.Get(language, "Target_Friends"), "target_set:1")],
            [InlineKeyboardButton.WithCallbackData(Loc.Get(language, "Target_Relationship"), "target_set:2")],
            [InlineKeyboardButton.WithCallbackData(Loc.Get(language, "Target_AdultOnly"), "target_set:3")]
        ]);
    }
}
