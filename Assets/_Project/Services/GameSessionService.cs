using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CardGame.Client;
using CardGame.Contracts;

namespace Blackjack.Services
{
    public interface IGameSessionService
    {
        GameStateView State { get; }
        bool CanAct(string action);
        event Action Changed;
        Task<IResult> ReadyAsync(bool ready, CancellationToken token = default);
        Task<IResult> ActionAsync(string action, CancellationToken token = default);
        Task<IResult> RefreshAsync(CancellationToken token = default);
    }
    public sealed class GameSessionService : RoomFeature, IGameSessionService
    {
        Action<GameStateView> received;
        Action<RoomConnectionState> connection;
        public GameStateView State => Room?.Gameplay.State;
        public event Action Changed;
        public GameSessionService(IRoomConnectionSource source, IUnityThread thread) : base(source, thread) => Initialize();
        public bool CanAct(string action) => Room?.Gameplay.CanAct == true && State?.AllowedActions.Contains(action) == true;
        protected override void Subscribe(RoomConnection room, bool add)
        {
            if (add)
            {
                received = _ => Notify(room, OnChanged);
                connection = _ => Notify(room, OnChanged);
                room.Gameplay.StateChanged += received; room.StateChanged += connection;
            }
            else { room.Gameplay.StateChanged -= received; room.StateChanged -= connection; }
        }
        protected override void OnChanged() => Changed?.Invoke();
        public Task<IResult> ReadyAsync(bool ready, CancellationToken token = default) => !CanAct(ready ? "Ready" : "Unready")
            ? Task.FromResult(Result.Fail("InvalidAction", "Wait for your current game state.")) : ResultBoundary.ClientAsync(() => Room.Gameplay.SetReadyAsync(ready, token));
        public Task<IResult> ActionAsync(string action, CancellationToken token = default) => !CanAct(action)
            ? Task.FromResult(Result.Fail("InvalidAction", "That action is not available.")) : ResultBoundary.ClientAsync(() => Room.Gameplay.SendActionAsync(action, token));
        public Task<IResult> RefreshAsync(CancellationToken token = default) => Room == null
            ? Task.FromResult(Result.Fail("NotMember", "Join a room first.")) : ResultBoundary.ClientAsync(() => Room.Gameplay.RefreshStateAsync(token));
    }
}
