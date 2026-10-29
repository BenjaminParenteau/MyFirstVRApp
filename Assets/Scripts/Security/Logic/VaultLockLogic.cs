using System;

namespace HighStakes.Security
{
    /// <summary>Seat the correct key, make a short turn, then turn the wheel to release the door.</summary>
    public sealed class VaultLockLogic
    {
        public const string KeyCode = "meridian_reserve_03";
        public const float KeyTravel = 45f, WheelTravel = 90f;
        public bool KeySeated { get; private set; }
        public bool Armed { get; private set; }
        public bool IsOpen { get; private set; }
        public float KeyAngle { get; private set; }
        public float WheelAngle { get; private set; }

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
            Armed = KeyAngle >= KeyTravel;
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
