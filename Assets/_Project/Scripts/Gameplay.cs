using UnityEngine;

namespace _Project.Scripts
{
    public interface ITablePresenter
    {
        void Render();
        bool TryGetHandBounds(string id, out Bounds bounds);
        void Clear();
    }

    // Offline mock presenter: graphic assets stay assigned, but no cards are created or changed.
    public class Gameplay : MonoBehaviour, ITablePresenter
    {
        public CardDatabase cardDatabase;
        public Card cardPrefab;
        [SerializeField] float cardScale = 0.5f;
        [SerializeField] float cardSpacing = 0.35f;
        [SerializeField] float cardMoveDuration = 0.45f;
        [SerializeField] Vector3 cardCurveOffset = new(1f, 0.5f, 0f);
        public void Render() { /* Offline mock: presentation intentionally does nothing. */ }

        public bool TryGetHandBounds(string id, out Bounds bounds)
        { bounds = default; return false; }

        public void Clear() { /* Offline mock: there are no generated cards to clear. */ }

        void OnDestroy() => Clear();
    }
}
