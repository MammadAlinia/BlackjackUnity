using System;
using System.Threading;
using System.Threading.Tasks;
using CardGame.Client;

namespace Blackjack.Services
{
    public interface INetworkService
    {
        string ServerUrl { get; }
        CardGameClient Client { get; }
        Task<IResult> ConfigureAsync(string url, CancellationToken token = default);
        Task<IResult> ShutdownAsync();
    }
    public sealed class NetworkService : INetworkService
    {
        public string ServerUrl => Client?.ServerUri.AbsoluteUri ?? "https://localhost:7093/";
        public CardGameClient Client { get; private set; }
        public Task<IResult> ConfigureAsync(string url, CancellationToken token = default) => ResultBoundary.RunAsync(async () =>
        {
            token.ThrowIfCancellationRequested();
            var next = new CardGameClient(url);
            if (Client != null) await Client.DisposeAsync();
            Client = next;
            return Result.Ok();
        });
        public Task<IResult> ShutdownAsync() => ResultBoundary.RunAsync(async () =>
        { var previous = Client; Client = null; if (previous != null) await previous.DisposeAsync(); return Result.Ok(); });
    }
}
