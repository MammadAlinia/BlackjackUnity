using System.Threading;
using Blackjack._Project.Scripts.Services.Auth;
using Blackjack.Services;
using Cysharp.Threading.Tasks;

namespace Blackjack.UI
{
    public sealed class LoginController : ScreenController
    {
        readonly IAuthenticationService auth;

        public LoginController(UiContext context, IAuthenticationService auth) : base(context, "loginScreen")
        {
            this.auth = auth;
            Click("login", LoginAsync);
            Click("register", RegisterAsync);
        }

        UniTask<Result> LoginAsync(CancellationToken token)
        {
            var credentials = new EmailPassword(Find<UnityEngine.UIElements.TextField>("email").value,
                Find<UnityEngine.UIElements.TextField>("password").value);
            return auth.LoginAsync(credentials, token);
        }

        UniTask<Result> RegisterAsync(CancellationToken token)
        {
            var credentials = new EmailPassword(Find<UnityEngine.UIElements.TextField>("email").value,
                Find<UnityEngine.UIElements.TextField>("password").value);
            return auth.RegisterAsync(credentials, token);
        }
    }
}