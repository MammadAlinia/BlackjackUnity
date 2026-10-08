using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Triggers;
using UnityEngine;

namespace Blackjack.Services
{

    public interface IRoomService
    {
        UniTask<Result<string[]>> ListAsync(CancellationToken token = default);
        UniTask<Result> CreateAsync(string name, bool chat, int capacity, CancellationToken token = default);
        UniTask<Result> JoinAsync(string name, CancellationToken token = default);
        UniTask<Result> LeaveAsync(CancellationToken token = default);
        UniTask<Result> CloseAsync(CancellationToken token = default);
    }

    public interface IGameSessionService
    {
        UniTask<Result> ReadyAsync(bool ready, CancellationToken token = default);
        UniTask<Result> ActionAsync(string action, CancellationToken token = default);
        UniTask<Result> RefreshAsync(CancellationToken token = default);
    }

    public interface IChatService
    {
        bool IsAvailable { get; }
        UniTask<Result> SendAsync(string message, CancellationToken token = default);
        UniTask<Result> ReceiveAsync(string message, CancellationToken token = default);
    }

    public sealed class OfflineRoomService : IRoomService
    {
        public UniTask<Result<string[]>> ListAsync(CancellationToken token = default) =>
            UniTask.FromResult(Result.Ok(Array.Empty<string>())); // Mock rooms are always empty.

        public UniTask<Result> CreateAsync(string name, bool chat, int capacity, CancellationToken token = default) =>
            UniTask.FromResult(Result.Ok()); // Mock success lets the UI open its inert game screen.

        public UniTask<Result> JoinAsync(string name, CancellationToken token = default) =>
            UniTask.FromResult(Result.Ok()); // Mock success has no membership or game-state effect.

        public UniTask<Result> LeaveAsync(CancellationToken token = default) =>
            UniTask.FromResult(Result.Ok());

        public UniTask<Result> CloseAsync(CancellationToken token = default) =>
            UniTask.FromResult(Result.Ok());
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

    public sealed class OfflineChatService : IChatService
    {
        public bool IsAvailable => true;

        public UniTask<Result> SendAsync(string message, CancellationToken token = default) =>
            UniTask.FromResult(Result.Ok());

        public UniTask<Result> ReceiveAsync(string message, CancellationToken token = default)
            => UniTask.FromResult(Result.Ok());
    }
}