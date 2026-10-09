using Blackjack._Project.Scripts.Services.Auth;
using Blackjack.Services;
using Blackjack.Services.Room;
using Blackjack.Services.Transport;
using Reflex.Core;
using Reflex.Enums;
using UnityEngine;

namespace _Project.Scripts
{
    public sealed class RootInstaller : MonoBehaviour, IInstaller
    {
        public void InstallBindings(ContainerBuilder builder)
        {
            builder.AddNakama();

            Bind<OfflineGameSessionService>(builder, typeof(IGameSessionService));
            Bind<OfflineChatService>(builder, typeof(IChatService));
        }

        static void Bind<T>(ContainerBuilder builder, params System.Type[] contracts) =>
            builder.RegisterType(typeof(T), contracts, Lifetime.Singleton, Reflex.Enums.Resolution.Lazy);
    }

    public static class NakamaExtensions
    {
        public static ContainerBuilder AddNakama(this ContainerBuilder builder)
        {
            builder.RegisterType(typeof(NakamaTransport),
                new[]
                {
                    typeof(INetworkTransport),
                    typeof(NakamaTransport),
                    typeof(NetworkTransport<Nakama.Client>)
                },
                Lifetime.Singleton, Reflex.Enums.Resolution.Eager);

            builder.RegisterType(typeof(AuthNakama), new[]
                {
                    typeof(IAuthenticationService),
                    typeof(AuthNakama),
                }, Lifetime.Singleton,
                Reflex.Enums.Resolution.Eager);

            builder.RegisterType(typeof(NakamaRoomService),
                new[]
                {
                    typeof(IRoomService),
                    typeof(NakamaRoomService),
                },
                Lifetime.Singleton, Reflex.Enums.Resolution.Eager);

            return builder;
        }
    }
}