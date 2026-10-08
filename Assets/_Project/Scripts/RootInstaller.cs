using Blackjack._Project.Scripts.Services.Auth;
using Blackjack.Services;
using Reflex.Core;
using Reflex.Enums;
using UnityEngine;

namespace _Project.Scripts
{
    public sealed class RootInstaller : MonoBehaviour, IInstaller
    {
        public void InstallBindings(ContainerBuilder builder)
        {
            Bind<AuthMockEmailPassword>(builder, typeof(IAuthenticationService));
            Bind<OfflineRoomService>(builder, typeof(IRoomService));
            Bind<OfflineGameSessionService>(builder, typeof(IGameSessionService));
            Bind<OfflineChatService>(builder, typeof(IChatService));
        }

        static void Bind<T>(ContainerBuilder builder, params System.Type[] contracts) =>
            builder.RegisterType(typeof(T), contracts, Lifetime.Singleton, Reflex.Enums.Resolution.Lazy);
    }
}
