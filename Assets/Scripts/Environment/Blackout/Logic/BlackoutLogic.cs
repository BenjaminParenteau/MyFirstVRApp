using System;
using HighStakes.Environment.Audio;

namespace HighStakes.Environment.Blackout
{
    /// <summary>Where the blackout is in its cycle.</summary>
    public enum BlackoutStage
    {
        /// <summary>Lights on, nothing happening.</summary>
        Off,

        /// <summary>The lights are dying. Short, and the moment the player realises it was deliberate.</summary>
        PowerDown,

        /// <summary>Fully dark. Sound is the only sense.</summary>
        Dark,

        /// <summary>Power coming back. Deliberately slower than going out.</summary>
        PowerUp,
    }

    /// <summary>
    /// The blackout mode's decisions, with no Unity in them so they can be tested: the stage machine, how dark it
    /// is at a given moment, echolocation timing and loudness, the proximity warning curve, and how a bearing to
    /// an objective turns into something you can hear.
    /// </summary>
    public static class BlackoutLogic
    {
        /// <summary>Lights die fast — this is a breaker being thrown, not a dimmer.</summary>
        public const float PowerDownSeconds = 1.2f;

        /// <summary>Power returns slower than it left, so the player isn't startled into a flinch in a headset.</summary>
        public const float PowerUpSeconds = 2.2f;

        /// <summary>m/s. Real speed of sound, so echo delays are honest rather than arbitrary.</summary>
        public const float SpeedOfSound = 343f;

        // ------------------------------------------------------------------ stage machine

        /// <summary>
        /// Advance the stage. <paramref name="elapsed"/> is time spent in the current stage;
        /// <paramref name="wantDark"/> is whether something is currently asking for darkness.
        /// </summary>
        public static BlackoutStage Advance(BlackoutStage current, bool wantDark, float elapsed)
        {
            switch (current)
            {
                case BlackoutStage.Off:
                    return wantDark ? BlackoutStage.PowerDown : BlackoutStage.Off;

                case BlackoutStage.PowerDown:
                    if (!wantDark) return BlackoutStage.PowerUp;                 // cancelled mid-fade
                    return elapsed >= PowerDownSeconds ? BlackoutStage.Dark : BlackoutStage.PowerDown;

                case BlackoutStage.Dark:
                    return wantDark ? BlackoutStage.Dark : BlackoutStage.PowerUp;

                case BlackoutStage.PowerUp:
                    if (wantDark) return BlackoutStage.PowerDown;                // asked for dark again mid-recovery
                    return elapsed >= PowerUpSeconds ? BlackoutStage.Off : BlackoutStage.PowerUp;

                default:
                    return BlackoutStage.Off;
            }
        }

        /// <summary>
        /// How dark it is right now: 0 = normal lighting, 1 = fully black. Eased, because a hard cut to black in a
        /// headset reads as a dropped frame rather than as the lights going out.
        /// </summary>
        public static float Darkness(BlackoutStage stage, float elapsed)
        {
            switch (stage)
            {
                case BlackoutStage.Off: return 0f;
                case BlackoutStage.Dark: return 1f;
                case BlackoutStage.PowerDown: return Ease(Clamp01(elapsed / PowerDownSeconds));
                case BlackoutStage.PowerUp: return 1f - Ease(Clamp01(elapsed / PowerUpSeconds));
                default: return 0f;
            }
        }

        /// <summary>True in the stages where the player cannot see and the dark mix should be applied.</summary>
        public static bool IsDarkEnoughToNeedSound(BlackoutStage stage)
            => stage == BlackoutStage.Dark || stage == BlackoutStage.PowerDown;

        // ------------------------------------------------------------------ echolocation

        /// <summary>
        /// How long a ping takes to come back from something <paramref name="distance"/> away: out and back at the
        /// real speed of sound, with a floor so a surface right in front of you is still a distinct echo rather
        /// than part of the click.
        /// </summary>
        public static float ReturnDelay(float distance)
        {
            float physical = 2f * Math.Max(0f, distance) / SpeedOfSound;
            return Math.Max(0.045f, physical);
        }

        /// <summary>
        /// How loud a return is, 0..1. Falls off with distance and goes silent past range. Soft surfaces come back
        /// quieter than hard ones, which is the cue that tells felt from concrete.
        /// </summary>
        public static float ReturnVolume(float distance, float maxRange, SurfaceHardness hardness)
        {
            if (maxRange <= 0f || distance >= maxRange) return 0f;
            float falloff = 1f - Clamp01(distance / maxRange);
            float byMaterial = hardness == SurfaceHardness.Hard ? 1f
                             : hardness == SurfaceHardness.Medium ? 0.75f
                             : 0.45f;
            return Clamp01(falloff * falloff * byMaterial);
        }

