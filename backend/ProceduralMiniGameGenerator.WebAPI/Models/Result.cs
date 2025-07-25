namespace ProceduralMiniGameGenerator.WebAPI.Models
{
    /// <summary>
    /// Represents the result of an operation that can succeed or fail
    /// </summary>
    /// <typeparam name="T">The type of the success value</typeparam>
    public class Result<T>
    {
        private readonly T? _value;
        private readonly string? _error;

        private Result(bool isSuccess, T? value, string? error)
        {
            IsSuccess = isSuccess;
            _value = value;
            _error = error;
        }

        /// <summary>
        /// Gets a value indicating whether the operation was successful
        /// </summary>
        public bool IsSuccess { get; }
        
        /// <summary>
        /// Gets a value indicating whether the operation failed
        /// </summary>
        public bool IsFailure => !IsSuccess;

        /// <summary>
        /// Gets the success value. Throws if the result represents a failure.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when accessing value of a failed result</exception>
        public T Value => IsSuccess ? _value! : throw new InvalidOperationException("Cannot access value of failed result");
        
        /// <summary>
        /// Gets the error message. Throws if the result represents a success.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when accessing error of a successful result</exception>
        public string Error => IsFailure ? _error! : throw new InvalidOperationException("Cannot access error of successful result");

        /// <summary>
        /// Creates a successful result with the specified value
        /// </summary>
        /// <param name="value">The success value</param>
        /// <returns>A successful result containing the value</returns>
        public static Result<T> Success(T value) => new(true, value, null);
        
        /// <summary>
        /// Creates a failed result with the specified error message
        /// </summary>
        /// <param name="error">The error message</param>
        /// <returns>A failed result containing the error message</returns>
        public static Result<T> Failure(string error) => new(false, default, 
            string.IsNullOrWhiteSpace(error) ? "An error occurred" : error);

        public static implicit operator Result<T>(T value) => Success(value);
        public static implicit operator Result<T>(string error) => Failure(error);

        public TResult Match<TResult>(Func<T, TResult> onSuccess, Func<string, TResult> onFailure)
        {
            return IsSuccess ? onSuccess(Value) : onFailure(Error);
        }

        public async Task<TResult> MatchAsync<TResult>(Func<T, Task<TResult>> onSuccess, Func<string, Task<TResult>> onFailure)
        {
            return IsSuccess ? await onSuccess(Value) : await onFailure(Error);
        }

        /// <summary>
        /// Maps the success value to a new type using the provided function
        /// </summary>
        /// <typeparam name="TNew">The new type to map to</typeparam>
        /// <param name="mapper">Function to transform the success value</param>
        /// <returns>A new Result with the mapped value or the original error</returns>
        public Result<TNew> Map<TNew>(Func<T, TNew> mapper)
        {
            return IsSuccess ? Result<TNew>.Success(mapper(Value)) : Result<TNew>.Failure(Error);
        }

        /// <summary>
        /// Binds the result to another operation that returns a Result
        /// </summary>
        /// <typeparam name="TNew">The type of the new result</typeparam>
        /// <param name="binder">Function that takes the success value and returns a new Result</param>
        /// <returns>The result of the binding operation or the original error</returns>
        public Result<TNew> Bind<TNew>(Func<T, Result<TNew>> binder)
        {
            return IsSuccess ? binder(Value) : Result<TNew>.Failure(Error);
        }

        /// <summary>
        /// Attempts to get the value safely without throwing exceptions
        /// </summary>
        /// <param name="value">The value if successful, default otherwise</param>
        /// <returns>True if successful, false otherwise</returns>
        public bool TryGetValue(out T? value)
        {
            value = IsSuccess ? _value : default;
            return IsSuccess;
        }

        /// <summary>
        /// Maps the success value to a new type using an async function
        /// </summary>
        /// <typeparam name="TNew">The new type to map to</typeparam>
        /// <param name="mapper">Async function to transform the success value</param>
        /// <returns>A new Result with the mapped value or the original error</returns>
        public async Task<Result<TNew>> MapAsync<TNew>(Func<T, Task<TNew>> mapper)
        {
            return IsSuccess ? Result<TNew>.Success(await mapper(Value)) : Result<TNew>.Failure(Error);
        }

        /// <summary>
        /// Binds the result to another async operation that returns a Result
        /// </summary>
        /// <typeparam name="TNew">The type of the new result</typeparam>
        /// <param name="binder">Async function that takes the success value and returns a new Result</param>
        /// <returns>The result of the binding operation or the original error</returns>
        public async Task<Result<TNew>> BindAsync<TNew>(Func<T, Task<Result<TNew>>> binder)
        {
            return IsSuccess ? await binder(Value) : Result<TNew>.Failure(Error);
        }

        /// <summary>
        /// Returns a string representation of the result for debugging purposes
        /// </summary>
        public override string ToString()
        {
            return IsSuccess ? $"Success: {_value}" : $"Failure: {_error}";
        }
    }

    /// <summary>
    /// Represents the result of an operation without a return value
    /// </summary>
    public class Result
    {
        private readonly string? _error;

        private Result(bool isSuccess, string? error)
        {
            IsSuccess = isSuccess;
            _error = error;
        }

        public bool IsSuccess { get; }
        public bool IsFailure => !IsSuccess;

        public string Error => IsFailure ? _error! : throw new InvalidOperationException("Cannot access error of successful result");

        public static Result Success() => new(true, null);
        public static Result Failure(string error) => new(false, error);

        public static implicit operator Result(string error) => Failure(error);

        public TResult Match<TResult>(Func<TResult> onSuccess, Func<string, TResult> onFailure)
        {
            return IsSuccess ? onSuccess() : onFailure(Error);
        }

        public async Task<TResult> MatchAsync<TResult>(Func<Task<TResult>> onSuccess, Func<string, Task<TResult>> onFailure)
        {
            return IsSuccess ? await onSuccess() : await onFailure(Error);
        }
    }

    /// <summary>
    /// Static helper methods for Result operations
    /// </summary>
    public static class ResultExtensions
    {
        /// <summary>
        /// Executes a function and wraps the result or exception in a Result
        /// </summary>
        /// <typeparam name="T">The return type</typeparam>
        /// <param name="func">The function to execute</param>
        /// <returns>A Result containing the function result or error</returns>
        public static Result<T> Try<T>(Func<T> func)
        {
            try
            {
                return Result<T>.Success(func());
            }
            catch (Exception ex)
            {
                return Result<T>.Failure(ex.Message);
            }
        }

        /// <summary>
        /// Executes an async function and wraps the result or exception in a Result
        /// </summary>
        /// <typeparam name="T">The return type</typeparam>
        /// <param name="func">The async function to execute</param>
        /// <returns>A Result containing the function result or error</returns>
        public static async Task<Result<T>> TryAsync<T>(Func<Task<T>> func)
        {
            try
            {
                var result = await func();
                return Result<T>.Success(result);
            }
            catch (Exception ex)
            {
                return Result<T>.Failure(ex.Message);
            }
        }
    }
}