using TMPro;
using UnityEngine;

namespace HighStakes.TableGames.Blackjack
{
    /// <summary>
    /// One card on the felt, drawn as text on a plain white card so no card art is needed. Face down it shows only
    /// its back and carries no text, so the hole card cannot be read off the table.
    /// </summary>
    public sealed class CardView : MonoBehaviour
    {
        [SerializeField] TMP_Text face;
        [SerializeField] GameObject back;
        [SerializeField] Color red = new Color(0.75f, 0.1f, 0.1f);
        [SerializeField] Color black = new Color(0.08f, 0.08f, 0.08f);

        static readonly string[] Ranks = { "A", "2", "3", "4", "5", "6", "7", "8", "9", "10", "J", "Q", "K" };

        // Indexed by Suit: spades, hearts, clubs, diamonds.
        static readonly string[] Suits = { "\u2660", "\u2665", "\u2663", "\u2666" };

        /// <summary>The card face up, or null while face down.</summary>
        public Card? Shown { get; private set; }

        public void ShowFace(Card card)
        {
            Shown = card;
            back.SetActive(false);
            face.text = Ranks[card.Rank - 1] + "\n" + Suits[(int)card.Suit];
            face.color = card.Suit == Suit.Hearts || card.Suit == Suit.Diamonds ? red : black;
        }

        public void ShowBack()
        {
            Shown = null;
            back.SetActive(true);
            face.text = "";
        }
    }
}
