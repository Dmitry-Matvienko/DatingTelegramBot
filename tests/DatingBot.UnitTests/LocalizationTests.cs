using DatingBot.Application.Services;
using DatingBot.Application.Validators;
using DatingBot.Domain.Enums;
using FluentAssertions;
using Xunit;

namespace DatingBot.UnitTests;

public class LocalizationTests
{
    private readonly LocalizationService _loc = new();
    private readonly NameValidator _nameValidator = new();
    private readonly CityValidator _cityValidator = new();

    [Theory]
    [InlineData("Іван", true)]
    [InlineData("Олександр", true)]
    [InlineData("Євген", true)]
    [InlineData("राहुल", true)]
    [InlineData("João", true)]
    [InlineData("Gonçalves", true)]
    [InlineData("Budi", true)]
    [InlineData("Alex123", false)]
    public async Task NameValidator_ShouldSupportUnicodeCharactersAcrossAllLanguages(string name, bool expectedValid)
    {
        var result = await _nameValidator.ValidateAsync(name);
        result.IsValid.Should().Be(expectedValid);
    }

    [Theory]
    [InlineData("Київ", true)]
    [InlineData("Lviv", true)]
    [InlineData("São Paulo", true)]
    [InlineData("Saint-Denis", true)]
    [InlineData("Дніпро", true)]
    [InlineData("मुंबई", true)]
    [InlineData("Jakarta", true)]
    [InlineData("123City", false)]
    public async Task CityValidator_ShouldSupportUnicodeCharactersAndHyphens(string city, bool expectedValid)
    {
        var result = await _cityValidator.ValidateAsync(city);
        result.IsValid.Should().Be(expectedValid);
    }

    [Fact]
    public void FormatCommonInterestsBadge_ShouldCorrectlyPluralizePerLanguage()
    {
        // Russian
        _loc.FormatCommonInterestsBadge(AppLanguage.Russian, 1).Should().Contain("1 общий интерес");
        _loc.FormatCommonInterestsBadge(AppLanguage.Russian, 2).Should().Contain("2 общих интереса");
        _loc.FormatCommonInterestsBadge(AppLanguage.Russian, 5).Should().Contain("5 общих интересов");
        _loc.FormatCommonInterestsBadge(AppLanguage.Russian, 11).Should().Contain("11 общих интересов");

        // Ukrainian
        _loc.FormatCommonInterestsBadge(AppLanguage.Ukrainian, 1).Should().Contain("1 спільний інтерес");
        _loc.FormatCommonInterestsBadge(AppLanguage.Ukrainian, 3).Should().Contain("3 спільні інтереси");
        _loc.FormatCommonInterestsBadge(AppLanguage.Ukrainian, 7).Should().Contain("7 спільних інтересів");

        // English
        _loc.FormatCommonInterestsBadge(AppLanguage.English, 1).Should().Contain("1 common interest");
        _loc.FormatCommonInterestsBadge(AppLanguage.English, 3).Should().Contain("3 common interests");

        // Portuguese
        _loc.FormatCommonInterestsBadge(AppLanguage.Portuguese, 1).Should().Contain("1 interesse em comum");
        _loc.FormatCommonInterestsBadge(AppLanguage.Portuguese, 2).Should().Contain("2 interesses em comum");
    }

    [Theory]
    [InlineData(AppLanguage.Russian, "Error_NameLetters", "Имя может содержать только буквы")]
    [InlineData(AppLanguage.Ukrainian, "Error_NameLetters", "Ім'я може містити лише літери")]
    [InlineData(AppLanguage.English, "Error_NameLetters", "Name can only contain letters")]
    [InlineData(AppLanguage.Hindi, "Error_NameLetters", "नाम में केवल अक्षर")]
    [InlineData(AppLanguage.Portuguese, "Error_NameLetters", "O nome pode conter apenas letras")]
    [InlineData(AppLanguage.Indonesian, "Error_NameLetters", "Nama hanya boleh berisi huruf")]
    public void Error_NameLetters_ShouldBeTranslatedForEachLanguage(AppLanguage language, string key, string expectedSubstring)
    {
        var text = _loc.Get(language, key);
        text.Should().Contain(expectedSubstring);
    }

    [Theory]
    [InlineData(AppLanguage.Russian, "Notification_ReportResolved", "жалоба была обработана")]
    [InlineData(AppLanguage.Ukrainian, "Notification_ReportResolved", "скаргу було оброблено")]
    [InlineData(AppLanguage.English, "Notification_ReportResolved", "report has been processed")]
    [InlineData(AppLanguage.Hindi, "Notification_ReportResolved", "शिकायत संसाधित")]
    [InlineData(AppLanguage.Portuguese, "Notification_ReportResolved", "denúncia foi processada")]
    [InlineData(AppLanguage.Indonesian, "Notification_ReportResolved", "laporan Anda telah diproses")]
    public void Notification_ReportResolved_ShouldBeTranslatedForEachLanguage(AppLanguage language, string key, string expectedSubstring)
    {
        var text = _loc.Get(language, key);
        text.Should().Contain(expectedSubstring);
    }

