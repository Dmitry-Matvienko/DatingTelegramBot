using DatingBot.Application.DTOs;
using DatingBot.Application.Interfaces;
using DatingBot.Application.Services;
using DatingBot.Domain.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace DatingBot.Bot.Keyboards;

public static class ProfileKeyboards
{
    private static readonly ILocalizationService Loc = new LocalizationService();

    public static InlineKeyboardMarkup GetProfileEditKeyboard(AppLanguage language = AppLanguage.Russian)
    {
        var nameLabel = Loc.Get(language, "Label_Name");
        var ageLabel = Loc.Get(language, "Label_Age");

        return new InlineKeyboardMarkup([
            [
                InlineKeyboardButton.WithCallbackData($"👤 {nameLabel}", "edit:name"),
                InlineKeyboardButton.WithCallbackData($"🎂 {ageLabel}", "edit:age")
            ],
            [
                InlineKeyboardButton.WithCallbackData($"📍 {Loc.Get(language, "Label_City")}", "edit:city"),
                InlineKeyboardButton.WithCallbackData($"📏 {Loc.Get(language, "Label_Height")}", "edit:height")
            ],
            [
                InlineKeyboardButton.WithCallbackData(Loc.Get(language, "Btn_Photo"), "edit:photo"),
                InlineKeyboardButton.WithCallbackData($"🚻 {Loc.Get(language, "Label_Gender")}", "edit:gender")
            ],
            [
                InlineKeyboardButton.WithCallbackData(Loc.Get(language, "Btn_Filters"), "edit:search_params"),
                InlineKeyboardButton.WithCallbackData($"🎯 {Loc.Get(language, "Label_Target")}", "edit:target")
            ],
            [
                InlineKeyboardButton.WithCallbackData($"🏷 {Loc.Get(language, "Label_Interests")}", "edit:interests"),
                InlineKeyboardButton.WithCallbackData(Loc.Get(language, "Btn_AiBio"), "edit:ai_bio")
            ],
            [
                InlineKeyboardButton.WithCallbackData(Loc.Get(language, "Btn_Greeting"), "edit:greeting"),
                InlineKeyboardButton.WithCallbackData($"{Loc.Get(language, "Menu_Language")}", "edit:language")
            ],
            [
                InlineKeyboardButton.WithCallbackData(Loc.Get(language, "Btn_MainMenu"), "edit:main_menu")
            ]
        ]);
    }

    public static InlineKeyboardMarkup GetCancelKeyboard(AppLanguage language = AppLanguage.Russian)
    {
        return new InlineKeyboardMarkup([
            [
                InlineKeyboardButton.WithCallbackData(Loc.Get(language, "Btn_Cancel"), "edit:cancel")
            ]
        ]);
    }

    public static InlineKeyboardMarkup GetEditHeightKeyboard(AppLanguage language = AppLanguage.Russian)
    {
        return new InlineKeyboardMarkup([
            [
                InlineKeyboardButton.WithCallbackData(Loc.Get(language, "Btn_RemoveHeight"), "edit:height_remove")
            ],
            [
                InlineKeyboardButton.WithCallbackData(Loc.Get(language, "Btn_Cancel"), "edit:cancel")
            ]
        ]);
    }

    public static InlineKeyboardMarkup GetEditGenderKeyboard(AppLanguage language = AppLanguage.Russian)
    {
        return new InlineKeyboardMarkup([
            [
                InlineKeyboardButton.WithCallbackData(Loc.Get(language, "Gender_Male"), "edit_gender:1"),
                InlineKeyboardButton.WithCallbackData(Loc.Get(language, "Gender_Female"), "edit_gender:2")
            ],
            [
                InlineKeyboardButton.WithCallbackData(Loc.Get(language, "Btn_Cancel"), "edit:cancel")
            ]
        ]);
    }

    public static InlineKeyboardMarkup GetEditTargetGenderKeyboard(AppLanguage language = AppLanguage.Russian)
    {
        return new InlineKeyboardMarkup([
            [
                InlineKeyboardButton.WithCallbackData(Loc.Get(language, "TargetGender_Male"), "edit_target_gender:1"),
                InlineKeyboardButton.WithCallbackData(Loc.Get(language, "TargetGender_Female"), "edit_target_gender:2")
            ],
            [
                InlineKeyboardButton.WithCallbackData(Loc.Get(language, "TargetGender_All"), "edit_target_gender:3")
            ],
            [
                InlineKeyboardButton.WithCallbackData(Loc.Get(language, "Btn_Cancel"), "edit:cancel")
            ]
        ]);
    }

    public static InlineKeyboardMarkup GetEditDatingTargetKeyboard(AppLanguage language = AppLanguage.Russian)
    {
        return new InlineKeyboardMarkup([
            [InlineKeyboardButton.WithCallbackData(Loc.Get(language, "Target_Friends"), "edit_target:1")],
            [InlineKeyboardButton.WithCallbackData(Loc.Get(language, "Target_Relationship"), "edit_target:2")],
            [InlineKeyboardButton.WithCallbackData(Loc.Get(language, "Target_AdultOnly"), "edit_target:3")],
            [InlineKeyboardButton.WithCallbackData(Loc.Get(language, "Btn_Cancel"), "edit:cancel")]
        ]);
    }

