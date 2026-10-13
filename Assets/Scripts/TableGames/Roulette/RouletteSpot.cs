using TMPro;
using UnityEngine;

namespace HighStakes.TableGames.Roulette
{
    /// <summary>One place on the layout chips can go, with the chip stack and amount that show what is on it.</summary>
    [RequireComponent(typeof(PressableButton))]
    public sealed class RouletteSpot : MonoBehaviour
    {
        [SerializeField] RouletteBetKind kind;
        [Tooltip("The number for a straight bet; 1-3 for a dozen or column.")]
        [SerializeField] int number;
        [SerializeField] GameObject chips;
        [SerializeField] TMP_Text amountText;

        public RouletteBet Bet
        {
            get
            {
                switch (kind)
                {
                    case RouletteBetKind.Straight: return RouletteBet.Straight(number);
                    case RouletteBetKind.Dozen: return RouletteBet.Dozen(number);
                    case RouletteBetKind.Column: return RouletteBet.Column(number);
                    default: return RouletteBet.EvenMoney(kind);
                }
            }
        }

        public PressableButton Button => GetComponent<PressableButton>();

        /// <summary>Shows the chips riding on this spot; none hides the stack.</summary>
        public void Show(long amount)
        {
            chips.SetActive(amount > 0);
            amountText.text = amount > 0 ? amount.ToString("N0") : "";
        }
    }
}
