using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using CardGame.Client;
using UnityEngine;

namespace Blackjack.Services
{
    // Queue order includes deletion: an in-flight refresh can never resurrect a logged-out session.
    public sealed class SessionPersistence
    {
        readonly IStorage storage;
        readonly IUnityThread thread;
        readonly object gate = new();
        Task<IResult> pending = Task.FromResult(Result.Ok());
        public event Action<IResult> Failed;
        public SessionPersistence(IStorage storage, IUnityThread thread) { this.storage = storage; this.thread = thread; }
        public static string Key(string server)
        {
            using var hash = SHA256.Create();
            return "blackjack.session." + BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(server))).Replace("-", "");
        }
        public Task<IResult> SaveAsync(string server, SessionCredentials credentials, string name) => Queue(async () =>
        {
            var json = await thread.InvokeAsync(() => JsonUtility.ToJson(new SavedSession
            { accessToken = credentials.AccessToken, refreshToken = credentials.RefreshToken, expiresAt = credentials.ExpiresAtUtc.ToUnixTimeSeconds(), accountName = name }));
            return await storage.SetAsync(Key(server), json);
        });
        public Task<IResult> ClearAsync(string server) => Queue(() => storage.RemoveAsync(Key(server)));
        Task<IResult> Queue(Func<Task<IResult>> write)
        {
            lock (gate) return pending = WriteAfterAsync(pending, write);
        }
        async Task<IResult> WriteAfterAsync(Task<IResult> previous, Func<Task<IResult>> write)
        {
            await previous;
            var result = await ResultBoundary.RunAsync(write);
            if (!result.Succeed) thread.Post(() => Failed?.Invoke(result));
            return result;
        }
    }
}
