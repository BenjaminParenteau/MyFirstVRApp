using System.IO;
using HighStakes.Core;
using HighStakes.Environment;
using HighStakes.Security;
using HighStakes.UI;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using Object = UnityEngine.Object;

/// <summary>Reusable custody station, double-bit key and mechanical lock; no source scene regeneration.</summary>
public static class VaultKeyBuilder
{
    const string Folder = "Assets/Content/Progression/VaultDressing";
    static SharedStyleKit kit;
    static Material steel, dark, brass;
    static int layer;

    [MenuItem("High Stakes/Vault/Build Custodian Key System")]
    public static void Build()
    {
        kit = TableKit.Kit(out layer);if (!kit) return;
        Directory.CreateDirectory(Folder + "/Resources");AssetDatabase.Refresh();
        string viewPath = Folder + "/Resources/VaultDoorViewMaterial.mat";
        if (!AssetDatabase.LoadAssetAtPath<Material>(viewPath))
            AssetDatabase.CreateAsset(new Material(Shader.Find("Universal Render Pipeline/Unlit")), viewPath);
        steel = AssetDatabase.LoadAssetAtPath<Material>(Folder + "/Materials/Brushed steel.mat");
        dark = AssetDatabase.LoadAssetAtPath<Material>(Folder + "/Materials/Gunmetal.mat");brass = kit.brassGold;
        var scene = EditorSceneManager.NewPreviewScene();
        var holder = new GameObject("Key system build");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(holder, scene);
        try { Key(holder.transform);Station(holder.transform);Mechanism(holder.transform);AssetDatabase.SaveAssets(); }
        finally {Object.DestroyImmediate(holder);EditorSceneManager.ClosePreviewScene(scene);}
        Debug.Log("[VaultKey] Built custody station, serial-matched double-bit key and trapped-key bolt interlock.");
    }

    static Transform Group(Transform p, string name, Vector3 pos)
    {
        var t = new GameObject(name).transform;t.SetParent(p, false);t.localPosition = pos;return t;
    }
    static GameObject Part(Transform p, string name, Vector3 pos, Vector3 size, Material mat, PrimitiveType type = PrimitiveType.Cube)
    {
        var go = GameObject.CreatePrimitive(type);go.name = name;go.transform.SetParent(p, false);
        go.transform.localPosition = pos;go.transform.localScale = size;go.GetComponent<Renderer>().sharedMaterial = mat;
        Object.DestroyImmediate(go.GetComponent<Collider>());return go;
    }
    static TextMeshPro Text(Transform p, string words, Vector3 pos, Vector2 size, float font, Color color)
    {
        var t = Group(p, "Engraving", pos).gameObject.AddComponent<TextMeshPro>();
        t.font = TMP_Settings.defaultFontAsset;t.text = words;t.fontSize = font * 10;t.color = color;
        t.enableAutoSizing = true;t.fontSizeMax = font * 10;t.fontSizeMin = font * 5;
        t.alignment = TextAlignmentOptions.Center;t.rectTransform.sizeDelta = size;t.ForceMeshUpdate();return t;
    }
    static void Save(Transform root, string name)
    {
        root.name = name;PrefabUtility.SaveAsPrefabAsset(root.gameObject, Folder + "/Resources/" + name + ".prefab");
        Object.DestroyImmediate(root.gameObject);
    }

