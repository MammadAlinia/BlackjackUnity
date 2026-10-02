using System;
using System.Threading;
using System.Threading.Tasks;
using CardGame.Client;
using UnityEngine;

namespace Blackjack.Services
{
    public sealed class AuthenticationService : IAuthenticationService, ISessionSource, IDisposable
    {
        readonly INetworkService network;
        readonly IStorage storage;
        readonly IGoogleLogin google;
        readonly IUnityThread thread;
        readonly SessionPersistence persistence;
        readonly SemaphoreSlim operations = new(1, 1);
        public ClientSession Session { get; private set; }
        public bool IsAuthenticated => Session?.State == SessionState.Active;
        public string AccountName { get; private set; }
        public event Action Changed;
        public event Action<IResult> PersistenceFailed;
        Action<SessionCredentials> rotated;
        Action<SessionState> stateChanged;
        public AuthenticationService(INetworkService network, IStorage storage, IGoogleLogin google, IUnityThread thread, SessionPersistence persistence)
        {
            this.network = network; this.storage = storage; this.google = google; this.thread = thread; this.persistence = persistence;
            persistence.Failed += OnPersistenceFailed;
        }
        void OnPersistenceFailed(IResult result) => PersistenceFailed?.Invoke(result);
        public Task<IResult> LoginAsync(string server, string email, string password, string code = null, CancellationToken token = default) =>
            AuthenticateAsync(server, email, () => ResultBoundary.ClientAsync(() => network.Client.LoginAsync(email, password, code, token)), token);
        public Task<IResult> GoogleAsync(string server, CancellationToken token = default) =>
            AuthenticateAsync(server, "Google account", () => google.LoginAsync(network.Client, token), token);
        public Task<IResult> RegisterAsync(string server, string email, string password, CancellationToken token = default) => SerializeAsync(async () =>
        {
            var configured = await network.ConfigureAsync(server, token);
            return configured.Succeed ? await ResultBoundary.ClientAsync(() => network.Client.RegisterAsync(email, password, token)) : configured;
        }, token);
        public Task<IResult> RestoreAsync(string server, CancellationToken token = default) => SerializeAsync(async () =>
        {
            var configured = await network.ConfigureAsync(server, token);
            if (!configured.Succeed) return configured;
            var saved = await storage.GetAsync(SessionPersistence.Key(network.ServerUrl), token);
            if (!saved.Succeed) return saved;
            if (string.IsNullOrEmpty(saved.Value)) return Result.Fail("NoSavedSession", "Sign in to play.");
            SavedSession credentials;
            try { credentials = await thread.InvokeAsync(() => JsonUtility.FromJson<SavedSession>(saved.Value), token); }
            catch (OperationCanceledException) { return Result.Fail("Cancelled", "Restoration cancelled."); }
            catch (Exception) { await persistence.ClearAsync(network.ServerUrl); return Result.Fail("InvalidCredentials", "Saved session is invalid. Sign in again."); }
            if (credentials == null || string.IsNullOrEmpty(credentials.refreshToken))
            { await persistence.ClearAsync(network.ServerUrl); return Result.Fail("InvalidCredentials", "Sign in again."); }
            var restored = await ResultBoundary.ClientAsync(() => network.Client.RestoreSessionAsync(credentials.refreshToken, token));
            if (!restored.Succeed)
            {
                if (restored.ErrorCode == "Unauthorized" || restored.ErrorCode == "InvalidRequest") await persistence.ClearAsync(network.ServerUrl);
                return restored;
            }
            return await AdoptAsync(restored.Value, credentials.accountName);
        }, token);
        Task<IResult> AuthenticateAsync(string server, string name, Func<Task<IResult<ClientSession>>> login, CancellationToken token) => SerializeAsync(async () =>
        {
            var configured = await network.ConfigureAsync(server, token);
            if (!configured.Succeed) return configured;
            var result = await login();
            return result.Succeed ? await AdoptAsync(result.Value, name) : result;
        }, token);
        async Task<IResult> AdoptAsync(ClientSession session, string name)
        {
            Unsubscribe(); Session = session; AccountName = name;
            var server = network.ServerUrl;
            rotated = credentials => thread.Post(() => { if (ReferenceEquals(Session, session)) _ = persistence.SaveAsync(server, credentials, name); });
            stateChanged = state => thread.Post(() =>
            {
                if (!ReferenceEquals(Session, session)) return;
                if (state == SessionState.Expired) { Unsubscribe(); Session = null; _ = persistence.ClearAsync(server); }
                Changed?.Invoke();
            });
            session.CredentialsChanged += rotated; session.StateChanged += stateChanged;
            await persistence.SaveAsync(server, session.Credentials, name);
            thread.Post(() => Changed?.Invoke());
            return Result.Ok();
        }
        public Task<IResult> LogoutAsync(CancellationToken token = default) => SerializeAsync(async () =>
        {
            var previous = Session; Unsubscribe(); Session = null; AccountName = null;
            // Cleanup is intentionally independent of cancellation after logout begins.
            if (previous != null) await previous.LogoutAsync();
            var cleared = await persistence.ClearAsync(network.ServerUrl);
            thread.Post(() => Changed?.Invoke());
            return cleared;
        }, token);
        Task<IResult> SerializeAsync(Func<Task<IResult>> operation, CancellationToken token) => ResultBoundary.RunAsync(async () =>
        {
            await operations.WaitAsync(token);
            try { return await operation(); } finally { operations.Release(); }
        });
        void Unsubscribe()
        {
            if (Session == null) return;
            Session.CredentialsChanged -= rotated; Session.StateChanged -= stateChanged;
        }
        public void Dispose() { Unsubscribe(); persistence.Failed -= OnPersistenceFailed; }
    }
}
