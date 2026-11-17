namespace ElMomenNursery.Domain.Abstractions;

public class Result
{
    protected internal Result(bool isSuccess, Error error, string? message = null)
    {
        ArgumentNullException.ThrowIfNull(error);

        if (isSuccess && error != Error.None || !isSuccess && error == Error.None)
        {
            throw new ArgumentException("حالة النتيجة لا تتوافق مع الخطأ المرفق.", nameof(error));
        }

        IsSuccess = isSuccess;
        Error = error;
        Message = message;
    }

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public Error Error { get; }
    public string? Message { get; }

    public static Result Success(string? message = null) => new(true, Error.None, message);
    public static Result Failure(Error error) => new(false, error);

    public static Result<TValue> Success<TValue>(TValue value, string? message = null) => new(value, true, Error.None, message);
    public static Result<TValue> Failure<TValue>(Error error) => new(default, false, error);

    public static implicit operator Result(Error error) => Failure(error);
}

public sealed class Result<TValue> : Result
{
    private readonly TValue? _value;

    internal Result(TValue? value, bool isSuccess, Error error, string? message = null)
        : base(isSuccess, error, message)
    {
        _value = value;
    }

    public TValue Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("لا يمكن قراءة قيمة نتيجة غير ناجحة.");

    public static implicit operator Result<TValue>(TValue value) => Success(value);
    public static implicit operator Result<TValue>(Error error) => Failure<TValue>(error);
}
