using DatingBot.Domain.Enums;

namespace DatingBot.Application.Interfaces;

public interface ILocalizationService
{
    string Get(AppLanguage language, string key, params object[] args);
    string GetGenderText(AppLanguage language, Gender? gender);
    string GetTargetGenderText(AppLanguage language, TargetGender? targetGender);
    string GetDatingTargetText(AppLanguage language, DatingTarget? target);
    string GetInterestTitle(AppLanguage language, string key, string fallbackTitle);
    string GetMatchBadge(AppLanguage language, string badgeKey, params object[] args);
    string FormatCommonInterestsBadge(AppLanguage language, int count);
    string GetRandomSearchTip(AppLanguage language);
    IReadOnlyList<string> GetAllSearchTipKeys();
}
