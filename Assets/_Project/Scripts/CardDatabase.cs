using Blackjack.Services;
using UnityEngine;

namespace _Project.Scripts
{
    [CreateAssetMenu(menuName = "Data/CardDatabase")]
    public class CardDatabase : ScriptableObject
    {
        public Texture2D[] hiddenTextures;
        public Texture2D[] clubs, hearts, spades, diamonds;
        public Result<Texture2D> GetCardTexture(int value)
        {
            if (value < 0 || value > 51) return Result<Texture2D>.Fail("InvalidCard", "Invalid card index.");
            var textures = (value / 13) switch { 0 => clubs, 1 => hearts, 2 => spades, 3 => diamonds, _ => null };
            return textures != null && value % 13 < textures.Length && textures[value % 13] != null
                ? Result<Texture2D>.Ok(textures[value % 13]) : Result<Texture2D>.Fail("MissingTexture", "Card artwork is missing.");
        }
        public static Result<int> Parse(string card)
        {
            if (string.IsNullOrWhiteSpace(card) || card.Length < 2 || card.Length > 3)
                return Result<int>.Fail("InvalidCard", "Invalid card name.");
            var suit = "CHSD".IndexOf(card[card.Length - 1]);
            var text = card.Substring(0, card.Length - 1);
            var rank = text switch { "A" => 1, "J" => 11, "Q" => 12, "K" => 13, _ => int.TryParse(text, out var n) ? n : 0 };
            return suit >= 0 && rank >= 1 && rank <= 13 ? Result<int>.Ok(suit * 13 + rank - 1)
                : Result<int>.Fail("InvalidCard", "Invalid card name.");
        }
    }
}
