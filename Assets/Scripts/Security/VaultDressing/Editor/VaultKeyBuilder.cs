using System.IO;
using HighStakes.Core;
using HighStakes.Environment;
using HighStakes.Security;
using HighStakes.UI;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using Object = UnityEngine.Object;

/// <summary>Countertop key and simple mechanical lock; no source scene regeneration.</summary>
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
        try { Key(holder.transform);Mechanism(holder.transform);AssetDatabase.SaveAssets(); }
        finally {Object.DestroyImmediate(holder);EditorSceneManager.ClosePreviewScene(scene);}
        Debug.Log("[VaultKey] Built counter key and simple keyhole/handwheel interaction.");
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
        item.description = "A brass vault key found on the security-office counter. Use it at the vault keyhole, then turn the wheel.";
        EditorUtility.SetDirty(item);
        var pickup = root.gameObject.AddComponent<PickupItem>();
        var serialized = new SerializedObject(pickup);serialized.FindProperty("item").objectReferenceValue = item;serialized.ApplyModifiedPropertiesWithoutUndo();
        UnityEventTools.AddPersistentListener(pickup.Collected, key.Collect);
        // The thick bow, offset shoulder and asymmetric opposing wards make this a recognisable mechanical key.
        var bow = Group(root, "Oval brass bow", new Vector3(0, 0, -.045f));
        bow.localScale = new Vector3(.14f, .18f, .20f);bow.localRotation = Quaternion.Euler(90, 0, 0);
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
        Save(root, "VaultCustodianKey");
    }

    static void Mechanism(Transform parent)
    {
        var root = Group(parent, "VaultKeyMechanism", Vector3.zero);
        var owner = root.gameObject.AddComponent<VaultKeyLock>();
        Part(root, "Lever lock housing", new Vector3(.74f, 1.4f, -.21f), new Vector3(.36f, .43f, .16f), dark);
        var plate = Part(root, "Numbered cylinder plate", new Vector3(.74f, 1.4f, -.30f), new Vector3(.29f, .37f, .025f), brass);
        owner.cylinder = Group(root, "Quarter-turn cylinder", new Vector3(.74f, 1.38f, -.33f));
        Part(owner.cylinder, "Cylinder face", Vector3.zero, new Vector3(.095f, .015f, .095f), steel, PrimitiveType.Cylinder).transform.localRotation = Quaternion.Euler(90, 0, 0);
        Part(owner.cylinder, "Double-bit keyway", new Vector3(0, 0, -.018f), new Vector3(.035f, .013f, .006f), kit.chipBlack);
        owner.socket = Group(root, "Captive key seat", new Vector3(.74f, 1.38f, -.43f));
        owner.keyHandle = Handle(root, "Grip key / turn wrist", new Vector3(.74f, 1.38f, -.46f), owner, true);
        owner.keyHandle.gameObject.AddComponent<BoxCollider>().size = new Vector3(.32f, .32f, .3f);
        owner.wheelHandle = Handle(root, "Grip handwheel / sweep hand clockwise", new Vector3(0, 1.4f, -.36f), owner, false);
        owner.wheelHandle.gameObject.AddComponent<SphereCollider>().radius = .22f;
        for (int i = 0; i < 8; i++)
        {
            float a = i * Mathf.PI / 4;
            var collider = owner.wheelHandle.gameObject.AddComponent<SphereCollider>();
            collider.center = new Vector3(Mathf.Cos(a) * .3f, Mathf.Sin(a) * .3f, 0);collider.radius = .11f;
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
        owner.indicator = Part(root, "Safety indicator", new Vector3(.94f, 1.4f, -.32f), new Vector3(.06f, .16f, .04f), kit.chipRed).GetComponent<Renderer>();
        owner.lockedMaterial = kit.chipRed;owner.armedMaterial = brass;owner.openMaterial = kit.chipGreen;
        Save(root, "VaultKeyMechanism");
    }

    static VaultRotaryHandle Handle(Transform root, string name, Vector3 pos, VaultKeyLock owner, bool cylinder)
    {
        var t = Group(root, name, pos);t.gameObject.layer = layer;
        t.gameObject.AddComponent<XRSimpleInteractable>();
        var handle = t.gameObject.AddComponent<VaultRotaryHandle>();handle.owner = owner;handle.keyCylinder = cylinder;return handle;
    }
}
