// Improved Result class with consistent error handling
public class Result<T>
{
    public bool IsSuccess { get; private set; }
    public bool IsFailure => !IsSuccess;
    public T? Value { get; private set; }
    public IReadOnlyList<string> Errors { get; private set; }
    public string? ErrorMessage => Errors.FirstOrDefault();
    public string? Error => ErrorMessage; // Backward compatibility
    public Exception? Exception { get; private set; }

    private Result(bool isSuccess, T? value, IEnumerable<string> errors, Exception? exception)
    {
        IsSuccess = isSuccess;
        Value = value;
        Errors = errors.ToList().AsReadOnly();
        Exception = exception;
    }

    public static Result<T> Success(T value) => new(true, value, Array.Empty<string>(), null);
    public static Result<T> Failure(string errorMessage) => new(false, default, new[] { errorMessage }, null);
    public static Result<T> Failure(Exception exception) => new(false, default, new[] { exception.Message }, exception);
    public static Result<T> Failure(IEnumerable<string> errors) => new(false, default, errors, null);
}