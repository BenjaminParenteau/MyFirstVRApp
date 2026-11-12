using System;

namespace HighStakes.Environment
{
    /// <summary>What the watch shows for one destination, and whether pressing it does anything.</summary>
    public enum TravelState
    {
        /// <summary>Never been there, so it isn't on the watch as a place yet.</summary>
        Unknown,

        /// <summary>Been there, but the card that opened it isn't in hand any more.</summary>
        Locked,

        /// <summary>Where the player is standing: shown, but pressing it does nothing.</summary>
        Here,

        /// <summary>Press to travel.</summary>
        Ready,
    }

    /// <summary>One place the watch can send the player back to.</summary>
    public readonly struct TravelDestination
    {
        /// <summary>Area scene, as SceneFlow names it.</summary>
        public readonly string Scene;

        /// <summary>Short name on the watch. Kept to about eight characters so the rows don't wrap.</summary>
        public readonly string Label;

        /// <summary>
        /// portalId of the door to arrive at, which is what decides where fast travel actually drops the player.
        /// For the three back rooms this is the door inside the room, not the one at the top of its hallway — the
        /// point of fast travel is to skip the corridor, and the hallway and the room are separate islands 40 m
        /// apart in the same scene anyway (HallwayBuilder, "their own islands").
        /// </summary>
        public readonly string ArrivalDoor;

        /// <summary>KeycardAccess id the player must still hold, or null when the area needs no card.</summary>
        public readonly string RequiredAccess;

        public TravelDestination(string scene, string label, string arrivalDoor, string requiredAccess)
        {
            Scene = scene;
            Label = label;
            ArrivalDoor = arrivalDoor;
            RequiredAccess = requiredAccess;
        }
    }

    /// <summary>
    /// The rules behind the watch's travel list: where the player may jump to, given where they have been and which
    /// cards they still hold. No Unity types, so EditMode tests cover it.
    /// <br/>
    /// The guiding rule is that fast travel never opens a door walking wouldn't: a destination has to have been
    /// reached on foot once, and the card its door asks for has to still be held. So this can't be used to skip a
    /// puzzle or get into the vault early — only to avoid walking somewhere already earned.
    /// </summary>
    public static class FastTravelLogic
    {
        /// <summary>Access id of the VIP lounge door's reader (KeycardReader's default).</summary>
        public const string VipAccess = "VIP";

        /// <summary>Access id of the casino's STAFF ONLY door, which every back-of-house area sits behind.</summary>
        public const string StaffAccess = "STAFF";

        /// <summary>
        /// The six areas, in the order they appear on the watch: the floor and the lounge first, then the back of
        /// house. Scene names match SceneFlow; door ids match the ones the builders write (WorldBuilder for the
        /// casino, VipRoomBuilder for the lounge, HallwayBuilder for the hub and the three wings).
        /// </summary>
        public static readonly TravelDestination[] Destinations =
        {
            new TravelDestination("MVP_Casino", "CASINO", "Casino_Staff", null),
            new TravelDestination("MVP_VIP", "VIP", "VIP_HallStart", VipAccess),
            new TravelDestination("MVP_SecurityHall", "HALL", "Hub_Start", StaffAccess),
            new TravelDestination("MVP_SecurityOffice", "OFFICE", "RoomA_Door", StaffAccess),
            new TravelDestination("MVP_CameraRoom", "CAMERAS", "RoomB_Door", StaffAccess),
            new TravelDestination("MVP_Vault", "VAULT", "RoomC_Door", StaffAccess),
        };

        /// <summary>Index of a scene in <see cref="Destinations"/>, or -1 when it isn't a travel destination.</summary>
        public static int IndexOf(string scene)
        {
            for (int i = 0; i < Destinations.Length; i++)
                if (string.Equals(Destinations[i].Scene, scene, StringComparison.Ordinal))
                    return i;
            return -1;
        }

        /// <summary>
        /// What the watch shows for one destination. <paramref name="visited"/> and <paramref name="hasAccess"/> are
        /// supplied by the caller (SceneFlow and KeycardAccess), so the rule itself stays a function of plain values.
        /// </summary>
        public static TravelState StateOf(in TravelDestination destination, bool visited, bool hasAccess,
            string currentScene)
        {
            if (string.Equals(destination.Scene, currentScene, StringComparison.Ordinal)) return TravelState.Here;
            if (!visited) return TravelState.Unknown;
            if (destination.RequiredAccess != null && !hasAccess) return TravelState.Locked;
            return TravelState.Ready;
        }

        /// <summary>Only <see cref="TravelState.Ready"/> travels; the rest are shown but do nothing when pressed.</summary>
        public static bool CanTravel(TravelState state) => state == TravelState.Ready;

        /// <summary>
        /// Why a press did nothing, for the watch's one-line reply. Null when the press travels.
        /// </summary>
        public static string RefusalFor(TravelState state)
        {
            switch (state)
            {
                case TravelState.Here: return "YOU ARE HERE";
                case TravelState.Unknown: return "NOT BEEN THERE YET";
                case TravelState.Locked: return "KEYCARD REQUIRED";
                default: return null;
            }
        }
    }
}
