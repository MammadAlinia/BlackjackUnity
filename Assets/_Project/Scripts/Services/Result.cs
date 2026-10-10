namespace Blackjack.Services
{
    public readonly struct Result
    {
        public bool Succeed { get; }
        public string ErrorCode { get; }
        public string ErrorMessage { get; }

        Result(bool succeed, string errorCode, string errorMessage)
        {
            Succeed = succeed;
            ErrorCode = errorCode;
            ErrorMessage = errorMessage;
        }

        public static Result Ok() => new(true, "None", string.Empty);
        public static Result Fail(string code, string message) => new(false, code, message);
        public static Result<T> Fail<T>( string code, string message) => Result<T>.Fail(code, message);
        public static Result<T> Ok<T>(T value) => Result<T>.Ok(value);
    }

    public readonly struct Result<T>
    {
        public bool Succeed { get; }
        public string ErrorCode { get; }
        public string ErrorMessage { get; }
        public T Value { get; }

        Result(bool succeed, T value, string errorCode, string errorMessage)
        {
            Succeed = succeed;
            Value = value;
            ErrorCode = errorCode;
            ErrorMessage = errorMessage;
        }

        public static Result<T> Ok(T value) => new(true, value, "None", string.Empty);
        public static Result<T> Fail(string code, string message) => new(false, default, code, message);
    }
}