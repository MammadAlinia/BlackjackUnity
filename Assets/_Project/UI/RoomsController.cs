using System.Threading;
using Blackjack.Services;
using Cysharp.Threading.Tasks;
using UnityEngine.UIElements;

namespace Blackjack.UI
{
    public sealed class RoomsController : ScreenController
    {
        readonly IRoomService rooms;

        public RoomsController(UiContext context, IRoomService rooms) : base(context, "roomsScreen")
        {
            this.rooms = rooms;
            Click("roomsBack", _ =>
            {
                context.Go("home");
                return UniTask.FromResult(Result.Ok());
            });
            Click("refreshRooms", LoadAsync);
            Click("joinRoom", JoinAsync);
            Click("createRoom", CreateAsync);
        }

        public override void Show(bool visible)
        {
            base.Show(visible);
            if (visible) Run(LoadAsync);
        }

        async UniTask<Result> CreateAsync(CancellationToken token)
        {
            var created = await rooms.CreateAsync(Find<TextField>("roomName").value,
                Find<Toggle>("enableChat").value, Find<IntegerField>("capacity").value, token);
            return created.Succeed ? await OpenGameAsync(Find<TextField>("roomName").value.Trim(), token) : created;
        }

        async UniTask<Result> JoinAsync(CancellationToken token)
        {
            return await OpenGameAsync(Find<TextField>("roomName").value.Trim(), token);
        }

        async UniTask<Result> OpenGameAsync(string name, CancellationToken token)
        {
            var result = await rooms.JoinAsync(name, token);
            if (result.Succeed && !token.IsCancellationRequested) Context.Go("game");
            return result;
        }

        async UniTask<Result> LoadAsync(CancellationToken token)
        {
            var result = await rooms.ListAsync(token);
            if (!result.Succeed) return Result.Fail(result.ErrorCode, result.ErrorMessage);

            var container = Find<ScrollView>("roomList");
            container.Clear();
            container.Add(new Label("No offline rooms are available."));
            return Result.Ok();
        }
    }
}