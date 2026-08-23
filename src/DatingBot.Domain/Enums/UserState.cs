namespace DatingBot.Domain.Enums;

public enum UserState
{
    None = 0,
    Registration_SelectingGender = 1,
    Registration_SelectingTargetGender = 2,
    Registration_WaitingForName = 3,
    Registration_WaitingForAge = 4,
    Registration_WaitingForCity = 5,
    Registration_WaitingForHeight = 6,
    Registration_WaitingForPhoto = 7,
    Registration_SelectingInterests = 8,
    Registration_SelectingTarget = 9,
    Registration_WaitingForAiBio = 10,
    Registration_SelectingLanguage = 11,
    Active = 100,
    Paused = 101,
    
    // Режимы редактирования профиля
    Editing_Name = 201,
    Editing_Age = 202,
    Editing_City = 203,
    Editing_Height = 204,
    Editing_Photo = 205,
    Editing_Gender = 206,
    Editing_TargetGender = 207,
    Editing_DatingTarget = 208,
    Editing_Interests = 209,
    Editing_AiBio = 210,
    Editing_SearchAgeCategories = 211,
    Editing_SearchMinAge = 212,
    Editing_SearchMaxAge = 213,
    Editing_Language = 214,
    Editing_Greeting = 215,

    // Режим просмотра и поиска анкет
    Searching = 300,
    Reporting_WaitingForDetails = 301,

    // Состояния администратора
    Admin_Panel = 400,
    Admin_Stats_WaitingForCity = 401,
    Admin_Broadcasting_WaitingForContent = 402,
    Admin_Broadcasting_WaitingForButton = 403,
    Admin_Broadcasting_WaitingForCity = 404,
    Admin_BrowsingProfiles = 405,
    Admin_Revenue = 406,

    Banned = 999
}
