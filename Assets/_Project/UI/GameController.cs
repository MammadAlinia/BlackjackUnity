using System;
using System.Linq;
using System.Threading.Tasks;
using _Project.Scripts;
using Blackjack.Services;
using CardGame.Client;
using UnityEngine;
using UnityEngine.UIElements;

namespace Blackjack.UI
{
    public sealed class GameController : ScreenController
    {
        readonly IRoomService rooms;
        readonly IGameSessionService game;
        readonly ITablePresenter table;
        readonly TableHandLabels handLabels;
        readonly IVisualElementScheduledItem timer;
        long timerRevision = -1;
        float serverSampleTime;
        DateTimeOffset serverTime;
        public GameController(UiContext context, IRoomService rooms, IGameSessionService game, ITablePresenter table) : base(context, "gameScreen")
        {
            this.rooms = rooms; this.game = game; this.table = table;
            handLabels = new TableHandLabels(Find<VisualElement>("handLabels"), table);
            rooms.Changed += Refresh; game.Changed += Refresh;
            Click("ready", ct => game.ReadyAsync(!game.CanAct("Unready"), ct));
            Click("hit", ct => game.ActionAsync("Hit", ct));
            Click("stand", ct => game.ActionAsync("Stand", ct));
            Click("refreshState", ct => game.RefreshAsync(ct));
            Click("rejoin", ct => rooms.JoinAsync(rooms.RoomId, ct));
            Click("leave", async ct => { var result = await rooms.LeaveAsync(ct); if (result.Succeed) context.Go("rooms"); return result; });
            Click("closeRoom", ct => rooms.CloseAsync(ct));
            timer = View.schedule.Execute(() => { UpdateTimer(); handLabels.UpdatePositions(); }).Every(100); timer.Pause();
        }
        public override void Show(bool visible)
        { base.Show(visible); if (visible) timer.Resume(); else { timer.Pause(); handLabels.Clear(); table.Clear(); timerRevision = -1; } }
        public override void Refresh()
        {
            if (Context.ActiveScreen != "game") return;
            var state = game.State;
            Find<Label>("roomTitle").text = (rooms.RoomId ?? "Room") + " · " + rooms.State;
            Find<Button>("ready").text = game.CanAct("Unready") ? "Unready" : "Ready";
            Find<Button>("ready").SetEnabled(game.CanAct("Ready") || game.CanAct("Unready"));
            Find<Button>("hit").SetEnabled(game.CanAct("Hit")); Find<Button>("stand").SetEnabled(game.CanAct("Stand"));
            Find<Button>("closeRoom").EnableInClassList("hidden", rooms.Details?.CreatorId != rooms.UserId);
            Find<Button>("rejoin").EnableInClassList("hidden", rooms.State != RoomConnectionState.Disconnected && rooms.State != RoomConnectionState.Closed);
            Find<Button>("refreshState").SetEnabled(rooms.State == RoomConnectionState.Connected && state != null);
            Find<Label>("phase").text = state?.Phase ?? (rooms.Details?.GameType == null ? "Gameplay is disabled in this room." : "Waiting for synchronized state…");
            if (state != null && state.Revision != timerRevision)
            { timerRevision = state.Revision; serverTime = state.ServerTimeUtc; serverSampleTime = Time.realtimeSinceStartup; }
            table.Render(state, rooms.UserId); handLabels.Render(state, rooms.UserId); UpdateTimer();
        }
        void UpdateTimer()
        {
            var deadline = game.State?.TurnEndsAtUtc;
            Find<Label>("turnTimer").text = deadline.HasValue ? "Turn: " + Math.Max(0, (int)Math.Ceiling((deadline.Value - serverTime).TotalSeconds - (Time.realtimeSinceStartup - serverSampleTime))) + "s" : "";
        }
        public override void Dispose() { rooms.Changed -= Refresh; game.Changed -= Refresh; timer.Pause(); handLabels.Clear(); table.Clear(); base.Dispose(); }
    }
}
