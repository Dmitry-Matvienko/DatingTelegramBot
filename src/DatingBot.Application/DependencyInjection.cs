using DatingBot.Application.Interfaces;
using DatingBot.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace DatingBot.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IRegistrationService, RegistrationService>();
        services.AddScoped<IProfileEditingService, ProfileEditingService>();
        services.AddScoped<IMatchmakingService, MatchmakingService>();
        services.AddScoped<ISearchService, SearchService>();
        services.AddScoped<IModerationService, ModerationService>();
        services.AddScoped<IAdminService, AdminService>();
        services.AddScoped<IInactivityReminderService, InactivityReminderService>();
        services.AddScoped<IReferralService, ReferralService>();
        services.AddSingleton<ILocalizationService, LocalizationService>();
        return services;
    }
}
