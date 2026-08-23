namespace DatingBot.Application.Common;

public class Result
{
    public bool IsSuccess { get; }
    public string? ErrorMessage { get; }
    public bool IsFailure => !IsSuccess;

    protected Result(bool isSuccess, string? errorMessage)
    {
        IsSuccess = isSuccess;
        ErrorMessage = errorMessage;
    }

    public static Result Success() => new(true, null);
    public static Result Failure(string errorMessage) => new(false, errorMessage);
}

public class Result<T> : Result
{
    public T? Value { get; }

    protected Result(bool isSuccess, T? value, string? errorMessage) : base(isSuccess, errorMessage)
    {
        Value = value;
    }

    public static Result<T> Success(T value) => new(true, value, null);
    public static new Result<T> Failure(string errorMessage) => new(false, default, errorMessage);
}
