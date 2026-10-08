using Blackjack.Services;
using _Project.Scripts;
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

        public GameController(UiContext context, IRoomService rooms, IGameSessionService game, ITablePresenter table)
            : base(context, "gameScreen")
        {
            this.rooms = rooms;
            this.game = game;
            this.table = table;
            handLabels = new TableHandLabels(Find<VisualElement>("handLabels"));
            Click("ready", ct => game.ReadyAsync(true, ct));
            Click("hit", ct => game.ActionAsync("Hit", ct));
            Click("stand", ct => game.ActionAsync("Stand", ct));
            Click("refreshState", ct => game.RefreshAsync(ct));
            Click("leave", async ct =>
            {
                var result = await rooms.LeaveAsync(ct);
                if (result.Succeed) context.Go("rooms");
                return result;
            });
        }

        public override void Show(bool visible)
        {
            base.Show(visible);
            if (!visible) { handLabels.Clear(); table.Clear(); }
        }

        public override void Refresh()
        {
            if (Context.ActiveScreen != "game") return;
            Find<Label>("roomTitle").text = "Offline table";
            Find<Label>("phase").text = "Offline mock: gameplay is disabled.";
            Find<Label>("turnTimer").text = string.Empty;
            Find<Button>("ready").SetEnabled(true);
            Find<Button>("hit").SetEnabled(true);
            Find<Button>("stand").SetEnabled(true);
            Find<Button>("refreshState").SetEnabled(true);
            table.Render();
            handLabels.Render();
        }

        public override void Dispose()
        {
            handLabels.Clear();
            table.Clear();
            base.Dispose();
        }
    }
}
