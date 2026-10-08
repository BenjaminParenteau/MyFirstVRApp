using System.Linq;
using HighStakes.Security;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Builds the Pit Boss NPC from the Microsoft Rocketbox avatar Business_Male_04 (MIT, see LICENSE-Rocketbox.md) in
/// Assets/Content/Characters/PitBoss: URP materials, import settings, a look-around / walk Animator, the NPC_PitBoss
/// prefab, and his patrol loop on the MVP_Casino floor. Rocketbox's own FixRocketboxMaxImport postprocessor is not
/// used because it rewrites every material and normal map in the project; its fixes are applied here to these files.
/// </summary>
public static class PitBossBuilder
{
    const string Folder = "Assets/Content/Characters/PitBoss";
    const string ModelPath = Folder + "/Business_Male_04.fbx";
    const string WalkPath = Folder + "/Animations/m_walk_slow_01.fbx";
    const string LookPath = Folder + "/Animations/m_idle_look_around_01.fbx";
    const string ControllerPath = Folder + "/PitBoss.controller";
    const string PrefabPath = Folder + "/NPC_PitBoss.prefab";
    const string CasinoPath = "Assets/Scenes/MVP/MVP_Casino.unity";

    // A loop of the open floor: up the centre aisle, across past the bar end, down the aisle between the left tables
    // and the slot bank, back across the entrance end. He pauses at each corner to look around.
    static readonly Vector3[] Patrol =
    {
        new Vector3(0f, 0f, -5f),
        new Vector3(0f, 0f, 6f),
        new Vector3(-6f, 0f, 6f),
        new Vector3(-6f, 0f, -5f),
    };

