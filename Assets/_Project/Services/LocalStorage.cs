using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Blackjack.Services
{
    public interface IStorage
    {
        Task<IResult<string>> GetAsync(string key, CancellationToken token = default);
        Task<IResult> SetAsync(string key, string value, CancellationToken token = default);
        Task<IResult> RemoveAsync(string key, CancellationToken token = default);
    }
    public sealed class LocalStorage : IStorage
    {
        readonly IUnityThread thread;
        public LocalStorage(IUnityThread thread) => this.thread = thread;
        public Task<IResult<string>> GetAsync(string key, CancellationToken token = default) =>
            ReadAsync(() => PlayerPrefs.HasKey(key) ? PlayerPrefs.GetString(key) : null, token);
        public Task<IResult> SetAsync(string key, string value, CancellationToken token = default) =>
            WriteAsync(() => PlayerPrefs.SetString(key, value), token);
        public Task<IResult> RemoveAsync(string key, CancellationToken token = default) =>
            WriteAsync(() => PlayerPrefs.DeleteKey(key), token);
        async Task<IResult<string>> ReadAsync(Func<string> read, CancellationToken token)
        {
            try { return Result<string>.Ok(await thread.InvokeAsync(read, token)); }
            catch (OperationCanceledException) { return Result<string>.Fail("Cancelled", "Storage operation cancelled."); }
            catch (Exception) { return Result<string>.Fail("Storage", "Could not read local storage."); }
        }
        async Task<IResult> WriteAsync(Action write, CancellationToken token)
        {
            try { return await thread.InvokeAsync(() => { write(); PlayerPrefs.Save(); return Result.Ok(); }, token); }
            catch (OperationCanceledException) { return Result.Fail("Cancelled", "Storage operation cancelled."); }
            catch (Exception) { return Result.Fail("Storage", "Could not save local storage."); }
        }
    }
}
