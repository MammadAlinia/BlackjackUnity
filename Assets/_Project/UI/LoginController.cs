using System.Threading;
using System.Threading.Tasks;
using Blackjack.Services;
using UnityEngine.UIElements;

namespace Blackjack.UI
{
    public sealed class LoginController : ScreenController
    {
        readonly IAuthenticationService auth;
        readonly INetworkService network;
        readonly IStorage storage;
        public LoginController(UiContext context, IAuthenticationService auth, INetworkService network, IStorage storage) : base(context, "loginScreen")
        {
            this.auth = auth; this.network = network; this.storage = storage;
            Find<TextField>("server").value = network.ServerUrl;
            Click("login", token => SignInAsync(false, token));
            Click("register", RegisterAsync);
            Click("google", token => SignInAsync(true, token));
            Click("restore", token => auth.RestoreAsync(Find<TextField>("server").value, token));
        }
        async Task<IResult> RegisterAsync(CancellationToken token)
        {
            var result = await auth.RegisterAsync(Find<TextField>("server").value, Find<TextField>("email").value.Trim(), Find<TextField>("password").value, token);
            if (result.Succeed) Context.Status("Account created. Sign in with your password.");
            return result;
        }
        async Task<IResult> SignInAsync(bool google, CancellationToken token)
        {
            Context.Status(google ? "Complete sign-in in your browser…" : "Signing in…");
            var result = google ? await auth.GoogleAsync(Find<TextField>("server").value, token)
                : await auth.LoginAsync(Find<TextField>("server").value, Find<TextField>("email").value.Trim(), Find<TextField>("password").value,
                    string.IsNullOrWhiteSpace(Find<TextField>("twoFactor").value) ? null : Find<TextField>("twoFactor").value.Trim(), token);
            if (result.Succeed) Find<TextField>("password").value = "";
            if (result.Succeed) Context.Report(await storage.SetAsync("blackjack.server", network.ServerUrl));
            return result;
        }
        public async Task<IResult> RestoreAsync(CancellationToken token)
        {
            var saved = await storage.GetAsync("blackjack.server", token);
            if (!saved.Succeed) return saved;
            Find<TextField>("server").value = saved.Value ?? network.ServerUrl;
            return await auth.RestoreAsync(Find<TextField>("server").value, token);
        }
    }
}