        /// <summary>
        /// Pitch for a return, so close things read higher and far things lower. A second distance cue layered on
        /// top of the delay, because delay alone is hard to judge for anything under a couple of metres.
        /// </summary>
        public static float ReturnPitch(float distance, float maxRange)
        {
            float t = Clamp01(maxRange <= 0f ? 1f : distance / maxRange);
            return 1.25f - 0.45f * t;
        }

        // ------------------------------------------------------------------ proximity

        /// <summary>
        /// The "something is close" tone's volume. Silent beyond <paramref name="far"/>, rising as the player
        /// closes in. This is the difference between navigating and groping.
        /// </summary>
        public static float ProximityVolume(float distance, float near, float far)
        {
            if (far <= near) return 0f;
            if (distance >= far) return 0f;
            if (distance <= near) return 1f;
            return 1f - Clamp01((distance - near) / (far - near));
        }

        /// <summary>
        /// The proximity tone's pitch, rising as a surface gets closer. Kept inside a musical-ish range so it
        /// reads as information rather than as an alarm — it should feel like the room closing in.
        /// </summary>
        public static float ProximityPitch(float distance, float near, float far)
            => 0.8f + 0.7f * ProximityVolume(distance, near, far);

        // ------------------------------------------------------------------ orientation

        /// <summary>
        /// Stereo pan, -1 (hard left) to +1 (hard right), for something at <paramref name="signedAngleDeg"/> off
        /// the player's forward (negative = left). Saturates at 90°, so anything to the side pans fully; behind is
        /// deliberately ambiguous, which is honest — you turn your head to resolve it, exactly as you would in
        /// the dark.
        /// </summary>
        public static float BearingPan(float signedAngleDeg)
        {
            float a = Wrap180(signedAngleDeg);
            float side = a / 90f;
            return side < -1f ? -1f : (side > 1f ? 1f : side);
        }

        /// <summary>
        /// Ticks per second for an orientation beacon: faster the closer the player is to facing it. Turns
        /// "which way is out" into something you can hear yourself getting right, without ever telling the player
        /// where to go.
        /// </summary>
        public static float BearingTickRate(float signedAngleDeg, float slowest = 1.2f, float fastest = 7f)
        {
            float off = Math.Abs(Wrap180(signedAngleDeg)) / 180f;
            return fastest + (slowest - fastest) * Clamp01(off);
        }

        /// <summary>True when the player is facing closely enough at a bearing to count as "aimed at it".</summary>
        public static bool IsFacing(float signedAngleDeg, float toleranceDeg = 25f)
            => Math.Abs(Wrap180(signedAngleDeg)) <= toleranceDeg;

        // ------------------------------------------------------------------ the run

        /// <summary>
        /// Seconds before the Guide offers a nudge, growing each time so he escalates rather than nagging. The
        /// first silence is long on purpose: being lost is the mode, and rescuing the player too early removes it.
        /// </summary>
        public static float NudgeAfterSeconds(int nudgesAlready)
        {
            switch (nudgesAlready)
            {
                case 0: return 30f;
                case 1: return 22f;
                case 2: return 16f;
                default: return 12f;
            }
        }

        /// <summary>
        /// Score for a completed run: time is the main term, wall bumps cost, and pings cost a little so
        /// spamming the sonar into a solid picture of the room isn't the optimal strategy. Never below zero.
        /// </summary>
        public static int Score(float seconds, int bumps, int pings)
        {
            float raw = 1000f - seconds * 4f - bumps * 40f - pings * 5f;
            return raw <= 0f ? 0 : (int)Math.Round(raw);
        }

        /// <summary>A word for a score, for the end-of-run line.</summary>
        public static string Rank(int score)
        {
            if (score >= 800) return "GHOST";
            if (score >= 600) return "PROFESSIONAL";
            if (score >= 380) return "LUCKY";
            if (score > 0) return "HEARD";
            return "CAUGHT";
        }

        // ------------------------------------------------------------------ helpers

        static float Clamp01(float v) => v < 0f ? 0f : (v > 1f ? 1f : v);

        static float Ease(float t) => t * t * (3f - 2f * t);

        /// <summary>Fold an angle into -180..180 so bearing maths never depends on how it was measured.</summary>
        public static float Wrap180(float deg)
        {
            deg = (float)(deg % 360.0);
            if (deg > 180f) deg -= 360f;
            if (deg < -180f) deg += 360f;
            return deg;
        }
    }
}
