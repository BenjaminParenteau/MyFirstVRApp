using System;

namespace HighStakes.Security
{
    /// <summary>Mechanical interlock: seat the correct key, hold its quarter turn, then withdraw the bolts.</summary>
    public sealed class VaultLockLogic
    {
        public const string KeyCode = "meridian_reserve_03";
        public const float KeyTravel = 90f, WheelTravel = 270f, HoldSeconds = .6f;
        public bool KeySeated { get; private set; }
        public bool Armed { get; private set; }
        public bool IsOpen { get; private set; }
        public float KeyAngle { get; private set; }
        public float WheelAngle { get; private set; }
        float dwell;

        public bool Insert(string code)
        {
            if (code != KeyCode || KeySeated || IsOpen) return false;
            KeySeated = true;
            return true;
        }

        public void TurnKey(float degrees)
        {
            if (!KeySeated || Armed || IsOpen || float.IsNaN(degrees) || float.IsInfinity(degrees)) return;
            KeyAngle = Clamp(KeyAngle + degrees, 0, KeyTravel);
            if (KeyAngle < KeyTravel - 1) dwell = 0;
        }

        public void Tick(float seconds, bool holdingKey)
        {
            if (!KeySeated || Armed || IsOpen || seconds <= 0 || float.IsNaN(seconds) || float.IsInfinity(seconds)) return;
            if (holdingKey && KeyAngle >= KeyTravel - 1)
            {
                dwell += seconds;
                if (dwell >= HoldSeconds) { Armed = true; KeyAngle = KeyTravel; }
            }
            else
            {
                dwell = 0;
                if (!holdingKey) KeyAngle = Math.Max(0, KeyAngle - seconds * 120);
            }
        }

        public void TurnWheel(float degrees)
        {
            if (!Armed || IsOpen || float.IsNaN(degrees) || float.IsInfinity(degrees)) return;
            WheelAngle = Clamp(WheelAngle + degrees, 0, WheelTravel);
            IsOpen = WheelAngle >= WheelTravel;
        }

        static float Clamp(float value, float min, float max) => Math.Max(min, Math.Min(max, value));
    }
}