    static void Key(Transform parent)
    {
        var root = Group(parent, "VaultCustodianKey", Vector3.zero);root.gameObject.layer = layer;
        var body = root.gameObject.AddComponent<Rigidbody>();body.isKinematic = true;body.useGravity = false;body.mass = .18f;
        var collider = root.gameObject.AddComponent<BoxCollider>();collider.center = new Vector3(0, 0, .035f);collider.size = new Vector3(.09f, .095f, .23f);
        var grab = root.gameObject.AddComponent<XRGrabInteractable>();grab.throwOnDetach = false;
        grab.movementType = XRBaseInteractable.MovementType.Kinematic;
        var key = root.gameObject.AddComponent<VaultKeyItem>();
        var itemPath = Folder + "/SO_Item_CustodianKey.asset";
        var item = AssetDatabase.LoadAssetAtPath<ItemData>(itemPath);
        if (!item) { item = ScriptableObject.CreateInstance<ItemData>();AssetDatabase.CreateAsset(item, itemPath); }
        item.id = VaultLockLogic.KeyCode;item.displayName = "Reserve 03 Custodian Key";
        item.description = "A heavy, serial-matched double-bit treasury key from Security Operations. Insert with its bow upright; turn clockwise to the quarter-turn stop and hold until the safety latch clicks. The key remains captive. Then turn the vault handwheel clockwise three quarters of a revolution to withdraw the bolts.";
        EditorUtility.SetDirty(item);
        var pickup = root.gameObject.AddComponent<PickupItem>();
        var serialized = new SerializedObject(pickup);serialized.FindProperty("item").objectReferenceValue = item;serialized.ApplyModifiedPropertiesWithoutUndo();
        UnityEventTools.AddPersistentListener(pickup.Collected, key.Collect);
        // The thick bow, offset shoulder and asymmetric opposing wards make this a recognisable mechanical key.
        var bow = Group(root, "Oval brass bow", new Vector3(0, 0, -.045f));
        bow.localScale = new Vector3(.14f, .18f, .20f);
        bow.gameObject.AddComponent<MeshFilter>().sharedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(Folder + "/Meshes/HandleWheel.asset");
        bow.gameObject.AddComponent<MeshRenderer>().sharedMaterial = brass;
        Part(root, "Forged shaft", new Vector3(0, 0, .04f), new Vector3(.012f, .012f, .16f), steel, PrimitiveType.Cylinder).transform.localRotation = Quaternion.Euler(90, 0, 0);
        // Cylinders extend along local Y, so shaft length is the Y scale before rotation.
        root.Find("Forged shaft").localScale = new Vector3(.012f, .08f, .012f);
        Part(root, "Shoulder stop", Vector3.zero, new Vector3(.044f, .014f, .018f), brass);
        Part(root, "Upper bit", new Vector3(.013f, 0, .098f), new Vector3(.027f, .014f, .042f), steel);
        Part(root, "Opposing lower bit", new Vector3(-.012f, 0, .091f), new Vector3(.024f, .014f, .033f), steel);
        for (int n = 0; n < 4; n++)
        {
            float depth = new[] { .014f, .022f, .010f, .019f }[n];
            Part(root, "Cut ward " + (n + 1), new Vector3(depth, .009f, .082f + n * .009f), new Vector3(.008f, .009f, .006f), brass);
        }
        Part(root, "Serial escutcheon", new Vector3(0, 0, -.052f), new Vector3(.056f, .024f, .008f), dark);
        Text(root, "03", new Vector3(0, 0, -.057f), new Vector2(.045f, .02f), .018f, new Color(.94f, .76f, .36f));
        Save(root, "VaultCustodianKey");
    }

    static void Station(Transform parent)
    {
        var root = Group(parent, "VaultKeyCustodyStation", Vector3.zero);
        Part(root, "Custody pedestal", new Vector3(0, .49f, .08f), new Vector3(.63f, .98f, .46f), dark);
        var support = root.gameObject.AddComponent<BoxCollider>();support.center = new Vector3(0, .5f, .08f);support.size = new Vector3(.63f, 1, .46f);
        Part(root, "Brass trimmed tray", new Vector3(0, 1.005f, .04f), new Vector3(.67f, .04f, .5f), brass);
        Part(root, "Recessed key bed", new Vector3(0, 1.03f, .04f), new Vector3(.59f, .012f, .42f), kit.fabricBurgundy);
        for (int side = -1; side <= 1; side += 2)
            Part(root, "Key support", new Vector3(side * .052f, 1.063f, -.03f), new Vector3(.025f, .05f, .15f), dark);
        Part(root, "Custody backplate", new Vector3(0, 1.35f, .27f), new Vector3(.67f, .61f, .035f), dark);
        Text(root, "RESERVE 03\nCUSTODIAN KEY", new Vector3(0, 1.51f, .247f), new Vector2(.59f, .19f), .075f, new Color(.94f, .76f, .36f));
        Text(root, "SERIAL MATCHED / DOUBLE BIT\nQUARTER TURN + HOLD\nTHEN WITHDRAW BOLTS AT HANDWHEEL\nKEY REMAINS CAPTIVE", new Vector3(0, 1.27f, .247f), new Vector2(.59f, .24f), .039f, Color.white);
        Text(root, "SECURITY OPERATIONS\nNIGHT SHIFT / KEY CUSTODY", new Vector3(0, .75f, -.155f), new Vector2(.55f, .2f), .055f, Color.white);
        Text(root, "EMPTY CRADLE = KEY IN SERVICE", new Vector3(0, 1.038f, -.18f), new Vector2(.55f, .04f), .029f, Color.white).transform.localRotation = Quaternion.Euler(90, 0, 0);
        Save(root, "VaultKeyCustodyStation");
    }

