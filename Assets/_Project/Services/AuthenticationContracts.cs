using System;
using System.Threading;
using System.Threading.Tasks;
using CardGame.Client;

namespace Blackjack.Services
{
    public interface IAuthenticationService
    {
        bool IsAuthenticated { get; }
        string AccountName { get; }
        event Action Changed;
        event Action<IResult> PersistenceFailed;
        Task<IResult> RestoreAsync(string server, CancellationToken token = default);
        Task<IResult> LoginAsync(string server, string email, string password, string code = null, CancellationToken token = default);
        Task<IResult> RegisterAsync(string server, string email, string password, CancellationToken token = default);
        Task<IResult> GoogleAsync(string server, CancellationToken token = default);
        Task<IResult> LogoutAsync(CancellationToken token = default);
    }
    public interface ISessionSource { ClientSession Session { get; } }
    public interface IGoogleLogin { Task<IResult<ClientSession>> LoginAsync(CardGameClient client, CancellationToken token); }
    [Serializable]
    public sealed class SavedSession
    {
        public string accessToken;
        public string refreshToken;
        public long expiresAt;
        public string accountName;
    }
}
