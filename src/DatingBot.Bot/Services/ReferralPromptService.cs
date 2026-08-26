using System.Net;
using System.Text;
using DatingBot.Application.Interfaces;
using DatingBot.Bot.Keyboards;
using DatingBot.Domain.Enums;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace DatingBot.Bot.Services;

public class ReferralPromptService(
    ITelegramBotClient botClient,
    IReferralService referralService,
    ILocalizationService loc)
{
    public async Task SendReferralProgramInfoAsync(long chatId, AppLanguage language, bool isAdmin = false, CancellationToken cancellationToken = default)
    {
        await botClient.SendMessage(
            chatId: chatId,
            text: loc.Get(language, "Referral_Info"),
            parseMode: ParseMode.Html,
            replyMarkup: ReferralKeyboards.GetReferralMenuInlineKeyboard(language, loc, isAdmin),
            cancellationToken: cancellationToken
        );
    }

    public async Task SendReferralReportAsync(long chatId, AppLanguage language, CancellationToken cancellationToken = default)
    {
        var result = await referralService.GetTopReferrersAsync(15, cancellationToken);
        if (!result.IsSuccess || result.Value is null || result.Value.Count == 0)
        {
            await botClient.SendMessage(
                chatId: chatId,
                text: loc.Get(language, "Referral_Report_Empty"),
                parseMode: ParseMode.Html,
                cancellationToken: cancellationToken
            );
            return;
        }

        var sb = new StringBuilder();
        sb.AppendLine(loc.Get(language, "Referral_Report_Title"));

        for (var i = 0; i < result.Value.Count; i++)
        {
            var item = result.Value[i];
            var profileUrl = TelegramUrlHelper.GetUserProfileUrl(item.TelegramId, item.Username);
            var encodedName = WebUtility.HtmlEncode(item.Name ?? item.Username ?? "User");
            var clickableName = $"<a href=\"{profileUrl}\">{encodedName}</a>";
            var line = string.Format(loc.Get(language, "Referral_Report_Item"), i + 1, clickableName, item.InvitedCount);
            sb.AppendLine(line);
        }

        await botClient.SendMessage(
            chatId: chatId,
            text: sb.ToString().TrimEnd(),
            parseMode: ParseMode.Html,
            cancellationToken: cancellationToken
        );
    }

    public async Task SendMyReferralLinksAsync(long chatId, AppLanguage language, long telegramId, CancellationToken cancellationToken = default)
    {
        var result = await referralService.GetUserReferralLinkAsync(telegramId, cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            await botClient.SendMessage(
                chatId: chatId,
                text: loc.Get(language, "Referral_NoLinksYet"),
                parseMode: ParseMode.Html,
                cancellationToken: cancellationToken
            );
            return;
        }

        await botClient.SendMessage(
            chatId: chatId,
            text: $"<code>{result.Value.LinkUrl}</code>",
            parseMode: ParseMode.Html,
            cancellationToken: cancellationToken
        );
    }

    public async Task SendCreateReferralLinkAsync(long chatId, AppLanguage language, long telegramId, CancellationToken cancellationToken = default)
    {
        var result = await referralService.CreateOrGetReferralLinkAsync(telegramId, cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            await botClient.SendMessage(
                chatId: chatId,
                text: loc.Get(language, "Referral_NoLinksYet"),
                parseMode: ParseMode.Html,
                cancellationToken: cancellationToken
            );
            return;
        }

        var prefix = loc.Get(language, "Referral_LinkCreated_Prefix");
        var text = $"{prefix}\n\n<code>{result.Value.LinkUrl}</code>";

        await botClient.SendMessage(
            chatId: chatId,
            text: text,
            parseMode: ParseMode.Html,
            cancellationToken: cancellationToken
        );
    }
}
