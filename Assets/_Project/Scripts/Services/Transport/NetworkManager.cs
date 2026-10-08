using Cysharp.Threading.Tasks;
using Nakama;

namespace Blackjack.Services.Transport
{
    public interface INetworkTransport
    {
    }

    public abstract class NetworkTransport<T> : INetworkTransport
    {
        public T Client { get; protected set; }
        public abstract UniTask<Result<T>> Connect(params string[] args);
    }

    public class NakamaTransport : NetworkTransport<Client>
    {
        public override UniTask<Result<Client>> Connect(params string[] args)
        {
            Client = new Client(args[0]);

            return UniTask.FromResult(Result.Ok(Client));
        }
    }
}