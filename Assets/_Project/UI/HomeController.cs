using System.Threading.Tasks;
using Blackjack.Services;
using UnityEngine;
using UnityEngine.UIElements;

namespace Blackjack.UI
{
    public sealed class HomeController : ScreenController
    {
        readonly IAuthenticationService auth;
        public HomeController(UiContext context, IAuthenticationService auth) : base(context, "homeScreen")
        {
            this.auth = auth;
            Click("browseRooms", _ => { context.Go("rooms"); return Task.FromResult(Result.Ok()); });
            Click("logout", token => auth.LogoutAsync(token));
            Click("quit", _ => { Application.Quit(); return Task.FromResult(Result.Ok()); });
        }
        public override void Refresh() => Find<Label>("account").text = auth.AccountName;
    }
}
