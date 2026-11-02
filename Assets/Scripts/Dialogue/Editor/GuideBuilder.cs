using System.Diagnostics;
using HighStakes.Dialogue;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Debug = UnityEngine.Debug;

/// <summary>
/// Builds the Guide's voice: one GuideLine asset per line below, placeholder audio spoken by macOS `say`, and the
/// UI_GuideVoice prefab (GuideVoice + one GuideTrigger per line), added to MVP_UI and MVP_Main. The table below is the
/// source of the lines: edit it and re-run. A real recording replaces a WAV in Content/UI/Guide/Audio with the same
/// name. Slot C (Pak) owns this.
/// </summary>
public static class GuideBuilder
{
    const string Folder = "Assets/Content/UI/Guide";
    const string AudioFolder = Folder + "/Audio";
    const string PrefabPath = "Assets/Content/UI/UI_GuideVoice.prefab";
    const string UIScenePath = "Assets/Scenes/MVP/MVP_UI.unity";
    const string MainScenePath = "Assets/Scenes/MVP/MVP_Main.unity";

    // Placeholder voice: a calm British handler. Any `say -v '?'` voice works.
    const string Voice = "Daniel";
    const int WordsPerMinute = 170;

    struct Line
    {
        public string id, subtitle, lead;
        public GuideTrigger.When when;
        public int tier;
        public string item, scene;
    }

    static readonly Line[] Lines =
    {
        new Line { id = "guide_first_contact", when = GuideTrigger.When.FirstChipChange,
            subtitle = "Don't turn around. Keep playing. You can call me the Guide. This casino isn't what it looks like, and I need someone on the inside.",
            lead = "Keep playing the tables and build your chips." },
        new Line { id = "guide_tier_1", when = GuideTrigger.When.TierReached, tier = 1,
            subtitle = "They're starting to notice you. Good. A known high roller gets doors opened for them.",
            lead = "Talk to the Pit Boss about the VIP lounge." },
        new Line { id = "guide_tier_2", when = GuideTrigger.When.TierReached, tier = 2,
            subtitle = "Valued associate. The staff treat you like family now. Use it." },
        new Line { id = "guide_vip_card", when = GuideTrigger.When.ItemAdded, item = "vip_keycard",
            subtitle = "The Pit Boss's own card. The lounge is yours. Listen to what people say in there.",
            lead = "Use the VIP card on the lounge door and look around." },
        new Line { id = "guide_shift_note", when = GuideTrigger.When.ItemAdded, item = "shift_note",
            subtitle = "Cards, then the wheel, then dice. Those tables have brass number stands. Read them in that order.",
            lead = "Read the brass stands: cards, wheel, dice. That's the lockbox code." },
        new Line { id = "guide_staff_card", when = GuideTrigger.When.ItemAdded, item = "staff_keycard",
            subtitle = "Staff access. Behind that door is the real casino. Walk like you belong.",
            lead = "Go through the STAFF ONLY door." },
        new Line { id = "guide_back_of_house", when = GuideTrigger.When.AreaLoaded, scene = "MVP_SecurityHall",
            subtitle = "You're behind the curtain now. Cameras everywhere back here. Don't linger.",
            lead = "Search the security wing. Don't linger." },
    };