    static void Mechanism(Transform parent)
    {
        var root = Group(parent, "VaultKeyMechanism", Vector3.zero);
        var owner = root.gameObject.AddComponent<VaultKeyLock>();
        Part(root, "Lever lock housing", new Vector3(.74f, 1.4f, -.21f), new Vector3(.36f, .43f, .16f), dark);
        var plate = Part(root, "Numbered cylinder plate", new Vector3(.74f, 1.4f, -.30f), new Vector3(.29f, .37f, .025f), brass);
        Text(root, "03", new Vector3(.74f, 1.53f, -.318f), new Vector2(.18f, .07f), .065f, Color.black);
        owner.cylinder = Group(root, "Quarter-turn cylinder", new Vector3(.74f, 1.38f, -.33f));
        Part(owner.cylinder, "Cylinder face", Vector3.zero, new Vector3(.095f, .015f, .095f), steel, PrimitiveType.Cylinder).transform.localRotation = Quaternion.Euler(90, 0, 0);
        Part(owner.cylinder, "Double-bit keyway", new Vector3(0, 0, -.018f), new Vector3(.035f, .013f, .006f), kit.chipBlack);
        owner.socket = Group(root, "Captive key seat", new Vector3(.74f, 1.38f, -.43f));
        owner.keyHandle = Handle(root, "Grip key / turn wrist", new Vector3(.74f, 1.38f, -.46f), owner, true);
        owner.keyHandle.gameObject.AddComponent<BoxCollider>().size = new Vector3(.14f, .16f, .16f);
        owner.wheelHandle = Handle(root, "Grip handwheel / sweep hand clockwise", new Vector3(0, 1.4f, -.36f), owner, false);
        for (int i = 0; i < 8; i++)
        {
            float a = i * Mathf.PI / 4;
            var collider = owner.wheelHandle.gameObject.AddComponent<SphereCollider>();
            collider.center = new Vector3(Mathf.Cos(a) * .3f, Mathf.Sin(a) * .3f, 0);collider.radius = .065f;
        }
        // Explicit lists also work when the builder added colliders after the interactable.
        foreach (var handle in new[] { owner.keyHandle, owner.wheelHandle })
        {
            var interactable = handle.GetComponent<XRSimpleInteractable>();interactable.colliders.Clear();
            interactable.colliders.AddRange(handle.GetComponents<Collider>());
        }
        owner.bolts = new Transform[3];
        for (int i = 0; i < 3; i++)
        {
            float y = .65f + i * .62f;
            Part(root, "Bolt guide", new Vector3(1.08f, y, -.16f), new Vector3(.2f, .15f, .14f), dark);
            owner.bolts[i] = Part(root, "Sliding locking bolt", new Vector3(1.24f, y, -.16f), new Vector3(.28f, .095f, .09f), steel).transform;
        }
        Part(root, "Mechanical status plate", new Vector3(0, 2.23f, -.18f), new Vector3(1.75f, .42f, .035f), dark);
        owner.status = Text(root, "CUSTODIAN KEY REQUIRED\nSECURITY OFFICE / KEY CUSTODY\nINSERT STRAIGHT / BOW UPRIGHT", new Vector3(0, 2.23f, -.204f), new Vector2(1.58f, .34f), .065f, Color.white);
        owner.indicator = Part(root, "Safety indicator", new Vector3(-.94f, 2.23f, -.18f), new Vector3(.06f, .16f, .04f), kit.chipRed).GetComponent<Renderer>();
        owner.lockedMaterial = kit.chipRed;owner.armedMaterial = brass;owner.openMaterial = kit.chipGreen;
        Text(root, "CAPTIVE KEY / MECHANICAL INTERLOCK", new Vector3(0, .95f, -.2f), new Vector2(1.58f, .08f), .047f, new Color(.94f, .76f, .36f));
        Text(root, "SIMULATOR: NOTEBOOK F / TAKE KEY OUT\nLOOK AT LOCK: ENTER TO INSERT\nHOLD RIGHT ARROW TO TURN / LEFT TO REVERSE", new Vector3(0, .60f, -.20f), new Vector2(1.8f, .22f), .043f, Color.white);
        Save(root, "VaultKeyMechanism");
    }

    static VaultRotaryHandle Handle(Transform root, string name, Vector3 pos, VaultKeyLock owner, bool cylinder)
    {
        var t = Group(root, name, pos);t.gameObject.layer = layer;
        t.gameObject.AddComponent<XRSimpleInteractable>();
        var handle = t.gameObject.AddComponent<VaultRotaryHandle>();handle.owner = owner;handle.keyCylinder = cylinder;return handle;
    }
}
