using Blackjack.UI;
using Reflex.Core;
using Reflex.Enums;
using UnityEngine;
using UnityEngine.UIElements;

namespace _Project.Scripts
{
    public sealed class SceneInstaller : MonoBehaviour, IInstaller
    {
        public UIDocument document;
        public Gameplay table;

        public void InstallBindings(ContainerBuilder builder)
        {
            builder.RegisterValue(new UiContext(document));
            builder.RegisterValue(table, new[] { typeof(ITablePresenter) });

            Bind<LoginController>(builder);
            Bind<HomeController>(builder);
            Bind<RoomsController>(builder);
            Bind<GameController>(builder);
            Bind<ChatController>(builder);
            Bind<UiNavigation>(builder);
        }

        static void Bind<T>(ContainerBuilder builder) =>
            builder.RegisterType(typeof(T), Lifetime.Scoped, Reflex.Enums.Resolution.Lazy);
    }
}