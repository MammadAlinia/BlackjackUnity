using System;
using System.Threading.Tasks;
using CardGame.Client;

namespace Blackjack.Services
{
    public static class ResultBoundary
    {
        // Diagnostics contain only exception type and stack, never response bodies or credentials.
        public static event Action<string> Diagnostic;
        static void Report(Exception exception)
        { if (Code(exception) == "UnexpectedFailure") { try { Diagnostic?.Invoke(exception.GetType().FullName
            + (exception is TypeLoadException || exception is System.IO.FileNotFoundException ? ": " + exception.Message : "")
            + "\n" + exception.StackTrace); } catch { } } }
        public static async Task<IResult<T>> RunAsync<T>(Func<Task<IResult<T>>> operation)
        {
            try { return await operation(); }
            catch (Exception exception) { Report(exception); return Result<T>.Fail(Code(exception), Message(exception)); }
        }
        public static async Task<IResult> RunAsync(Func<Task<IResult>> operation)
        {
            try { return await operation(); }
            catch (Exception exception) { Report(exception); return Result.Fail(Code(exception), Message(exception)); }
        }
        public static Task<IResult<T>> ClientAsync<T>(Func<Task<CardGame.Contracts.Result<T>>> operation) =>
            RunAsync(async () => Result.From(await operation()));
        public static Task<IResult> ClientAsync(Func<Task<CardGame.Contracts.Result>> operation) =>
            RunAsync(async () => Result.From(await operation()));
        static string Code(Exception exception) => exception switch
        {
            OperationCanceledException => "Cancelled",
            ClientTimeoutException => "Timeout",
            ClientTransportException => "Network",
            ClientProtocolException => "Protocol",
            ObjectDisposedException => "Disposed",
            ArgumentException => "InvalidRequest",
            _ => "UnexpectedFailure"
        };
        static string Message(Exception exception) => exception switch
        {
            OperationCanceledException => "Operation cancelled.",
            ClientTimeoutException => "The server did not respond in time. Refresh state before retrying a command.",
            ClientTransportException => "Cannot reach the server. Check your connection and try again.",
            ClientProtocolException => "The server returned an invalid response.",
            ObjectDisposedException => "This session has ended.",
            ArgumentException => "Check the supplied values and server address.",
            _ => "The operation failed unexpectedly."
        };
    }
}
