using UnityEngine;

namespace HighStakes.Environment
{
    /// <summary>Scale and grid standards, in metres. Mirrors docs/StyleGuide.md.</summary>
    public static class StyleScale
    {
        public const float GridSnap = 0.5f;
        public const float TileSize = 2f;
        public const float CorridorWidth = 2f;
        public const float DoorWidth = 1f;
        public const float DoorHeight = 2.1f;
        public const float WallThickness = 0.15f;
        public const float CeilingHeight = 3f;
        public const float TableTopHeight = 0.75f;
        public const float SeatHeight = 0.45f;
        public const float ChipDiameter = 0.039f;
        public const float FigureHeight = 1.8f;
        public const float MaxStepHeight = 0.3f;
    }

    /// <summary>
    /// Single reference to every shared visual asset (materials, lighting, modular prefabs).
    /// Generated and refreshed by the "High Stakes/Build Style Kit" menu item; owned by Slot D.
    /// Other slots read from it and never edit it.
    /// </summary>
    [CreateAssetMenu(fileName = "SharedStyleKit", menuName = "High Stakes/Shared Style Kit")]
    public class SharedStyleKit : ScriptableObject
    {
        [Header("Warm palette (main floor, VIP)")]
        public Material carpetRed;
        public Material feltGreen;
        public Material woodDark;
        public Material brassGold;
        public Material plasterCream;
        public Material fabricBurgundy;

        [Header("Cool palette (back-of-house, vault)")]
        public Material concreteCool;
        public Material steelCool;
        public Material tileCool;
        public Material screenGlow;

        [Header("Props")]
        public Material chipWhite;
        public Material chipRed;
        public Material chipGreen;
        public Material chipBlack;
        public Material keycardPlastic;

        [Header("UI / accent colours")]
        public Color accentCyan = new Color32(0x38, 0xD6, 0xE8, 0xFF);
        public Color alertRed = new Color32(0xD8, 0x34, 0x2C, 0xFF);
        public Color safeGreen = new Color32(0x3C, 0xC8, 0x6E, 0xFF);

        [Header("Lighting")]
        public UnityEngine.Rendering.VolumeProfile volumeProfile;
        public GameObject lightWarmFloor;
        public GameObject lightCoolCorridor;

        [Header("Modular greybox prefabs")]
        public GameObject floorTileWarm;
        public GameObject floorTileCool;
        public GameObject wallWarm;
        public GameObject wallCool;
        public GameObject doorwayWarm;
        public GameObject doorwayCool;
        public GameObject ceilingTile;
        public GameObject tableRound;
        public GameObject chair;
        public GameObject chip;
        public GameObject keycard;
        public GameObject doorReader;
        public GameObject scaleReference;

        [Header("Player")]
        [Tooltip("The one player rig (XR Origin). Prefab Variant of the VR Template rig with our settings. Never add another rig; change this one.")]
        public GameObject playerRig;
    }
}
