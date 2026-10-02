using System;
using System.Threading;
using System.Threading.Tasks;

namespace Blackjack.Services
{
    public interface IUnityThread
    {
        void Post(Action action);
        Task<T> InvokeAsync<T>(Func<T> action, CancellationToken token = default);
    }
    public sealed class UnityThread : IUnityThread
    {
        readonly SynchronizationContext context = SynchronizationContext.Current;
        public void Post(Action action)
        {
            if (SynchronizationContext.Current == context) action();
            else context.Post(_ => action(), null);
        }
        public Task<T> InvokeAsync<T>(Func<T> action, CancellationToken token = default)
        {
            var completion = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
            Post(() =>
            {
                if (token.IsCancellationRequested) { completion.TrySetCanceled(token); return; }
                try { completion.TrySetResult(action()); }
                catch (Exception exception) { completion.TrySetException(exception); }
            });
            return completion.Task;
        }
    }
}
