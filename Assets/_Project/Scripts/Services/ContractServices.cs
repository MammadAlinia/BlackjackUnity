using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Triggers;
using UnityEngine;

namespace Blackjack.Services
{

   

    public interface IGameSessionService
    {
        UniTask<Result> ReadyAsync(bool ready, CancellationToken token = default);
        UniTask<Result> ActionAsync(string action, CancellationToken token = default);
        UniTask<Result> RefreshAsync(CancellationToken token = default);
    }




    public sealed class OfflineGameSessionService : IGameSessionService
    {
        public UniTask<Result> ReadyAsync(bool ready, CancellationToken token = default) =>
            UniTask.FromResult(Result.Ok());

        public UniTask<Result> ActionAsync(string action, CancellationToken token = default) =>
            UniTask.FromResult(Result.Ok());

        public UniTask<Result> RefreshAsync(CancellationToken token = default) =>
            UniTask.FromResult(Result.Ok());
    }
}