using System;
using Cysharp.Threading.Tasks;

namespace Blackjack.Services
{
    public static class ResultBoundary
    {
        public static async UniTask<Result> RunAsync(Func<UniTask<Result>> operation)
        {
            try { return await operation(); }
            catch (Exception exception) { return Result.Fail("UnexpectedFailure", exception.Message); }
        }
    }
}
