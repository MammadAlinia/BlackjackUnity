using System.Collections.Generic;
using System.Linq;
using Blackjack.Services;
using GameStateView = CardGame.Contracts.GameStateView;
using UnityEngine;

namespace _Project.Scripts
{
    public interface ITablePresenter
    {
        void Render(GameStateView state, string localUser);
        bool TryGetHandBounds(string id, out Bounds bounds);
        void Clear();
    }

    // Presentation only: the server owns every card, score, rule, deadline and outcome.
    public class Gameplay : MonoBehaviour, ITablePresenter
    {
        public CardDatabase cardDatabase;
        public Card cardPrefab;
        [SerializeField] float cardScale = 0.5f;
        [SerializeField] float cardSpacing = 0.35f;
        [SerializeField] float cardMoveDuration = 0.45f;
        [SerializeField] Vector3 cardCurveOffset = new(1f, 0.5f, 0f);
        readonly Dictionary<string, Player> hands = new();
        static readonly Vector3 DealerPosition = new(0, 2.8f, 0);
        long round = -1;
        long revision = -1;
        string roomId;

        public void Render(GameStateView state, string localUser)
        {
            if (state?.Blackjack == null)
            {
                Clear();
                return;
            }

            if (state.RoomId != roomId || state.RoundId != round)
            {
                Clear();
                roomId = state.RoomId;
                round = state.RoundId;
            }

            if (state.Revision < revision) return;
            revision = state.Revision;
            var players = state.Blackjack.Players;
            var wanted = new HashSet<string>(players.Select(p => p.UserId)) { "dealer" };

            foreach (var key in hands.Keys.Where(k => !wanted.Contains(k)).ToArray())
            {
                hands[key].Reset();
                Destroy(hands[key].gameObject);
                hands.Remove(key);
            }

            Reconcile("dealer", state.Blackjack.DealerCards, state.Blackjack.HiddenDealerCardCount, DealerPosition,
                Quaternion.identity);
            var opponents = players.Where(p => p.UserId != localUser).ToArray();
            var leftCount = (opponents.Length + 1) / 2;

            for (var i = 0; i < opponents.Length; i++)
            {
                var left = i < leftCount;
                var index = left ? i : i - leftCount;
                var count = left ? leftCount : opponents.Length - leftCount;
                var progress = count == 1 ? 0.5f : (float)index / (count - 1);
                var angle = Mathf.Lerp(left ? 120f : 15f, left ? 165f : 60f, progress) * Mathf.Deg2Rad;
                var position = new Vector3(5.5f * Mathf.Cos(angle), 0.8f - 4f * Mathf.Sin(angle), 0);

                if (count > 2)
                {
                    var height = left ? progress : 1f - progress;
                    position = new Vector3(
                        (left ? -1f : 1f) * Mathf.Lerp(3.1f, 5.5f, Mathf.Sin(height * Mathf.PI * 0.5f)),
                        Mathf.Lerp(-2.9f, 2.2f, height), 0);
                }

                Reconcile(opponents[i].UserId, opponents[i].Cards, 0, position, FaceDealer(position));
            }

            var self = players.FirstOrDefault(p => p.UserId == localUser);

            if (self != null)
            {
                var position = new Vector3(0, -3.2f, 0);
                Reconcile(self.UserId, self.Cards, 0, position, FaceDealer(position));
            }
        }

        static Quaternion FaceDealer(Vector3 position)
        {
            var direction = DealerPosition - position;
            return Quaternion.Euler(0, 0, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f);
        }

        void Reconcile(string id, string[] cards, int hidden, Vector3 position, Quaternion rotation)
        {
            if (!hands.TryGetValue(id, out var hand))
            {
                hand = PlayerExtensions.New();
                hand.name = id;
                hand.transform.SetParent(transform);
                hand.ConfigureLayout(cardSpacing, cardMoveDuration, cardCurveOffset);
                hands.Add(id, hand);
            }

            if (hand.transform.position != position || hand.transform.rotation != rotation)
            {
                hand.transform.SetPositionAndRotation(position, rotation);
                hand.RefreshLayout();
            }

            var count = cards.Length + hidden;

            while (hand.cards.Count > count)
            {
                var card = hand.cards[hand.cards.Count - 1];
                hand.Remove(card);
                Destroy(card.gameObject);
            }

            for (var i = 0; i < count; i++)
            {
                var parsed = i < cards.Length ? CardDatabase.Parse(cards[i]) : Result<int>.Ok(-1);
                if (!parsed.Succeed) continue;
                var texture = parsed.Value < 0
                    ? Result<Texture2D>.Ok(cardDatabase.hiddenTextures[0])
                    : cardDatabase.GetCardTexture(parsed.Value);
                if (!texture.Succeed) continue;

                if (i >= hand.cards.Count)
                {
                    var card = Instantiate(cardPrefab, transform.position, cardPrefab.transform.rotation,
                        hand.transform);
                    card.transform.localRotation = cardPrefab.transform.localRotation;
                    card.transform.localScale = cardPrefab.transform.localScale * cardScale;
                    card.Init(texture.Value, cardDatabase.hiddenTextures[0], parsed.Value);
                    hand.Add(card);
                }
                else if (hand.cards[i].myCardIndex != parsed.Value) hand.cards[i].Reveal(texture.Value, parsed.Value);
            }
        }

        public bool TryGetHandBounds(string id, out Bounds bounds)
        {
            bounds = default;
            if (!hands.TryGetValue(id, out var hand)) return false;
            bounds = new Bounds(hand.transform.position, Vector3.zero);
            var centerOffset = (hand.cards.Count - 1) * 0.5f;

            for (var i = 0; i < hand.cards.Count; i++)
            {
                var card = hand.cards[i];
                if (card == null || card.meshRenderer == null) continue;
                var position = hand.transform.position + hand.transform.right * ((i - centerOffset) * cardSpacing);
                var extents = card.meshRenderer.bounds.extents;
                bounds.Encapsulate(position - extents);
                bounds.Encapsulate(position + extents);
            }

            return true;
        }

        public void Clear()
        {
            foreach (var hand in hands.Values)
            {
                hand.Reset();
                Destroy(hand.gameObject);
            }

            hands.Clear();
            round = -1;
            revision = -1;
            roomId = null;
        }

        void OnDestroy() => Clear();
    }
}