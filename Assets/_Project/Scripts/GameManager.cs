using Blackjack.UI;
using Reflex.Attributes;
using Reflex.Core;
using UnityEngine;

namespace _Project.Scripts
{
    public sealed class GameManager : MonoBehaviour
    {
        [Inject] Container container;
        UiNavigation navigation;

        void Start()
        {
            navigation = container.Resolve<UiNavigation>();
            container.Resolve<UiContext>().Status("Offline mock mode. No backend is connected.");
        }

        void OnDestroy() => navigation?.Dispose();
    }
}
