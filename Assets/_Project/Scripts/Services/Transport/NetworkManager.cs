using Cysharp.Threading.Tasks;
using Nakama;

namespace Blackjack.Services.Transport
{
    public abstract class NetworkManager<T>
    {
        public T Client { get; protected set; }
        public abstract UniTask<Result<T>> Connect(string[] args);
    }

    public class NakamaTransport : NetworkManager<Nakama.Client>
    {
        public override UniTask<Result<Client>> Connect(string[] args)
        {
            Client = new Client(args[0]);

            return UniTask.FromResult(Result.Ok(Client));
        }
    }
}