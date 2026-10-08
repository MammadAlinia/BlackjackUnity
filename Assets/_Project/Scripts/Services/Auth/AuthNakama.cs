using System;
using System.Threading;
using Blackjack.Services;
using Blackjack.Services.Transport;
using Cysharp.Threading.Tasks;
using Nakama;
using Reflex.Attributes;

namespace Blackjack._Project.Scripts.Services.Auth
{
    public sealed class AuthNakama : IAuthenticationService
    {
        [Inject] NetworkManager<Nakama.Client> _networkManager;
        public string AccountName { get; set; }
        public bool IsAuthenticated { get; set; }
        public event Action Changed;
        public ISession Session { get; set; }

        public UniTask<Result> LoginAsync(IAuthCredentials credentials, CancellationToken token = default)
        {
            return UniTask.FromResult(Result.Ok());
        }

        public UniTask<Result> RegisterAsync(IAuthCredentials credentials, CancellationToken token = default)
        {
            return UniTask.FromResult(Result.Ok());
        }

        public UniTask<Result> LogoutAsync(CancellationToken token = default)
        {
            return UniTask.FromResult(Result.Ok());
        }

        public async UniTask<Result> LoginWithEmailAsync(EmailPassword emailPassword, CancellationToken token = default)
        {
            Session = await _networkManager.Client.AuthenticateEmailAsync(emailPassword.Email,
                emailPassword.Password, null,
                false, canceller: token);
            if (!Session.Created || Session == null)
                return Result.Fail("401", "failed to login");

            return Result.Ok();
        }
    }
}