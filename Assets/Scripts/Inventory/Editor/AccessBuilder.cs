using HighStakes.Environment;
using HighStakes.Inventory;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using static TableKit;

/// <summary>
/// Builds the VIP keycard prefab and the card reader that locks the casino's VIP door until that card touches it.
/// The Pit Boss hands the card out (PitBossBuilder wires his KeycardGiver to the prefab built here).
/// </summary>
public static class AccessBuilder
{
    public const string VipKeycardPath = "Assets/Content/Inventory/Prefabs/Item_VIPKeycard.prefab";
    public const string VipAccess = "VIP";

    /// <summary>A gold credit-card-sized VIP card: grabbable, floats where it is handed over until taken.</summary>
    public static Keycard BuildVipKeycard()
    {
        var kit = Kit(out var layer);
        if (kit == null) return null;

        var root = new GameObject("Item_VIPKeycard") { layer = layer };
        // The card lies in its XY plane, face on -Z. The collider is thicker than the card so a dropped card can't
        // slip through the floor.
        var body = Part(kit.brassGold, root.transform, "Card", PrimitiveType.Cube, Vector3.zero, new Vector3(0.0856f, 0.054f, 0.0008f), false, false);
        body.layer = layer;
        root.AddComponent<BoxCollider>().size = new Vector3(0.0856f, 0.054f, 0.006f);
        var vip = Text(root.transform, "VIP", new Vector3(0f, 0.006f, -0.0006f), Quaternion.identity, new Vector2(0.07f, 0.03f), 0.01f, 0.4f, kit.chipBlack.color);
        vip.text = "VIP";
        vip.fontStyle = FontStyles.Bold;
        var name = Text(root.transform, "Meridian", new Vector3(0f, -0.017f, -0.0006f), Quaternion.identity, new Vector2(0.07f, 0.01f), 0.005f, 0.1f, kit.chipBlack.color);
        name.text = "THE MERIDIAN";

        var rigidbody = root.AddComponent<Rigidbody>();
        rigidbody.mass = 0.01f;
        rigidbody.isKinematic = true; // hangs where it was handed over until taken
        rigidbody.useGravity = false;
        rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
        rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        var grab = root.AddComponent<XRGrabInteractable>();
        grab.useDynamicAttach = true;
        var card = root.AddComponent<Keycard>();
        Set(card, "accessId", p => p.stringValue = VipAccess);

        return SavePrefab(root, VipKeycardPath).GetComponent<Keycard>();
    }

    /// <summary>
    /// Locks <paramref name="door"/> behind a reader on Ashton's VIP door: a trigger the card is touched to, a status
    /// light (red locked, green open) and a sign on the door. Positions are in the door's space (front faces -Z).
    /// </summary>
    public static void AddVipReader(DoorPortal door, Vector3 readerLocal)
    {
        var kit = Kit(out _);
        door.locked = true;

        var reader = new GameObject("VIP_Reader").transform;
        reader.SetParent(door.transform, false);
        reader.localPosition = readerLocal;
        var trigger = reader.gameObject.AddComponent<BoxCollider>();
        trigger.isTrigger = true;
        trigger.size = new Vector3(0.22f, 0.3f, 0.25f);
        trigger.center = new Vector3(0f, 0f, -0.08f); // reaches out in front of the plate, where the card is held
        var light = Part(kit.chipRed, reader, "StatusLight", PrimitiveType.Cube, new Vector3(0f, 0.15f, -0.03f), new Vector3(0.06f, 0.02f, 0.01f), false, false);

        var sign = Text(door.transform, "VIP_Sign", new Vector3(0f, 2.05f, -0.1f), Quaternion.identity, new Vector2(1.6f, 0.12f), 0.1f, 1.5f, kit.brassGold.color);
        sign.fontStyle = FontStyles.Bold;

        var component = reader.gameObject.AddComponent<KeycardReader>();
        Set(component, "accessId", p => p.stringValue = VipAccess);
        SetRef(component, "door", door);
        SetRef(component, "statusLight", light.GetComponent<Renderer>());
        SetRef(component, "lockedMaterial", kit.chipRed);
        SetRef(component, "openMaterial", kit.chipGreen);
        SetRef(component, "sign", sign);
    }
}