    [MenuItem("High Stakes/UI/Build Guide Voice")]
    public static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("[Guide] Stop Play mode first, then run Build Guide Voice again.");
            return;
        }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return; // adding to scenes replaces the open scene

        WristDisplayBuilder.EnsureFolder(AudioFolder);
        GenerateMissingAudio();
        var lines = BuildLines();
        BuildPrefab(lines);
        AssetDatabase.SaveAssets();

        AddToScene(UIScenePath, "UI_Content");
        AddToScene(MainScenePath, null);
        Debug.Log($"[Guide] Built {lines.Length} lines and {PrefabPath}. Press Play in MVP_UI and add chips to hear first contact.");
    }

    [MenuItem("High Stakes/UI/Regenerate Guide Voice (macOS)")]
    public static void RegenerateAudio()
    {
        WristDisplayBuilder.EnsureFolder(AudioFolder);
        foreach (var line in Lines)
            Speak(line);
        AssetDatabase.Refresh();
        BuildLines(); // re-link the clips
        AssetDatabase.SaveAssets();
    }

    // ---------------------------------------------------------------- audio

    static void GenerateMissingAudio()
    {
        bool made = false;
        foreach (var line in Lines)
        {
            if (!System.IO.File.Exists(AudioPath(line.id)))
                made |= Speak(line);
        }
        if (made) AssetDatabase.Refresh();
    }

    static bool Speak(Line line)
    {
        if (Application.platform != RuntimePlatform.OSXEditor)
        {
            Debug.LogWarning($"[Guide] No audio for '{line.id}': placeholder voices are made with macOS `say`. The line still shows as a subtitle.");
            return false;
        }

        string text = line.subtitle.Replace("\\", "\\\\").Replace("\"", "\\\"");
        var info = new ProcessStartInfo("/usr/bin/say",
            $"-v {Voice} -r {WordsPerMinute} --data-format=LEI16@22050 -o \"{AudioPath(line.id)}\" \"{text}\"")
        {
            UseShellExecute = false,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };
        using (var process = Process.Start(info))
        {
            string error = process.StandardError.ReadToEnd();
            process.WaitForExit();
            if (process.ExitCode != 0)
            {
                Debug.LogError($"[Guide] `say` failed for '{line.id}': {error}");
                return false;
            }
        }
        return true;
    }

    static string AudioPath(string id) => $"{AudioFolder}/VO_{id}.wav";

    // ---------------------------------------------------------------- assets

    static GuideLine[] BuildLines()
    {
        var assets = new GuideLine[Lines.Length];
        for (int i = 0; i < Lines.Length; i++)
        {
            string path = $"{Folder}/SO_GuideLine_{Lines[i].id}.asset";
            var asset = AssetDatabase.LoadAssetAtPath<GuideLine>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<GuideLine>();
                AssetDatabase.CreateAsset(asset, path);
            }
            asset.id = Lines[i].id;
            asset.subtitle = Lines[i].subtitle;
            asset.lead = Lines[i].lead ?? "";
            asset.clip = AssetDatabase.LoadAssetAtPath<AudioClip>(AudioPath(Lines[i].id));
            EditorUtility.SetDirty(asset);
            assets[i] = asset;
        }
        return assets;
    }

    static void BuildPrefab(GuideLine[] lines)
    {
        var root = new GameObject("UI_GuideVoice");
        var earpiece = root.AddComponent<AudioSource>();
        earpiece.playOnAwake = false;
        earpiece.spatialBlend = 0f; // in your ear, not somewhere in the room
        earpiece.priority = 0;      // never culled by casino sounds

        var voice = root.AddComponent<GuideVoice>();
        WristDisplayBuilder.Set(voice, "earpiece", earpiece);
        var so = new SerializedObject(voice);
        var array = so.FindProperty("lines");
        array.arraySize = lines.Length;
        for (int i = 0; i < lines.Length; i++)
            array.GetArrayElementAtIndex(i).objectReferenceValue = lines[i];
        so.ApplyModifiedPropertiesWithoutUndo();

        foreach (var line in Lines)
        {
            var go = new GameObject("Trigger_" + line.id);
            go.transform.SetParent(root.transform, false);
            var trigger = go.AddComponent<GuideTrigger>();
            var t = new SerializedObject(trigger);
            t.FindProperty("lineId").stringValue = line.id;
            t.FindProperty("when").enumValueIndex = (int)line.when;
            t.FindProperty("tierIndex").intValue = line.tier;
            t.FindProperty("itemId").stringValue = line.item ?? "";
            t.FindProperty("sceneName").stringValue = line.scene ?? "";
            t.FindProperty("voiceSource").objectReferenceValue = voice;
            t.ApplyModifiedPropertiesWithoutUndo();
        }

        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);
    }

    // ---------------------------------------------------------------- scenes

    // MVP_Main is generated by WorldBuilder (Slot D). Until it instantiates UI_GuideVoice too, this adds it;
    // Build MVP World would drop it again (same as the notebook).
    static void AddToScene(string scenePath, string parentName)
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath) == null)
        {
            Debug.LogWarning("[Guide] " + scenePath + " not found; Guide voice not added.");
            return;
        }

        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath); // load after opening, as in NotebookBuilder
        Transform parent = null;
        foreach (var root in scene.GetRootGameObjects())
        {
            if (root.GetComponentInChildren<GuideVoice>(true) != null)
                return; // already there
            if (root.name == parentName) parent = root.transform;
        }

        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
        if (parent != null) instance.transform.SetParent(parent, false);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }
}
