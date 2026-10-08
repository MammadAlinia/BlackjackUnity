using UnityEngine.UIElements;

namespace Blackjack.UI
{
    // Offline mock presentation has no player hands or scores to label.
    public sealed class TableHandLabels
    {
        readonly VisualElement layer;

        public TableHandLabels(VisualElement layer) => this.layer = layer;
        public void Render() => Clear();
        public void UpdatePositions() { }
        public void Clear() => layer.Clear();
    }
}
