using System.Threading;
using Blackjack.Services;
using Blackjack.UI;
using Reflex.Attributes;
using Reflex.Core;
using UnityEngine;

namespace _Project.Scripts
{
    public class GameManager : MonoBehaviour
    {
        [Inject] Container container;
        [Inject] INetworkService network;
        readonly CancellationTokenSource lifetime = new();
        UiNavigation navigation;
        async void Start()
        {
            navigation = container.Resolve<UiNavigation>();
            var context = container.Resolve<UiContext>();
            context.Status("Restoring saved session…");
            var result = await container.Resolve<LoginController>().RestoreAsync(lifetime.Token);
            if (!lifetime.IsCancellationRequested) context.Report(result);
        }
        void OnDestroy()
        {
            lifetime.Cancel(); navigation?.Dispose();
            // Shutting down transports preserves stored credentials for the next launch.
            _ = network?.ShutdownAsync();
            lifetime.Dispose();
        }
    }
}