    [Theory]
    [InlineData(AppLanguage.Russian, "Notification_ViolatorBanned", "заблокирована")]
    [InlineData(AppLanguage.Ukrainian, "Notification_ViolatorBanned", "заблоковано")]
    [InlineData(AppLanguage.English, "Notification_ViolatorBanned", "banned")]
    [InlineData(AppLanguage.Hindi, "Notification_ViolatorBanned", "ब्लॉक")]
    [InlineData(AppLanguage.Portuguese, "Notification_ViolatorBanned", "banida")]
    [InlineData(AppLanguage.Indonesian, "Notification_ViolatorBanned", "diblokir")]
    public void Notification_ViolatorBanned_ShouldBeTranslatedForEachLanguage(AppLanguage language, string key, string expectedSubstring)
    {
        var text = _loc.Get(language, key);
        text.Should().Contain(expectedSubstring);
    }

    [Theory]
    [InlineData(AppLanguage.Russian, "Notification_ViolatorProfileDeleted", "анкета удалена")]
    [InlineData(AppLanguage.Ukrainian, "Notification_ViolatorProfileDeleted", "анкету видалено")]
    [InlineData(AppLanguage.English, "Notification_ViolatorProfileDeleted", "profile has been deleted")]
    [InlineData(AppLanguage.Hindi, "Notification_ViolatorProfileDeleted", "प्रोफ़ाइल हटा")]
    [InlineData(AppLanguage.Portuguese, "Notification_ViolatorProfileDeleted", "perfil foi excluído")]
    [InlineData(AppLanguage.Indonesian, "Notification_ViolatorProfileDeleted", "Profil Anda telah dihapus")]
    public void Notification_ViolatorProfileDeleted_ShouldBeTranslatedForEachLanguage(AppLanguage language, string key, string expectedSubstring)
    {
        var text = _loc.Get(language, key);
        text.Should().Contain(expectedSubstring);
    }

    [Theory]
    [InlineData(AppLanguage.Russian, "Admin_Welcome", "Администратор")]
    [InlineData(AppLanguage.Ukrainian, "Admin_Welcome", "Адміністратор")]
    [InlineData(AppLanguage.English, "Admin_Welcome", "Administrator")]
    [InlineData(AppLanguage.Hindi, "Admin_Welcome", "व्यवस्थापक")]
    [InlineData(AppLanguage.Portuguese, "Admin_Welcome", "Administrador")]
    [InlineData(AppLanguage.Indonesian, "Admin_Welcome", "Administrator")]
    public void Admin_Welcome_ShouldBeTranslatedForEachLanguage(AppLanguage language, string key, string expectedSubstring)
    {
        var text = _loc.Get(language, key);
        text.Should().Contain(expectedSubstring);
    }

    [Theory]
    [InlineData(AppLanguage.Russian, "Admin_Btn_Stats", "Статистика")]
    [InlineData(AppLanguage.Ukrainian, "Admin_Btn_Stats", "Статистика")]
    [InlineData(AppLanguage.English, "Admin_Btn_Stats", "Audience")]
    [InlineData(AppLanguage.Hindi, "Admin_Btn_Stats", "सांख्यिकी")]
    [InlineData(AppLanguage.Portuguese, "Admin_Btn_Stats", "Estatísticas")]
    [InlineData(AppLanguage.Indonesian, "Admin_Btn_Stats", "Statistik")]
    public void Admin_Btn_Stats_ShouldBeTranslatedForEachLanguage(AppLanguage language, string key, string expectedSubstring)
    {
        var text = _loc.Get(language, key);
        text.Should().Contain(expectedSubstring);
    }

    [Theory]
    [InlineData(AppLanguage.Russian, "Admin_Broadcast_Audience_Friends", "общения")]
    [InlineData(AppLanguage.Ukrainian, "Admin_Broadcast_Audience_Friends", "спілкування")]
    [InlineData(AppLanguage.English, "Admin_Broadcast_Audience_Friends", "friendship")]
    [InlineData(AppLanguage.Hindi, "Admin_Broadcast_Audience_Friends", "दोस्ती")]
    [InlineData(AppLanguage.Portuguese, "Admin_Broadcast_Audience_Friends", "amizade")]
    [InlineData(AppLanguage.Indonesian, "Admin_Broadcast_Audience_Friends", "pertemanan")]
    public void Admin_Broadcast_Audience_Friends_ShouldBeTranslatedForEachLanguage(AppLanguage language, string key, string expectedSubstring)
    {
        var text = _loc.Get(language, key);
        text.Should().Contain(expectedSubstring);
    }

