using System.Collections.Generic;
using System.Linq;
using _Project.Scripts;
using CardGame.Contracts;
using UnityEngine;
using UnityEngine.UIElements;

namespace Blackjack.UI
{
    public sealed class TableHandLabels
    {
        sealed class HandLabel
        {
            public readonly VisualElement View = new() { pickingMode = PickingMode.Ignore };
            public readonly Label Name = new() { pickingMode = PickingMode.Ignore, enableRichText = false };
            public readonly Label Score = new() { pickingMode = PickingMode.Ignore };
            public readonly Label Details = new() { pickingMode = PickingMode.Ignore, enableRichText = false };
            public HandLabel()
            {
                View.AddToClassList("hand-label");
                Name.AddToClassList("hand-name"); Score.AddToClassList("hand-score"); Details.AddToClassList("hand-details");
                View.Add(Name); View.Add(Score); View.Add(Details);
            }
        }

        readonly VisualElement layer;
        readonly ITablePresenter table;
        readonly Dictionary<string, HandLabel> labels = new();
        public TableHandLabels(VisualElement layer, ITablePresenter table)
        { this.layer = layer; this.table = table; }

        public void Render(GameStateView state, string localUser)
        {
            if (state?.Blackjack == null) { Clear(); return; }
            var wanted = new HashSet<string>(state.Blackjack.Players.Select(player => player.UserId)) { "dealer" };
            foreach (var id in labels.Keys.Where(id => !wanted.Contains(id)).ToArray())
            { labels[id].View.RemoveFromHierarchy(); labels.Remove(id); }
            UpdateLabel("dealer", "Dealer", state.Blackjack.DealerTotal is int total ? "Score: " + total : "Score: hidden", "", false, false);
            foreach (var player in state.Blackjack.Players)
            {
                var local = player.UserId == localUser;
                var name = string.IsNullOrWhiteSpace(player.Name) ? player.UserId : player.Name;
                var details = string.Join(" · ", new[] { player.Ready ? "Ready" : null, player.Status, player.Result,
                    player.Connected ? null : "Reconnecting" }.Where(value => !string.IsNullOrWhiteSpace(value)).Distinct());
                UpdateLabel(player.UserId, name + (local ? " (You)" : ""), "Score: " + player.Total, details,
                    local, player.UserId == state.CurrentPlayerId);
            }
            UpdatePositions();
        }

        void UpdateLabel(string id, string name, string score, string details, bool local, bool current)
        {
            if (!labels.TryGetValue(id, out var label))
            { label = new HandLabel(); label.View.name = "hand-" + id; labels.Add(id, label); layer.Add(label.View); }
            label.Name.text = name; label.Name.tooltip = name;
            label.Score.text = score;
            label.Details.text = details; label.Details.tooltip = details;
            label.Details.EnableInClassList("hidden", string.IsNullOrEmpty(details));
            label.View.EnableInClassList("local-hand", local);
            label.View.EnableInClassList("current-hand", current);
            label.View.EnableInClassList("dealer-hand", id == "dealer");
        }

        public void UpdatePositions()
        {
            var camera = Camera.main;
            foreach (var pair in labels)
            {
                var bounds = default(Bounds);
                var visible = camera != null && layer.panel != null && table.TryGetHandBounds(pair.Key, out bounds);
                pair.Value.View.EnableInClassList("hidden", !visible);
                if (!visible) continue;
                var anchor = new Vector3(bounds.center.x, bounds.max.y + 0.16f, bounds.center.z);
                if (camera.WorldToViewportPoint(anchor).z <= 0)
                { pair.Value.View.AddToClassList("hidden"); continue; }
                var position = layer.WorldToLocal(RuntimePanelUtils.CameraTransformWorldToPanel(layer.panel, anchor, camera));
                pair.Value.View.style.left = position.x;
                pair.Value.View.style.top = position.y;
            }
        }

        public void Clear()
        { layer.Clear(); labels.Clear(); }
    }
}
