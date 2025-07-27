namespace ProceduralMiniGameGenerator.WebAPI.Models
{
    /// <summary>
    /// Generic result wrapper for operations that can succeed or fail
    /// </summary>
    public class Result<T>
    {
        public bool IsSuccess { get; private set; }
        public bool IsFailure => !IsSuccess;
        public T? Value { get; private set; }
        public string? ErrorMessage { get; private set; }
        public string? Error => ErrorMessage;
        public Exception? Exception { get; private set; }
        public List<string> Errors { get; private set; } = new();

        private Result(bool isSuccess, T? value, string? errorMessage, Exception? exception)
        {
            IsSuccess = isSuccess;
            Value = value;
            ErrorMessage = errorMessage;
            Exception = exception;
            
            if (!string.IsNullOrEmpty(errorMessage))
            {
                Errors.Add(errorMessage);
            }
        }

        public static Result<T> Success(T value) => new(true, value, null, null);
        public static Result<T> Failure(string errorMessage) => new(false, default, errorMessage, null);
        public static Result<T> Failure(Exception exception) => new(false, default, exception.Message, exception);
        public static Result<T> Failure(List<string> errors) 
        {
            var result = new Result<T>(false, default, errors.FirstOrDefault(), null);
            result.Errors.Clear();
            result.Errors.AddRange(errors);
            return result;
        }

        /// <summary>
        /// Pattern matching method for functional-style result handling
        /// </summary>
        public TResult Match<TResult>(Func<T, TResult> onSuccess, Func<string, TResult> onFailure)
        {
            return IsSuccess ? onSuccess(Value!) : onFailure(ErrorMessage ?? "Unknown error");
        }

        /// <summary>
        /// Pattern matching method with exception handling
        /// </summary>
        public TResult Match<TResult>(Func<T, TResult> onSuccess, Func<string, Exception?, TResult> onFailure)
        {
            return IsSuccess ? onSuccess(Value!) : onFailure(ErrorMessage ?? "Unknown error", Exception);
        }
    }

    /// <summary>
    /// Result wrapper for operations without return values
    /// </summary>
    public class Result
    {
        public bool IsSuccess { get; private set; }
        public bool IsFailure => !IsSuccess;
        public string? ErrorMessage { get; private set; }
        public string? Error => ErrorMessage;
        public Exception? Exception { get; private set; }
        public List<string> Errors { get; private set; } = new();

        private Result(bool isSuccess, string? errorMessage, Exception? exception)
        {
            IsSuccess = isSuccess;
            ErrorMessage = errorMessage;
            Exception = exception;
            
            if (!string.IsNullOrEmpty(errorMessage))
            {
                Errors.Add(errorMessage);
            }
        }

        public static Result Success() => new(true, null, null);
        public static Result Failure(string errorMessage) => new(false, errorMessage, null);
        public static Result Failure(Exception exception) => new(false, exception.Message, exception);
        public static Result Failure(List<string> errors) 
        {
            var result = new Result(false, errors.FirstOrDefault(), null);
            result.Errors.Clear();
            result.Errors.AddRange(errors);
            return result;
        }

        /// <summary>
        /// Pattern matching method for functional-style result handling
        /// </summary>
        public TResult Match<TResult>(Func<TResult> onSuccess, Func<string, TResult> onFailure)
        {
            return IsSuccess ? onSuccess() : onFailure(ErrorMessage ?? "Unknown error");
        }

        /// <summary>
        /// Pattern matching method with exception handling
        /// </summary>
        public TResult Match<TResult>(Func<TResult> onSuccess, Func<string, Exception?, TResult> onFailure)
        {
            return IsSuccess ? onSuccess() : onFailure(ErrorMessage ?? "Unknown error", Exception);
        }
    }
}