using System;

namespace HighStakes.UI
{
    /// <summary>The player's comfort choices. Defaults: smooth movement, snap turning, no vignette, standing.</summary>
    public struct ComfortChoices : IEquatable<ComfortChoices>
    {
        public bool SmoothMove;
        public bool SmoothTurn;
        public bool Vignette;
        public bool Seated;

        public bool Equals(ComfortChoices other) =>
            SmoothMove == other.SmoothMove && SmoothTurn == other.SmoothTurn && Vignette == other.Vignette && Seated == other.Seated;

        public override bool Equals(object obj) => obj is ComfortChoices other && Equals(other);

        public override int GetHashCode() => SettingsLogic.ToBits(this);
    }

    /// <summary>
    /// Pure rules for the comfort settings shown in the notebook's SETTINGS spread and on the start screen: one row per
    /// choice, each pressed to switch it. No Unity types, so it is covered by EditMode tests.
    /// </summary>
    public static class SettingsLogic
    {
        public const int Move = 0, Turn = 1, Vignette = 2, Seated = 3;

        /// <summary>Rows on the COMFORT page, in order: move, turn, vignette, seated.</summary>
        public const int Rows = 4;

        public const string Intro = "Point at a line and pull the trigger to change it. Your choices stay on this headset.";

        public static ComfortChoices Default => new ComfortChoices { SmoothMove = true };

        /// <summary>The row's text, for example "MOVE: TELEPORT".</summary>
        public static string Label(ComfortChoices c, int row)
        {
            switch (row)
            {
                case Move: return "MOVE: " + (c.SmoothMove ? "SMOOTH" : "TELEPORT");
                case Turn: return "TURN: " + (c.SmoothTurn ? "SMOOTH" : "SNAP");
                case Vignette: return "VIGNETTE: " + (c.Vignette ? "ON" : "OFF");
                case Seated: return "SEATED: " + (c.Seated ? "ON" : "OFF");
                default: throw new ArgumentOutOfRangeException(nameof(row));
            }
        }

        /// <summary>What the row's current choice does, for the page's note box.</summary>
        public static string Describe(ComfortChoices c, int row)
        {
            switch (row)
            {
                case Move: return c.SmoothMove ? "Left stick walks you smoothly." : "Push the left stick, aim the arc, release to jump there. Easiest on the stomach.";
                case Turn: return c.SmoothTurn ? "Right stick turns you smoothly." : "Right stick turns you in 45 degree steps. Easiest on the stomach.";
                case Vignette: return c.Vignette ? "The edges of your view darken while you move or turn." : "Full view while moving.";
                case Seated: return c.Seated ? "Your view is lifted to standing height, so you can play sitting down." : "Your view is at your real height.";
                default: throw new ArgumentOutOfRangeException(nameof(row));
            }
        }

        /// <summary>True when the row's choice is the "on" or "more comfortable" one; shown as the row's tick.</summary>
        public static bool IsOn(ComfortChoices c, int row)
        {
            switch (row)
            {
                case Move: return !c.SmoothMove;
                case Turn: return !c.SmoothTurn;
                case Vignette: return c.Vignette;
                case Seated: return c.Seated;
                default: throw new ArgumentOutOfRangeException(nameof(row));
            }
        }

        /// <summary>The choices with one row switched.</summary>
        public static ComfortChoices Toggle(ComfortChoices c, int row)
        {
            switch (row)
            {
                case Move: c.SmoothMove = !c.SmoothMove; break;
                case Turn: c.SmoothTurn = !c.SmoothTurn; break;
                case Vignette: c.Vignette = !c.Vignette; break;
                case Seated: c.Seated = !c.Seated; break;
                default: throw new ArgumentOutOfRangeException(nameof(row));
            }
            return c;
        }

        /// <summary>Packs the choices into one int for saving (PlayerPrefs).</summary>
        public static int ToBits(ComfortChoices c) =>
            (c.SmoothMove ? 1 : 0) | (c.SmoothTurn ? 2 : 0) | (c.Vignette ? 4 : 0) | (c.Seated ? 8 : 0);

        public static ComfortChoices FromBits(int bits) => new ComfortChoices
        {
            SmoothMove = (bits & 1) != 0,
            SmoothTurn = (bits & 2) != 0,
            Vignette = (bits & 4) != 0,
            Seated = (bits & 8) != 0,
        };
    }
}
