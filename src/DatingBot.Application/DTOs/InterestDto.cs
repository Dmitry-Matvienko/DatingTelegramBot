using DatingBot.Domain.Enums;

namespace DatingBot.Application.DTOs;

public record InterestDto(
    int Id,
    InterestType Code,
    string Title,
    string Icon,
    bool IsSelected
);
