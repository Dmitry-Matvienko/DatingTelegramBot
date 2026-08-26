using System.Security.Cryptography;
using DatingBot.Application.Common;
using DatingBot.Application.DTOs;
using DatingBot.Application.Interfaces;
using DatingBot.Domain.Entities;

namespace DatingBot.Application.Services;

public class ReferralService(
    IUserRepository userRepository,
    IUserProfileRepository userProfileRepository,
    IReferralRepository referralRepository,
    IBotInfoProvider botInfoProvider,
    IUnitOfWork unitOfWork) : IReferralService
{
    private const string CodeChars = "abcdefghijklmnopqrstuvwxyz0123456789";

    public async Task<Result<ReferralLinkDto?>> GetUserReferralLinkAsync(long telegramId, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByTelegramIdAsync(telegramId, cancellationToken);
        if (user is null)
            return Result<ReferralLinkDto?>.Failure("Пользователь не найден");

        var link = await referralRepository.GetByUserIdAsync(user.Id, cancellationToken);
        if (link is null)
            return Result<ReferralLinkDto?>.Success(null);

        var botUsername = await botInfoProvider.GetBotUsernameAsync(cancellationToken);
        var linkUrl = $"https://t.me/{botUsername}?start={link.Code}";

        return Result<ReferralLinkDto?>.Success(new ReferralLinkDto(
            link.Id,
            link.Code,
            linkUrl,
            link.InvitedCount,
            link.CreatedAt
        ));
    }

    public async Task<Result<ReferralLinkDto>> CreateOrGetReferralLinkAsync(long telegramId, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByTelegramIdAsync(telegramId, cancellationToken);
        if (user is null)
            return Result<ReferralLinkDto>.Failure("Пользователь не найден");

        var existingLink = await referralRepository.GetByUserIdAsync(user.Id, cancellationToken);
        if (existingLink is not null)
        {
            var botUsername = await botInfoProvider.GetBotUsernameAsync(cancellationToken);
            var linkUrl = $"https://t.me/{botUsername}?start={existingLink.Code}";
            return Result<ReferralLinkDto>.Success(new ReferralLinkDto(
                existingLink.Id,
                existingLink.Code,
                linkUrl,
                existingLink.InvitedCount,
                existingLink.CreatedAt
            ));
        }

        // Генерируем уникальный код ссылки
        string code;
        while (true)
        {
            code = "ref_" + GenerateRandomCode(8);
            var existingByCode = await referralRepository.GetByCodeAsync(code, cancellationToken);
            if (existingByCode is null)
                break;
        }

        var newLink = new ReferralLink
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Code = code,
            InvitedCount = 0,
            CreatedAt = DateTime.UtcNow
        };

        await referralRepository.AddLinkAsync(newLink, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var username = await botInfoProvider.GetBotUsernameAsync(cancellationToken);
        var url = $"https://t.me/{username}?start={newLink.Code}";

        return Result<ReferralLinkDto>.Success(new ReferralLinkDto(
            newLink.Id,
            newLink.Code,
            url,
            newLink.InvitedCount,
            newLink.CreatedAt
        ));
    }

    public async Task<Result<ReferralProcessedDto?>> ProcessReferralJoinAsync(long newTelegramId, string referralCode, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(referralCode))
            return Result<ReferralProcessedDto?>.Success(null);

        var code = referralCode.Trim();

        var newUser = await userRepository.GetByTelegramIdAsync(newTelegramId, cancellationToken);
        if (newUser is null)
            return Result<ReferralProcessedDto?>.Success(null);

        // Пользователь уже был приглашен ранее
        if (newUser.ReferredByUserId.HasValue)
            return Result<ReferralProcessedDto?>.Success(null);

        var hasBeenReferred = await referralRepository.HasBeenReferredAsync(newUser.Id, cancellationToken);
        if (hasBeenReferred)
            return Result<ReferralProcessedDto?>.Success(null);

        var referralLink = await referralRepository.GetByCodeAsync(code, cancellationToken);
        if (referralLink is null)
            return Result<ReferralProcessedDto?>.Success(null);

        // Защита от перехода по собственной ссылке
        if (referralLink.UserId == newUser.Id)
            return Result<ReferralProcessedDto?>.Success(null);

        var referrerUser = await userRepository.GetByIdAsync(referralLink.UserId, cancellationToken);
        if (referrerUser is null)
            return Result<ReferralProcessedDto?>.Success(null);

        // 1. Фиксируем переход
        referralLink.InvitedCount += 1;
        referralLink.UpdatedAt = DateTime.UtcNow;
        referralRepository.UpdateLink(referralLink);

        newUser.ReferredByUserId = referralLink.UserId;
        newUser.UpdatedAt = DateTime.UtcNow;
        userRepository.Update(newUser);

        var record = new ReferralRecord
        {
            Id = Guid.NewGuid(),
            ReferralLinkId = referralLink.Id,
            ReferrerUserId = referralLink.UserId,
            ReferredUserId = newUser.Id,
            CreatedAt = DateTime.UtcNow
        };
        await referralRepository.AddRecordAsync(record, cancellationToken);

        // 2. Начисляем +3 дня к топу поиска с накопительным эффектом
        var referrerProfile = await userProfileRepository.GetByUserIdAsync(referralLink.UserId, cancellationToken);
        var now = DateTime.UtcNow;
        var totalBoostDays = 3;

        if (referrerProfile is not null)
        {
            if (referrerProfile.TopBoostUntil == null || referrerProfile.TopBoostUntil.Value <= now)
            {
                referrerProfile.TopBoostUntil = now.AddDays(3);
            }
            else
            {
                referrerProfile.TopBoostUntil = referrerProfile.TopBoostUntil.Value.AddDays(3);
            }

            referrerProfile.UpdatedAt = now;
            userProfileRepository.Update(referrerProfile);

            var remaining = referrerProfile.TopBoostUntil.Value - now;
            totalBoostDays = Math.Max(1, (int)Math.Ceiling(remaining.TotalDays));
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<ReferralProcessedDto?>.Success(new ReferralProcessedDto(
            referrerUser.TelegramId,
            referrerUser.Language,
            totalBoostDays
        ));
    }

    private static string GenerateRandomCode(int length)
    {
        Span<char> result = stackalloc char[length];
        Span<byte> randomBytes = stackalloc byte[length];
        RandomNumberGenerator.Fill(randomBytes);

        for (int i = 0; i < length; i++)
        {
            result[i] = CodeChars[randomBytes[i] % CodeChars.Length];
        }

        return new string(result);
    }
}
