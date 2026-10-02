#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Threading;
using System.Threading.Tasks;
using Blackjack.Services;
using CardGame.Client;
using System.IO;
using System.Net.Http;
using UnityEngine;

namespace Blackjack.Verification
{
    // Runs the same Unity services in Editor tests and development players against an isolated server.
    public static class IntegrationScenario
    {
        public static Task<IResult> RunAsync(string server) => ResultBoundary.RunAsync(async () =>
        {
            var thread = new UnityThread();
            using var first = new Actor(thread); using var second = new Actor(thread); using var takeover = new Actor(thread);
            var suffix = Guid.NewGuid().ToString("N"); const string password = "Smoke-Test9!Password";
            var diagnosticPath = Path.Combine(Application.dataPath, "../Temp"); Directory.CreateDirectory(diagnosticPath);
            void Diagnostic(string value) => File.WriteAllText(Path.Combine(diagnosticPath, "Blackjack-scenario-diagnostic.txt"), value);
            ResultBoundary.Diagnostic += Diagnostic;
            try
            {
                var firstEmail = suffix + "@example.test";
                foreach (var actor in new[] { first, second })
                {
                    var email = actor == first ? firstEmail : Guid.NewGuid().ToString("N") + "@example.test";
                    var registered = await actor.Auth.RegisterAsync(server, email, password); if (!registered.Succeed) return Step("register", registered);
                    var login = await actor.Auth.LoginAsync(server, email, password); if (!login.Succeed) return Step("login", login);
                    if (actor.PersistenceFailure != null) return Step("persist login", actor.PersistenceFailure);
                    var persisted = await actor.Storage.GetAsync(SessionPersistence.Key(actor.Network.ServerUrl));
                    if (!persisted.Succeed || string.IsNullOrEmpty(persisted.Value)) return Result.Fail("Storage", "Login credentials were not persisted.");
                }
                var created = await first.Rooms.CreateAsync("smoke-" + suffix, true, 7); if (!created.Succeed) return Step("create", created);
                foreach (var actor in new[] { first, second })
                { var joined = await actor.Rooms.JoinAsync(created.Value.RoomId); if (!joined.Succeed) return Step("join", joined); }
                var recovered = await RecoveryAsync(first, server); if (!recovered.Succeed) return recovered;
                var otherLogin = await takeover.Auth.LoginAsync(server, firstEmail, password); if (!otherLogin.Succeed) return Step("takeover login", otherLogin);
                var otherJoin = await takeover.Rooms.JoinAsync(created.Value.RoomId); if (!otherJoin.Succeed) return Step("takeover join", otherJoin);
                if (!await Until(() => first.Rooms.State == RoomConnectionState.Closed) || first.Game.CanAct("Ready"))
                    return Result.Fail("Protocol", "Replaced connection still allowed gameplay.");
                var rejoin = await first.Rooms.JoinAsync(created.Value.RoomId); if (!rejoin.Succeed) return Step("explicit rejoin", rejoin);
                if (!await Until(() => takeover.Rooms.State == RoomConnectionState.Closed)) return Result.Fail("Timeout", "Takeover closure was not delivered.");
                await takeover.Network.ShutdownAsync();
                var received = new TaskCompletionSource<bool>(); second.Chat.MessageReceived += message => received.TrySetResult(message.Message == "hello");
                var sent = await first.Chat.SendAsync("hello"); if (!sent.Succeed) return Step("chat", sent);
                if (await Task.WhenAny(received.Task, Task.Delay(5000)) != received.Task || !await received.Task) return Result.Fail("Timeout", "Chat echo was not delivered.");
                for (var round = 0; round < 2; round++)
                {
                    foreach (var actor in new[] { first, second })
                    {
                        if (!await Until(() => actor.Game.CanAct("Ready"))) return Result.Fail("Timeout", "Ready state was not synchronized.");
                        var ready = await actor.Game.ReadyAsync(true); if (!ready.Succeed) return Step("ready", ready);
                        await Task.Delay(100);
                    }
                    for (var turn = 0; turn < 12 && first.Game.State?.Phase != "Results"; turn++)
                    {
                        if (!await Until(() => first.Game.CanAct("Stand") || second.Game.CanAct("Stand") || first.Game.State?.Phase == "Results"))
                            return Result.Fail("Timeout", "No synchronized turn.");
                        if (first.Game.State.Phase == "Results") break;
                        var actor = first.Game.CanAct("Stand") ? first : second;
                        var stand = await actor.Game.ActionAsync("Stand"); if (!stand.Succeed) return Step("stand", stand);
                        await Task.Delay(100);
                    }
                    if (!await Until(() => first.Game.State?.Phase == "Results" && second.Game.State?.Phase == "Results"))
                        return Result.Fail("Timeout", "The round did not finish.");
                    if (first.Game.State.Blackjack.HiddenDealerCardCount != 0) return Result.Fail("Protocol", "Dealer cards were not revealed.");
                }
                var leave = await second.Rooms.LeaveAsync(); if (!leave.Succeed) return Step("leave", leave);
                var close = await first.Rooms.CloseAsync(); if (!close.Succeed) return Step("close", close);
                if (!await Until(() => first.Rooms.State == RoomConnectionState.Closed)) return Result.Fail("Timeout", "Room closure was not delivered.");
                await first.Network.ShutdownAsync(); first.Auth.Dispose();
                using var restored = new Actor(thread);
                var restore = await restored.Auth.RestoreAsync(server); if (!restore.Succeed) return Step("restore", restore);
                var listed = await restored.Rooms.ListAsync(); if (!listed.Succeed) return Step("restored request", listed);
                var logout = await restored.Auth.LogoutAsync(); if (!logout.Succeed) return Step("logout", logout);
                var saved = await restored.Storage.GetAsync(SessionPersistence.Key(restored.Network.ServerUrl));
                if (!saved.Succeed || saved.Value != null) return Result.Fail("Storage", "Logout did not delete saved credentials.");
                await restored.Network.ShutdownAsync();
                return Result.Ok();
            }
            finally { ResultBoundary.Diagnostic -= Diagnostic; await takeover.Network.ShutdownAsync(); await first.Network.ShutdownAsync(); await second.Auth.LogoutAsync(); await second.Network.ShutdownAsync(); }
        });
        static async Task<IResult> RecoveryAsync(Actor actor, string server)
        {
            var observed = false; var disabled = false;
            void Changed(RoomConnectionState state)
            {
                if (state != RoomConnectionState.Reconnecting) return;
                observed = true; disabled = !actor.Game.CanAct("Ready") && !actor.Game.CanAct("Stand");
            }
            var connection = actor.Rooms.Current; connection.StateChanged += Changed;
            try
            {
                using var http = new HttpClient();
                using var response = await http.GetAsync(new Uri(new Uri(server), "_smoke/disconnect?id=" + Uri.EscapeDataString(actor.Rooms.UserId)));
                if (!response.IsSuccessStatusCode) return Result.Fail("Protocol", "Fixture could not interrupt the connection.");
                if (!await Until(() => observed && actor.Rooms.State == RoomConnectionState.Connected && actor.Game.CanAct("Ready")))
                    return Result.Fail("Timeout", "Reconnect did not restore synchronized gameplay (observed=" + observed + ", state=" + actor.Rooms.State + ").");
                return disabled ? Result.Ok() : Result.Fail("Protocol", "Gameplay remained enabled during reconnect.");
            }
            finally { connection.StateChanged -= Changed; }
        }
        static IResult Step(string name, IResult result) => Result.Fail(result.ErrorCode, name + ": " + result.ErrorMessage);
        static async Task<bool> Until(Func<bool> condition)
        { for (var i = 0; i < 200; i++) { if (condition()) return true; await Task.Delay(50); } return false; }
        sealed class Actor : IDisposable
        {
            public readonly NetworkService Network = new();
            public readonly LocalStorage Storage;
            public readonly AuthenticationService Auth;
            public readonly RoomService Rooms;
            public readonly GameSessionService Game;
            public readonly ChatService Chat;
            public IResult PersistenceFailure;
            public Actor(IUnityThread thread)
            {
                Storage = new LocalStorage(thread);
                Auth = new AuthenticationService(Network, Storage, new GoogleLogin(thread), thread, new SessionPersistence(Storage, thread));
                Auth.PersistenceFailed += result => PersistenceFailure = result;
                Rooms = new RoomService(Auth, Auth, thread); Game = new GameSessionService(Rooms, thread); Chat = new ChatService(Rooms, thread);
            }
            public void Dispose() { Chat.Dispose(); Game.Dispose(); Rooms.Dispose(); Auth.Dispose(); }
        }
    }
}
#endif
