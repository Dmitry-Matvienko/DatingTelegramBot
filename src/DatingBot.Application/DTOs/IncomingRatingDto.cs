namespace DatingBot.Application.DTOs;

public record IncomingRatingDto(
    Guid RatingId,
    UserProfileDto RaterProfile,
    int ScoreReceived,
    DateTime CreatedAt
);
