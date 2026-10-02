using System;
using System.Threading;
using System.Threading.Tasks;
using Blackjack.Services;
using UnityEngine.UIElements;

namespace Blackjack.UI
{
    public sealed class RoomsController : ScreenController
    {
        readonly IRoomService rooms;
        public RoomsController(UiContext context, IRoomService rooms) : base(context, "roomsScreen")
        {
            this.rooms = rooms;
            Click("roomsBack", _ => { context.Go("home"); return Task.FromResult(Result.Ok()); });
            Click("refreshRooms", LoadAsync);
            Click("joinRoom", token => JoinAsync(Find<TextField>("roomName").value.Trim(), token));
            Click("createRoom", async token =>
            {
                var result = await rooms.CreateAsync(Find<TextField>("roomName").value.Trim(), Find<Toggle>("enableChat").value, Find<IntegerField>("capacity").value, token);
                return result.Succeed ? await JoinAsync(result.Value.RoomId, token) : result;
            });
        }
        public override void Show(bool visible) { base.Show(visible); if (visible) Run(LoadAsync); }
        async Task<IResult> JoinAsync(string id, CancellationToken token)
        { var result = await rooms.JoinAsync(id, token); if (result.Succeed && !token.IsCancellationRequested) Context.Go("game"); return result; }
        async Task<IResult> LoadAsync(CancellationToken token)
        {
            var list = await rooms.ListAsync(token);
            if (!list.Succeed) return list;
            var details = await Task.WhenAll(Array.ConvertAll(list.Value, id => rooms.DetailsAsync(id, token)));
            if (token.IsCancellationRequested) return Result.Fail("Cancelled", "Refresh cancelled.");
            var container = Find<ScrollView>("roomList"); container.Clear();
            if (details.Length == 0) container.Add(new Label("No rooms yet. Create a table below."));
            foreach (var result in details)
            {
                if (!result.Succeed) { container.Add(new Label(result.ErrorMessage)); continue; }
                var room = result.Value;
                var row = new VisualElement(); row.AddToClassList("room-row");
                row.Add(new Label(room.RoomId + " · " + (room.ConnectedCount + room.ReservedCount) + "/" + room.MaxPlayers
                    + " · " + (room.GameType ?? "no game") + (room.ChatEnabled ? " · chat" : "")));
                var join = new Button(() => Run(ct => JoinAsync(room.RoomId, ct))) { text = "Join" };
                row.Add(join); container.Add(row);
            }
            return Result.Ok();
        }
    }
}
