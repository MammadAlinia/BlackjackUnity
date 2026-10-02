#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.IO;
using System.Threading.Tasks;
using Blackjack.Services;
using Blackjack.UI;
using Reflex.Extensions;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Blackjack.Verification
{
    // Visual verification uses the isolated fixture, never the configured production address.
    public static class UiPreview
    {
        public static async Task<string> RunAsync(string server)
        {
            var scope = SceneManager.GetActiveScene().GetSceneContainer();
            var context = scope.Resolve<UiContext>();
            var auth = scope.Resolve<IAuthenticationService>();
            var rooms = scope.Resolve<IRoomService>();
            var game = scope.Resolve<IGameSessionService>();
            var email = Guid.NewGuid().ToString("N") + "@example.test";
            const string password = "Preview9!Password";
            var registered = await auth.RegisterAsync(server, email, password);
            if (!registered.Succeed) return registered.ErrorMessage;
            var login = await auth.LoginAsync(server, email, password);
            if (!login.Succeed) return login.ErrorMessage;
            await Capture("home");
            context.Go("rooms"); await Capture("rooms");
            var created = await rooms.CreateAsync("preview-" + Guid.NewGuid().ToString("N"), true, 7);
            if (!created.Succeed) return created.ErrorMessage;
            try
            {
                var joined = await rooms.JoinAsync(created.Value.RoomId);
                if (!joined.Succeed) return joined.ErrorMessage;
                context.Go("game");
                await game.ReadyAsync(true);
                await scope.Resolve<IChatService>().SendAsync("Welcome to the table.");
                scope.Resolve<ChatController>().Show(true);
                await Capture("game");
                return "Passed";
            }
            finally { await rooms.CloseAsync(); await auth.LogoutAsync(); }
        }
        static async Task Capture(string name)
        {
            await Task.Delay(700);
            await Awaitable.EndOfFrameAsync();
            var directory = Path.Combine(Application.dataPath, "../Temp"); Directory.CreateDirectory(directory);
            var texture = ScreenCapture.CaptureScreenshotAsTexture();
            try { File.WriteAllBytes(Path.Combine(directory, "blackjack-" + name + ".png"), texture.EncodeToPNG()); }
            finally { UnityEngine.Object.Destroy(texture); }
            await Task.Delay(300);
        }
    }
}

#endif