    [MenuItem("High Stakes/Characters/Build Pit Boss")]
    public static void Build()
    {
        var body = Material("PitBoss_Body", "m015_body");
        var head = Material("PitBoss_Head", "m015_head");

        var model = (ModelImporter)AssetImporter.GetAtPath(ModelPath);
        model.animationType = ModelImporterAnimationType.Generic;
        model.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        model.importAnimation = false;
        model.importBlendShapes = false;
        model.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
        model.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), "m015_body"), body);
        model.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), "m015_head"), head);
        model.SaveAndReimport();
        var avatar = AssetDatabase.LoadAllAssetsAtPath(ModelPath).OfType<Avatar>().First();

        var walk = Clip(WalkPath, "PitBoss_Walk", avatar);
        var look = Clip(LookPath, "PitBoss_LookAround", avatar);

        var controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        controller.AddParameter("Walking", AnimatorControllerParameterType.Bool);
        var machine = controller.layers[0].stateMachine;
        var lookState = machine.AddState("LookAround");
        lookState.motion = look;
        var walkState = machine.AddState("Walk");
        walkState.motion = walk;
        machine.defaultState = lookState;
        Transition(lookState, walkState, true);
        Transition(walkState, lookState, false);

        var root = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath));
        PrefabUtility.UnpackPrefabInstance(root, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        root.name = "NPC_PitBoss";
        var animator = root.GetComponent<Animator>();
        if (animator == null) animator = root.AddComponent<Animator>();
        animator.runtimeAnimatorController = controller;
        animator.avatar = avatar;
        animator.applyRootMotion = false; // the clips walk in place; PatrolWalker moves him
        animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms; // no skinning work while off-screen
        var capsule = root.AddComponent<CapsuleCollider>(); // the player bumps into him instead of walking through
        capsule.center = new Vector3(0f, 0.9f, 0f);
        capsule.height = 1.8f;
        capsule.radius = 0.25f;
        var walker = root.AddComponent<PatrolWalker>();
        var so = new SerializedObject(walker);
        so.FindProperty("animator").objectReferenceValue = animator;
        so.FindProperty("walkSpeed").floatValue = StrideSpeed(walk, root);
        so.ApplyModifiedPropertiesWithoutUndo();
        var prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);

        PlaceInCasino(prefab);
        AssetDatabase.SaveAssets();
        Debug.Log("[PitBoss] Built " + PrefabPath + " and placed him on the MVP_Casino floor.");
    }

    // URP Lit with the Rocketbox colour and normal maps (their 3ds Max export expects a white tint).
    static Material Material(string name, string textures)
    {
        string path = $"{Folder}/Materials/{name}.mat";
        if (!AssetDatabase.IsValidFolder(Folder + "/Materials")) AssetDatabase.CreateFolder(Folder, "Materials");
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(material, path);
        }

        string normalPath = $"{Folder}/Textures/{textures}_normal.png";
        var normalImporter = (TextureImporter)AssetImporter.GetAtPath(normalPath);
        normalImporter.textureType = TextureImporterType.NormalMap;
        normalImporter.maxTextureSize = 1024;
        normalImporter.SaveAndReimport();
        var colorImporter = (TextureImporter)AssetImporter.GetAtPath($"{Folder}/Textures/{textures}_color.png");
        colorImporter.maxTextureSize = 1024;
        colorImporter.SaveAndReimport();

        material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>($"{Folder}/Textures/{textures}_color.png"));
        material.SetColor("_BaseColor", Color.white);
        material.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath));
        material.EnableKeyword("_NORMALMAP");
        material.SetFloat("_Smoothness", 0.25f);
        EditorUtility.SetDirty(material);
        return material;
    }

    // Generic rig on the model's own skeleton (every Rocketbox avatar shares it), one looping clip per file.
    // The walk still carries the hips forward (1.2 m per 1.47 s loop) and would snap back each loop, so the hips
    // (Bip01) are the root node: their ground travel becomes root motion, which the Animator discards
    // (applyRootMotion off) while PatrolWalker moves him. Height bob and facing stay in the pose.
    static AnimationClip Clip(string path, string clipName, Avatar avatar)
    {
        var importer = (ModelImporter)AssetImporter.GetAtPath(path);
        importer.animationType = ModelImporterAnimationType.Generic;
        importer.avatarSetup = ModelImporterAvatarSetup.CopyFromOther;
        importer.sourceAvatar = avatar;
        importer.motionNodeName = "Bip01";
        importer.materialImportMode = ModelImporterMaterialImportMode.None;
        var clip = importer.defaultClipAnimations[0];
        clip.name = clipName;
        clip.loopTime = true;
        clip.lockRootRotation = true;      // bake facing into the pose
        clip.keepOriginalOrientation = true;
        clip.lockRootHeightY = true;       // bake the step bob into the pose
        clip.keepOriginalPositionY = true;
        clip.lockRootPositionXZ = false;   // extract ground travel (then dropped)
        importer.clipAnimations = new[] { clip };
        importer.SaveAndReimport();
        return AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().First(c => c.name == clipName);
    }

    /// <summary>
    /// The walk clip's own pace: how far the hips travel over one loop, before extraction, divided by its length.
    /// Walking the patrol at exactly this speed keeps the feet planted instead of sliding.
    /// </summary>
    static float StrideSpeed(AnimationClip walk, GameObject character)
    {
        var raw = AssetDatabase.LoadAllAssetsAtPath(WalkPath).OfType<AnimationClip>().First(c => !c.name.StartsWith("__preview"));
        var hips = character.GetComponentsInChildren<Transform>().First(t => t.name == "Bip01");
        var curves = AnimationUtility.GetCurveBindings(raw);
        var z = curves.FirstOrDefault(b => b.path == "Bip01" && b.propertyName == "m_LocalPosition.z");
        var x = curves.FirstOrDefault(b => b.path == "Bip01" && b.propertyName == "m_LocalPosition.x");
        float Travel(EditorCurveBinding b) => b.path == null ? 0f :
            AnimationUtility.GetEditorCurve(raw, b).Evaluate(raw.length) - AnimationUtility.GetEditorCurve(raw, b).Evaluate(0f);
        float distance = new Vector2(Travel(x), Travel(z)).magnitude * hips.parent.lossyScale.x;
        float speed = distance > 0.1f ? distance / walk.length : 0.8f;
        Debug.Log($"[PitBoss] walk clip travels {distance:F2} m per {walk.length:F2} s loop: patrol speed {speed:F2} m/s");
        return speed;
    }

    static void Transition(AnimatorState from, AnimatorState to, bool walking)
    {
        var t = from.AddTransition(to);
        t.hasExitTime = false;
        t.duration = 0.25f;
        t.AddCondition(walking ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0f, "Walking");
    }

    static void PlaceInCasino(GameObject prefab)
    {
        var scene = EditorSceneManager.OpenScene(CasinoPath, OpenSceneMode.Single);
        foreach (var old in scene.GetRootGameObjects().Where(g => g.name == "PitBoss_Patrol"))
            Object.DestroyImmediate(old);

        var patrol = new GameObject("PitBoss_Patrol").transform;
        var points = new Transform[Patrol.Length];
        for (int i = 0; i < Patrol.Length; i++)
        {
            points[i] = new GameObject("Waypoint" + i).transform;
            points[i].SetParent(patrol, false);
            points[i].position = Patrol[i];
        }
        var boss = (GameObject)PrefabUtility.InstantiatePrefab(prefab, patrol);
        boss.transform.SetPositionAndRotation(Patrol[0], Quaternion.LookRotation(Patrol[1] - Patrol[0]));
        var so = new SerializedObject(boss.GetComponent<PatrolWalker>());
        var list = so.FindProperty("waypoints");
        list.arraySize = points.Length;
        for (int i = 0; i < points.Length; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = points[i];
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorSceneManager.SaveScene(scene);
    }
}