    public static InlineKeyboardMarkup GetSearchPreferencesKeyboard(AgeCategoryFilter selectedFilters, AppLanguage language = AppLanguage.Russian)
    {
        var under18Check = selectedFilters.HasFlag(AgeCategoryFilter.Under18) ? "✅" : "❌";
        var age18To25Check = selectedFilters.HasFlag(AgeCategoryFilter.Age18To25) ? "✅" : "❌";
        var age25To30Check = selectedFilters.HasFlag(AgeCategoryFilter.Age25To30) ? "✅" : "❌";
        var age30To40Check = selectedFilters.HasFlag(AgeCategoryFilter.Age30To40) ? "✅" : "❌";
        var age40PlusCheck = selectedFilters.HasFlag(AgeCategoryFilter.Age40Plus) ? "✅" : "❌";

        return new InlineKeyboardMarkup([
            [
                InlineKeyboardButton.WithCallbackData($"{under18Check} 👶 < 18", "edit_age_cat:1")
            ],
            [
                InlineKeyboardButton.WithCallbackData($"{age18To25Check} 18–25", "edit_age_cat:2"),
                InlineKeyboardButton.WithCallbackData($"{age25To30Check} 25–30", "edit_age_cat:4")
            ],
            [
                InlineKeyboardButton.WithCallbackData($"{age30To40Check} 30–40", "edit_age_cat:8"),
                InlineKeyboardButton.WithCallbackData($"{age40PlusCheck} 40+", "edit_age_cat:16")
            ],
            [
                InlineKeyboardButton.WithCallbackData(Loc.Get(language, "Btn_CustomAgeRange"), "edit:custom_age_range")
            ],
            [
                InlineKeyboardButton.WithCallbackData(Loc.Get(language, "Btn_SearchDistance"), "edit:search_distance")
            ],
            [
                InlineKeyboardButton.WithCallbackData(Loc.Get(language, "Btn_Done"), "edit_age_cat_save")
            ],
            [
                InlineKeyboardButton.WithCallbackData(Loc.Get(language, "Btn_Cancel"), "edit:cancel")
            ]
        ]);
    }

    public static InlineKeyboardMarkup GetEditSearchDistanceKeyboard(SearchDistancePreference currentDistance, AppLanguage language = AppLanguage.Russian)
    {
        var check100 = currentDistance == SearchDistancePreference.UpTo100Km ? "✅ " : "";
        var check500 = currentDistance == SearchDistancePreference.UpTo500Km ? "✅ " : "";
        var checkCountry = currentDistance == SearchDistancePreference.SameCountry ? "✅ " : "";
        var checkAnywhere = currentDistance == SearchDistancePreference.Anywhere ? "✅ " : "";

        return new InlineKeyboardMarkup([
            [
                InlineKeyboardButton.WithCallbackData($"{check100}{Loc.Get(language, "Distance_UpTo100Km")}", "edit_distance:1"),
                InlineKeyboardButton.WithCallbackData($"{check500}{Loc.Get(language, "Distance_UpTo500Km")}", "edit_distance:2")
            ],
            [
                InlineKeyboardButton.WithCallbackData($"{checkCountry}{Loc.Get(language, "Distance_SameCountry")}", "edit_distance:3")
            ],
            [
                InlineKeyboardButton.WithCallbackData($"{checkAnywhere}{Loc.Get(language, "Distance_Anywhere")}", "edit_distance:4")
            ],
            [
                InlineKeyboardButton.WithCallbackData(Loc.Get(language, "Btn_Cancel"), "edit:search_params")
            ]
        ]);
    }

    public static InlineKeyboardMarkup GetEditInterestsKeyboard(IReadOnlyList<InterestDto> interests, AppLanguage language = AppLanguage.Russian)
    {
        var rows = new List<List<InlineKeyboardButton>>();

        for (var i = 0; i < interests.Count; i += 2)
        {
            var row = new List<InlineKeyboardButton>();

            var item1 = interests[i];
            var prefix1 = item1.IsSelected ? "✅" : "❌";
            var title1 = Loc.GetInterestTitle(language, item1.Code.ToString().ToLowerInvariant(), item1.Title);
            row.Add(InlineKeyboardButton.WithCallbackData($"{prefix1} {item1.Icon} {title1}", $"edit_interest_toggle:{(int)item1.Code}"));

            if (i + 1 < interests.Count)
            {
                var item2 = interests[i + 1];
                var prefix2 = item2.IsSelected ? "✅" : "❌";
                var title2 = Loc.GetInterestTitle(language, item2.Code.ToString().ToLowerInvariant(), item2.Title);
                row.Add(InlineKeyboardButton.WithCallbackData($"{prefix2} {item2.Icon} {title2}", $"edit_interest_toggle:{(int)item2.Code}"));
            }

            rows.Add(row);
        }

        rows.Add([
            InlineKeyboardButton.WithCallbackData(Loc.Get(language, "Btn_Done"), "edit_interest_save")
        ]);

        rows.Add([
            InlineKeyboardButton.WithCallbackData(Loc.Get(language, "Btn_Cancel"), "edit:cancel")
        ]);

        return new InlineKeyboardMarkup(rows);
    }
}
