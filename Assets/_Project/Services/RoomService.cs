using System;
using System.Threading;
using System.Threading.Tasks;
using CardGame.Client;
using CardGame.Contracts;

namespace Blackjack.Services
{
    public interface IRoomService
    {
        RoomDetails Details { get; }
        string UserId { get; }
        string RoomId { get; }
        RoomConnectionState State { get; }
        event Action Changed;
        Task<IResult<string[]>> ListAsync(CancellationToken token = default);
        Task<IResult<RoomDetails>> DetailsAsync(string id, CancellationToken token = default);
        Task<IResult<RoomDetails>> CreateAsync(string name, bool chat, int capacity, CancellationToken token = default);
        Task<IResult> JoinAsync(string id, CancellationToken token = default);
        Task<IResult> LeaveAsync(CancellationToken token = default);
        Task<IResult> CloseAsync(CancellationToken token = default);
    }
    public interface IRoomConnectionSource { RoomConnection Current { get; } event Action ConnectionChanged; }
    public sealed class RoomService : IRoomService, IRoomConnectionSource, IDisposable
    {
        readonly ISessionSource sessions;
        readonly IAuthenticationService auth;
        readonly IUnityThread thread;
        readonly SemaphoreSlim operations = new(1, 1);
        Action<RoomConnectionState> stateChanged;
        public RoomConnection Current { get; private set; }
        public RoomDetails Details => Current?.Details;
        public string UserId => Current?.UserId;
        public string RoomId => Current?.RoomId;
        public RoomConnectionState State => Current?.State ?? RoomConnectionState.Left;
        public event Action Changed;
        public event Action ConnectionChanged;
        public RoomService(ISessionSource sessions, IAuthenticationService auth, IUnityThread thread)
        { this.sessions = sessions; this.auth = auth; this.thread = thread; auth.Changed += AuthenticationChanged; }
        void AuthenticationChanged() { if (!auth.IsAuthenticated) Bind(null); }
        RoomClient Client => sessions.Session?.State == SessionState.Active ? sessions.Session.Rooms : null;
        public Task<IResult<string[]>> ListAsync(CancellationToken token = default) => Client == null
            ? Task.FromResult(Result<string[]>.Fail("Unauthorized", "Sign in first.")) : ResultBoundary.ClientAsync(() => Client.ListAsync(token));
        public Task<IResult<RoomDetails>> DetailsAsync(string id, CancellationToken token = default) => Client == null
            ? Task.FromResult(Result<RoomDetails>.Fail("Unauthorized", "Sign in first.")) : ResultBoundary.ClientAsync(() => Client.GetDetailsAsync(id, token));
        public Task<IResult<RoomDetails>> CreateAsync(string name, bool chat, int capacity, CancellationToken token = default) => Client == null
            ? Task.FromResult(Result<RoomDetails>.Fail("Unauthorized", "Sign in first.")) : ResultBoundary.ClientAsync(() => Client.CreateAsync(name, "blackjack", chat, capacity, token));
        public Task<IResult> JoinAsync(string id, CancellationToken token = default) => SerialAsync(async () =>
        {
            var client = Client;
            if (client == null) return Result.Fail("Unauthorized", "Sign in first.");
            if (Current?.IsMember == true && Current.RoomId != id)
            { var leave = await ResultBoundary.ClientAsync(() => Current.LeaveAsync(token)); if (!leave.Succeed) return leave; }
            var result = await ResultBoundary.ClientAsync(() => client.JoinAsync(id, token));
            if (result.Succeed && ReferenceEquals(Client, client)) Bind(result.Value);
            return result;
        }, token);
        public Task<IResult> LeaveAsync(CancellationToken token = default) => SerialAsync(async () =>
        {
            var room = Current;
            if (room == null) return Result.Ok();
            var result = await ResultBoundary.ClientAsync(() => room.LeaveAsync(token));
            if (result.Succeed) { await room.DisposeAsync(); Bind(null); }
            return result;
        }, token);
        public Task<IResult> CloseAsync(CancellationToken token = default) => Client == null || Current == null
            ? Task.FromResult(Result.Fail("NotMember", "Join a room first.")) : ResultBoundary.ClientAsync(() => Client.CloseAsync(Current.RoomId, token));
        Task<IResult> SerialAsync(Func<Task<IResult>> operation, CancellationToken token) => ResultBoundary.RunAsync(async () =>
        { await operations.WaitAsync(token); try { return await operation(); } finally { operations.Release(); } });
        void Bind(RoomConnection room)
        {
            if (ReferenceEquals(Current, room)) return;
            if (Current != null) Current.StateChanged -= stateChanged;
            Current = room;
            if (room != null)
            {
                stateChanged = _ => thread.Post(() => { if (ReferenceEquals(Current, room)) Changed?.Invoke(); });
                room.StateChanged += stateChanged;
            }
            thread.Post(() => { ConnectionChanged?.Invoke(); Changed?.Invoke(); });
        }
        public void Dispose() { auth.Changed -= AuthenticationChanged; Bind(null); }
    }
}