    [Theory]
    [InlineData(AppLanguage.Russian, "Admin_Broadcast_Audience_Relationship", "отношений")]
    [InlineData(AppLanguage.Ukrainian, "Admin_Broadcast_Audience_Relationship", "стосунків")]
    [InlineData(AppLanguage.English, "Admin_Broadcast_Audience_Relationship", "relationship")]
    [InlineData(AppLanguage.Hindi, "Admin_Broadcast_Audience_Relationship", "रिश्ते")]
    [InlineData(AppLanguage.Portuguese, "Admin_Broadcast_Audience_Relationship", "relacionamento")]
    [InlineData(AppLanguage.Indonesian, "Admin_Broadcast_Audience_Relationship", "hubungan")]
    public void Admin_Broadcast_Audience_Relationship_ShouldBeTranslatedForEachLanguage(AppLanguage language, string key, string expectedSubstring)
    {
        var text = _loc.Get(language, key);
        text.Should().Contain(expectedSubstring);
    }

    [Theory]
    [InlineData(AppLanguage.Russian, "Admin_Broadcast_Audience_Adult", "18+")]
    [InlineData(AppLanguage.Ukrainian, "Admin_Broadcast_Audience_Adult", "18+")]
    [InlineData(AppLanguage.English, "Admin_Broadcast_Audience_Adult", "18+")]
    [InlineData(AppLanguage.Hindi, "Admin_Broadcast_Audience_Adult", "18+")]
    [InlineData(AppLanguage.Portuguese, "Admin_Broadcast_Audience_Adult", "18+")]
    [InlineData(AppLanguage.Indonesian, "Admin_Broadcast_Audience_Adult", "18+")]
    public void Admin_Broadcast_Audience_Adult_ShouldBeTranslatedForEachLanguage(AppLanguage language, string key, string expectedSubstring)
    {
        var text = _loc.Get(language, key);
        text.Should().Contain(expectedSubstring);
    }

    [Theory]
    [InlineData(AppLanguage.Russian, "Admin_Btn_Revenue", "Доход")]
    [InlineData(AppLanguage.Ukrainian, "Admin_Btn_Revenue", "Дохід")]
    [InlineData(AppLanguage.English, "Admin_Btn_Revenue", "Revenue")]
    [InlineData(AppLanguage.Hindi, "Admin_Btn_Revenue", "आय")]
    [InlineData(AppLanguage.Portuguese, "Admin_Btn_Revenue", "Receita")]
    [InlineData(AppLanguage.Indonesian, "Admin_Btn_Revenue", "Pendapatan")]
    public void Admin_Btn_Revenue_ShouldBeTranslatedForEachLanguage(AppLanguage language, string key, string expectedSubstring)
    {
        var text = _loc.Get(language, key);
        text.Should().Contain(expectedSubstring);
    }

    [Theory]
    [InlineData(AppLanguage.Russian, "Admin_Revenue_Btn_Balance", "Баланс")]
    [InlineData(AppLanguage.Ukrainian, "Admin_Revenue_Btn_Balance", "Баланс")]
    [InlineData(AppLanguage.English, "Admin_Revenue_Btn_Balance", "Balance")]
    [InlineData(AppLanguage.Hindi, "Admin_Revenue_Btn_Balance", "शेष")]
    [InlineData(AppLanguage.Portuguese, "Admin_Revenue_Btn_Balance", "Saldo")]
    [InlineData(AppLanguage.Indonesian, "Admin_Revenue_Btn_Balance", "Saldo")]
    public void Admin_Revenue_Btn_Balance_ShouldBeTranslatedForEachLanguage(AppLanguage language, string key, string expectedSubstring)
    {
        var text = _loc.Get(language, key);
        text.Should().Contain(expectedSubstring);
    }

    [Theory]
    [InlineData(AppLanguage.Russian, "Admin_Revenue_Btn_History", "История транзакций")]
    [InlineData(AppLanguage.Ukrainian, "Admin_Revenue_Btn_History", "Історія транзакцій")]
    [InlineData(AppLanguage.English, "Admin_Revenue_Btn_History", "Transaction History")]
    [InlineData(AppLanguage.Hindi, "Admin_Revenue_Btn_History", "लेनदेन इतिहास")]
    [InlineData(AppLanguage.Portuguese, "Admin_Revenue_Btn_History", "Histórico de Transações")]
    [InlineData(AppLanguage.Indonesian, "Admin_Revenue_Btn_History", "Riwayat Transaksi")]
    public void Admin_Revenue_Btn_History_ShouldBeTranslatedForEachLanguage(AppLanguage language, string key, string expectedSubstring)
    {
        var text = _loc.Get(language, key);
        text.Should().Contain(expectedSubstring);
    }
}
