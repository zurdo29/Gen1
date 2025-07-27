namespace ProceduralMiniGameGenerator.WebAPI.Models
{
    /// <summary>
    /// Generic result wrapper for operations that can succeed or fail
    /// </summary>
    public class Result<T>
    {
        public bool IsSuccess { get; private set; }
        public T? Value { get; private set; }
        public string? ErrorMessage { get; private set; }
        public Exception? Exception { get; private set; }

        private Result(bool isSuccess, T? value, string? errorMessage, Exception? exception)
        {
            IsSuccess = isSuccess;
            Value = value;
            ErrorMessage = errorMessage;
            Exception = exception;
        }

        public static Result<T> Success(T value) => new(true, value, null, null);
        public static Result<T> Failure(string errorMessage) => new(false, default, errorMessage, null);
        public static Result<T> Failure(Exception exception) => new(false, default, exception.Message, exception);
    }

    /// <summary>
    /// Result wrapper for operations without return values
    /// </summary>
    public class Result
    {
        public bool IsSuccess { get; private set; }
        public string? ErrorMessage { get; private set; }
        public Exception? Exception { get; private set; }

        private Result(bool isSuccess, string? errorMessage, Exception? exception)
        {
            IsSuccess = isSuccess;
            ErrorMessage = errorMessage;
            Exception = exception;
        }

        public static Result Success() => new(true, null, null);
        public static Result Failure(string errorMessage) => new(false, errorMessage, null);
        public static Result Failure(Exception exception) => new(false, exception.Message, exception);
    }
}