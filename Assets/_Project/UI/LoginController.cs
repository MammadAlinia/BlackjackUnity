using System.Threading;
using Blackjack._Project.Scripts.Services.Auth;
using Blackjack.Services;
using Cysharp.Threading.Tasks;
using Reflex.Attributes;

namespace Blackjack.UI
{
    public sealed class LoginController : ScreenController
    {
        readonly IAuthenticationService _auth;

        public LoginController(UiContext context, IAuthenticationService auth) : base(context, "loginScreen")
        {
            this._auth = auth;
            Click("login", LoginAsync);
            Click("register", RegisterAsync);
        }

        UniTask<Result> LoginAsync(CancellationToken token)
        {
            var credentials = new EmailPassword(Find<UnityEngine.UIElements.TextField>("email").value,
                Find<UnityEngine.UIElements.TextField>("password").value);
            return _auth.LoginAsync(credentials, token);
        }

        UniTask<Result> RegisterAsync(CancellationToken token)
        {
            var credentials = new EmailPassword(Find<UnityEngine.UIElements.TextField>("email").value,
                Find<UnityEngine.UIElements.TextField>("password").value);
            return _auth.RegisterAsync(credentials, token);
        }
    }
}