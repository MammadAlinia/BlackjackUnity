using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Blackjack._Project.Scripts.Services.Auth;
using Blackjack.Services.Transport;
using Cysharp.Threading.Tasks;
using Nakama;
using Reflex.Attributes;

namespace Blackjack.Services.Room
{
    public interface IRoomService
    {
        UniTask<Result<string[]>> ListAsync(CancellationToken token = default);
        UniTask<Result<string>> CreateAsync(string name, bool chat, int capacity, CancellationToken token = default);
        UniTask<Result> JoinAsync(string name, CancellationToken token = default);
        UniTask<Result> LeaveAsync(CancellationToken token = default);
        UniTask<Result> CloseAsync(CancellationToken token = default);
    }

    public sealed class NakamaRoomService : IRoomService
    {
        [Inject] NakamaTransport _transport;
        [Inject] AuthNakama _auth;
        ISocket _socket;

        IMatch _joinedMatch;

        ISocket Channel =>
            _socket ??= Socket.From(_transport.Client);

        public IMatch GetCurrentMatch() => _joinedMatch;

        public event Action<IMatch> joinedMatch;
        public event Action leftMatch;

        public async Task<Result<ISocket>> TryConnectSocket()
        {
            if (Channel.IsConnected)
                return Result.Ok(Channel);

            if (Channel.IsConnecting)
            {
                // idk what to do
            }

            try
            {
                await Channel.ConnectAsync(_auth.Session);
            }
            catch (Exception e)
            {
                return Result<ISocket>.Fail("0", e.Message);
            }

            return Result.Ok(Channel);
        }

        public async UniTask<Result<string[]>> ListAsync(CancellationToken token = default)
        {
            IApiMatchList matchList = null;

            try
            {
                matchList =
                    await _transport.Client.ListMatchesAsync(_auth.Session,
                        0, 10, 10,
                        false, null,
                        null,
                        canceller: token);
            }
            catch (Exception e)
            {
                return Result<string[]>.Fail("0", e.Message);
            }


            return Result.Ok(matchList.Matches.ToList()
                .Select(x => x.MatchId)
                .ToArray());
        }

        public async UniTask<Result<string>> CreateAsync(string name, bool chat, int capacity,
            CancellationToken token = default)
        {
            IMatch match = null;

            var connected = await TryConnectSocket();

            if (!connected.Succeed)
            {
                return Result.Fail<string>(connected.ErrorCode, connected.ErrorMessage);
            }

            var channel = connected.Value;

            try
            {
                match = await channel.CreateMatchAsync();
            }
            catch (Exception e)
            {
                return Result.Fail<string>("0", e.Message);
            }


            return Result.Ok(match.Id);
        }


        public async UniTask<Result> JoinAsync(string id, CancellationToken token = default)
        {
            var connected = await TryConnectSocket();
            if (!connected.Succeed)
                return Result.Fail(connected.ErrorCode, connected.ErrorMessage);

            var channel = connected.Value;

            IMatch join = null;

            try
            {
                join = await channel.JoinMatchAsync(id);
            }
            catch (Exception e)
            {
                return Result.Fail("0", e.Message);
            }

            _joinedMatch = join;
            joinedMatch?.Invoke(join);
            return Result.Ok();
        }

        public async UniTask<Result> LeaveAsync(CancellationToken token = default)
        {
            var connected = await TryConnectSocket();
            if (!connected.Succeed)
                return Result.Fail(connected.ErrorCode, connected.ErrorMessage);

            var channel = connected.Value;

            try
            {
                await channel.LeaveMatchAsync(_joinedMatch.Id);
            }
            catch (Exception e)
            {
                return Result.Fail("0", e.Message);
            }

            leftMatch?.Invoke();

            return Result.Ok();
        }

        public UniTask<Result> CloseAsync(CancellationToken token = default)
        {
            return LeaveAsync(token);
        }
    }

    public sealed class OfflineRoomService : IRoomService
    {
        public UniTask<Result<string[]>> ListAsync(CancellationToken token = default) =>
            UniTask.FromResult(Result.Ok(Array.Empty<string>())); // Mock rooms are always empty.

        public UniTask<Result<string>> CreateAsync(string name, bool chat, int capacity,
            CancellationToken token = default) =>
            UniTask.FromResult(Result.Ok("")); // Mock success lets the UI open its inert game screen.

        public UniTask<Result> JoinAsync(string name, CancellationToken token = default) =>
            UniTask.FromResult(Result.Ok()); // Mock success has no membership or game-state effect.

        public UniTask<Result> LeaveAsync(CancellationToken token = default) =>
            UniTask.FromResult(Result.Ok());

        public UniTask<Result> CloseAsync(CancellationToken token = default) =>
            UniTask.FromResult(Result.Ok());
    }
}