namespace Blackjack.Services
{
    public interface IResult
    {
        bool Succeed { get; }
        string ErrorCode { get; }
        string ErrorMessage { get; }
    }
    public interface IResult<out T> : IResult { T Value { get; } }

    public sealed class Result<T> : IResult<T>
    {
        public bool Succeed { get; }
        public string ErrorCode { get; }
        public string ErrorMessage { get; }
        public T Value { get; }
        Result(bool success, T value, string code, string message)
        { Succeed = success; Value = value; ErrorCode = code; ErrorMessage = message; }
        public static IResult<T> Ok(T value) => new Result<T>(true, value, "None", "");
        public static IResult<T> Fail(string code, string message) => new Result<T>(false, default, code, message);
    }
    public static class Result
    {
        public static IResult Ok() => Result<bool>.Ok(true);
        public static IResult Fail(string code, string message) => Result<bool>.Fail(code, message);
        public static IResult<T> Failure<T>(IResult error) => Result<T>.Fail(error.ErrorCode, error.ErrorMessage);
        public static IResult From(CardGame.Contracts.Result value) => value.Succeed ? Ok() : Fail(value.ErrorCode.ToString(), value.ErrorMessage);
        public static IResult<T> From<T>(CardGame.Contracts.Result<T> value) => value.Succeed
            ? Result<T>.Ok(value.Value) : Result<T>.Fail(value.ErrorCode.ToString(), value.ErrorMessage);
    }
}
