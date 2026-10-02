using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Blackjack.Services;
using UnityEngine.UIElements;

namespace Blackjack.UI
{
    public abstract class ScreenController : IDisposable
    {
        protected readonly UiContext Context;
        protected readonly VisualElement View;
        readonly List<Action> cleanup = new();
        CancellationTokenSource lifetime = new();
        protected CancellationToken Token => lifetime.Token;
        bool disposed;
        bool busy;
        protected ScreenController(UiContext context, string name) { Context = context; View = context.Root.Q(name); }
        protected T Find<T>(string name) where T : VisualElement => View.Q<T>(name);
        protected void Click(string name, Func<CancellationToken, Task<IResult>> operation)
        {
            var button = Find<Button>(name);
            Action callback = () => Run(operation);
            button.clicked += callback;
            cleanup.Add(() => button.clicked -= callback);
        }
        protected async void Run(Func<CancellationToken, Task<IResult>> operation)
        {
            if (busy || disposed) return;
            busy = true; var token = Token; View.SetEnabled(false); Context.Status("");
            var result = await ResultBoundary.RunAsync(() => operation(token));
            busy = false;
            if (disposed || token.IsCancellationRequested) return;
            View.SetEnabled(true); Context.Report(result);
            Refresh();
        }
        public virtual void Show(bool visible)
        {
            View.EnableInClassList("hidden", !visible);
            if (!visible) lifetime.Cancel();
            else { lifetime.Dispose(); lifetime = new CancellationTokenSource(); View.SetEnabled(!busy); Refresh(); }
        }
        public virtual void Refresh() { }
        public virtual void Dispose()
        {
            if (disposed) return;
            disposed = true; lifetime.Cancel(); lifetime.Dispose(); foreach (var release in cleanup) release(); cleanup.Clear();
        }
    }
}
