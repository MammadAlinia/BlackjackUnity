using Reflex.Core;
using UnityEngine;
using Reflex.Enums;
using Blackjack.Services;

namespace _Project.Scripts
{
    public class RootInstaller : MonoBehaviour, IInstaller
    {
        public void InstallBindings(ContainerBuilder containerBuilder)
        {
            Bind<UnityThread>(containerBuilder, typeof(IUnityThread));
            Bind<LocalStorage>(containerBuilder, typeof(IStorage));
            Bind<NetworkService>(containerBuilder, typeof(INetworkService));
            Bind<GoogleLogin>(containerBuilder, typeof(IGoogleLogin));
            Bind<SessionPersistence>(containerBuilder, typeof(SessionPersistence));
            Bind<AuthenticationService>(containerBuilder, typeof(IAuthenticationService), typeof(ISessionSource));
            Bind<RoomService>(containerBuilder, typeof(IRoomService), typeof(IRoomConnectionSource));
            Bind<GameSessionService>(containerBuilder, typeof(IGameSessionService));
            Bind<ChatService>(containerBuilder, typeof(IChatService));
        }
        static void Bind<T>(ContainerBuilder builder, params System.Type[] contracts) =>
            builder.RegisterType(typeof(T), contracts, Lifetime.Singleton, Reflex.Enums.Resolution.Lazy);
    }
}
