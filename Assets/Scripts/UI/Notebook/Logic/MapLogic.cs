using System;
using System.Collections.Generic;

namespace HighStakes.UI
{
    /// <summary>One room on the notebook's map: its area scene, its label and where it's drawn on the page.</summary>
    public readonly struct MapArea
    {
        public readonly string Scene;
        public readonly string Label;
        /// <summary>Centre on the page in metres from the page centre: U to the reader's right, V down the page.</summary>
        public readonly float U, V;
        public readonly float Width, Height;
        /// <summary>One line about the room for the map page's "YOU ARE HERE" box.</summary>
        public readonly string Note;

        public MapArea(string scene, string label, float u, float v, float width, float height, string note)
        {
            Scene = scene;
            Label = label;
            Note = note;
            U = u;
            V = v;
            Width = width;
            Height = height;
        }
    }

    /// <summary>
    /// Pure rules for the notebook's map page: which areas exist, how they connect (following the doors: casino to the
    /// VIP lounge and the security hall, the hall to its three wings), and which rooms are drawn once visited. No Unity
    /// types, so it is covered by EditMode tests.
    /// </summary>
    public static class MapLogic
    {
        public const string Unknown = "?";

        /// <summary>The "YOU ARE HERE" box when the current scene isn't on the map.</summary>
        public const string OffMap = "Somewhere off the map.";

        /// <summary>
        /// The rooms, laid out in the map page's framed box (about 12 x 9.5 cm): the three wings on top, the hall, then
        /// the casino floor.
        /// </summary>
        public static readonly MapArea[] Areas =
        {
            new MapArea("MVP_SecurityOffice", "OFFICE", -0.04f, -0.034f, 0.034f, 0.02f, "Security office. Watch for the breaker panel."),
            new MapArea("MVP_CameraRoom", "CAMERAS", 0f, -0.034f, 0.034f, 0.02f, "Every camera in the building ends up here."),
            new MapArea("MVP_Vault", "VAULT", 0.04f, -0.034f, 0.034f, 0.02f, "The vault. What we came for."),
            new MapArea("MVP_SecurityHall", "HALL", 0f, -0.003f, 0.104f, 0.016f, "Staff corridor behind the STAFF ONLY door."),
            new MapArea("MVP_VIP", "VIP", -0.042f, 0.031f, 0.028f, 0.02f, "The high-roller lounge. Listen in."),
            new MapArea("MVP_Casino", "CASINO", 0.019f, 0.031f, 0.066f, 0.028f, "The main floor: tables, bar and the Pit Boss."),
        };

        /// <summary>Doors between rooms, as pairs of indexes into <see cref="Areas"/>.</summary>
        public static readonly int[][] Paths =
        {
            new[] { 5, 4 }, // casino - VIP lounge
            new[] { 5, 3 }, // casino - security hall (STAFF ONLY door)
            new[] { 3, 0 }, // hall - office
            new[] { 3, 1 }, // hall - camera room
            new[] { 3, 2 }, // hall - vault
        };

        /// <summary>The index of the area for this scene in <see cref="Areas"/>, or -1 if it isn't on the map.</summary>
        public static int IndexOf(string scene)
        {
            for (int i = 0; i < Areas.Length; i++)
            {
                if (string.Equals(Areas[i].Scene, scene, StringComparison.Ordinal))
                    return i;
            }
            return -1;
        }

        /// <summary>Marks a scene visited. Returns false if it was already visited or isn't on the map.</summary>
        public static bool Visit(ISet<string> visited, string scene)
        {
            return IndexOf(scene) >= 0 && visited.Add(scene);
        }

        /// <summary>The room's label: its name once visited, <see cref="Unknown"/> before.</summary>
        public static string LabelFor(int area, ISet<string> visited)
        {
            return visited.Contains(Areas[area].Scene) ? Areas[area].Label : Unknown;
        }

        /// <summary>A door between two rooms is drawn once both rooms have been visited.</summary>
        public static bool ShowsPath(int path, ISet<string> visited)
        {
            return visited.Contains(Areas[Paths[path][0]].Scene) && visited.Contains(Areas[Paths[path][1]].Scene);
        }
    }
}
