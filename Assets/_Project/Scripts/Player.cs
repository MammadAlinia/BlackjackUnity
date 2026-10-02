using System;
using System.Collections.Generic;
using System.Linq;
using _Project.Scripts;
using LitMotion;
using LitMotion.Extensions;
using UnityEngine;

namespace _Project.Scripts
{
    public class Player : MonoBehaviour
    {
        public List<Card> cards = new List<Card>();

        readonly Dictionary<Card, MotionHandle> _cardMotions = new();

        float _cardSpacing = 1f;
        float _moveDuration = 0.45f;
        Vector3 _curveOffset = new(1f, 0.5f, 0f);

        public Player ConfigureLayout(float spacing, float duration, Vector3 curveOffset)
        {
            _cardSpacing = Mathf.Max(0f, spacing);
            _moveDuration = Mathf.Max(0f, duration);
            _curveOffset = curveOffset;
            return this;
        }

        public void Add(Card card)
        {
            if (card == null)
                return;

            cards.Add(card);
            LayoutCards(card);
        }

        public void Remove(Card card)
        {
            if (!cards.Remove(card))
                return;

            CancelMotion(card);
            LayoutCards();
        }

        public void Reset()
        {
            CancelAllMotions();

            foreach (var card in cards)
            {
                if (card != null)
                    Destroy(card.gameObject);
            }

            cards.Clear();
        }

        void OnDestroy()
        {
            CancelAllMotions();
        }

        void LayoutCards(Card curvedCard = null)
        {
            var centerOffset = (cards.Count - 1) * 0.5f;

            for (var i = 0; i < cards.Count; i++)
            {
                var card = cards[i];
                if (card == null)
                    continue;

                CancelMotion(card);

                var targetPosition = transform.position + transform.right * ((i - centerOffset) * _cardSpacing);
                var startPosition = card.transform.position;

                if (_moveDuration <= 0f)
                {
                    card.transform.position = targetPosition;
                    continue;
                }

                MotionHandle motion;

                if (card == curvedCard)
                {
                    var controlPoint = Vector3.Lerp(startPosition, targetPosition, 0.5f) + _curveOffset;
                    motion = LMotion.Create(0f, 1f, _moveDuration)
                        .WithEase(Ease.OutQuad)
                        .Bind(progress => card.transform.position = EvaluateQuadraticBezier(
                            startPosition, controlPoint, targetPosition, progress))
                        .AddTo(card);
                }
                else
                {
                    motion = LMotion.Create(startPosition, targetPosition, _moveDuration)
                        .WithEase(Ease.OutQuad)
                        .BindToPosition(card.transform)
                        .AddTo(card);
                }

                _cardMotions[card] = motion;
            }
        }

        static Vector3 EvaluateQuadraticBezier(Vector3 start, Vector3 control, Vector3 end, float progress)
        {
            var inverseProgress = 1f - progress;
            return inverseProgress * inverseProgress * start
                   + 2f * inverseProgress * progress * control
                   + progress * progress * end;
        }

        void CancelMotion(Card card)
        {
            if (!_cardMotions.Remove(card, out var motion))
                return;

            motion.TryCancel();
        }

        void CancelAllMotions()
        {
            foreach (var motion in _cardMotions.Values)
                motion.TryCancel();

            _cardMotions.Clear();
        }

        public void RefreshLayout() => LayoutCards();
    }
}

public static class PlayerExtensions
{
    public static Player New() => new GameObject().AddComponent<Player>();

    public static Player SetPosition(this Player player, Vector3 position)
    {
        player.transform.position = position;
        return player;
    }
}
