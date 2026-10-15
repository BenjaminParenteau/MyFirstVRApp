using HighStakes.Inventory;
using HighStakes.TableGames;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class BarPuzzleBuilder
{
    const string PrefabPath = "Assets/Content/Progression/BarPuzzle.prefab";
    const string CasinoPath = "Assets/Scenes/MVP/MVP_Casino.unity";

    [MenuItem("High Stakes/Puzzles/Build Bar PIN Pad")]
    public static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        var kit = TableKit.Kit(out int layer);
        if (kit == null) return;
        var root = new GameObject("Bar_PinPuzzle");
        var panel = new GameObject("TiltedKeypad").transform;
        panel.SetParent(root.transform, false);
        panel.localPosition = new Vector3(0,.025f,0);
        panel.localRotation = Quaternion.Euler(20,0,0);
        Color cream = new Color32(232,228,216,255);
        Color cyan = new Color32(56,214,232,255);
        Part("Base",kit.woodDark,root.transform,new Vector3(0,.015f,.055f),new Vector3(.44f,.03f,.27f));
        Part("BrassCase",kit.brassGold,panel,new Vector3(0,.25f,0),new Vector3(.42f,.50f,.05f));
        Part("BlackFace",kit.chipBlack,panel,new Vector3(0,.25f,-.028f),new Vector3(.394f,.475f,.008f));
        Label(panel,"Heading","BAR FLOAT",0,.463f,-.035f,.35f,.034f,1.55f,cream);
        Part("ScreenRim",kit.brassGold,panel,new Vector3(0,.383f,-.035f),new Vector3(.35f,.108f,.008f));
        Part("Screen",kit.chipBlack,panel,new Vector3(0,.383f,-.041f),new Vector3(.34f,.10f,.006f));
        var display=Label(panel,"PIN","_ _ _ _",0,.4f,-.046f,.31f,.052f,2.6f,cyan);
        var status=Label(panel,"Status","ENTER 4-DIGIT PIN",0,.36f,-.046f,.31f,.022f,.92f,cream);
        Label(panel,"Reserve","RESERVE  /  $500",0,.307f,-.035f,.34f,.025f,1.1f,cream);
        var keys=new PressableButton[12];
        for(int i=0;i<12;i++)
        {
            float x=(i%3-1)*.119f, y=.254f-(i/3)*.069f;
            string text=i<9?(i+1).ToString():i==9?"CLR":i==10?"0":"OK";
            var go=new GameObject("Key_"+text) { layer=layer };
            go.transform.SetParent(panel,false); go.transform.localPosition=new Vector3(x,y,-.052f);
            go.AddComponent<BoxCollider>().size=new Vector3(.105f,.058f,.025f);
            keys[i]=go.AddComponent<PressableButton>();
            // Use a very short dip: keypad is upright rather than a flat table button.
            TableKit.Set(keys[i],"pressDip",p=>p.floatValue=.002f);
            Part("Keycap",i==11?kit.brassGold:kit.chipWhite,go.transform,Vector3.zero,new Vector3(.105f,.058f,.025f));
            Label(go.transform,"Number",text,0,0,-.014f,.10f,.052f,i<9||i==10?2.45f:1.55f,Color.black);
        }
        // A small closeout slip is the only clue. Its instruction reverses the listed counts.
        var note=new GameObject("CloseoutSlip").transform;
        note.SetParent(root.transform,false); note.localPosition=new Vector3(-.355f,.035f,-.015f);
        note.localRotation=Quaternion.Euler(35,0,-5);
        Part("Paper",kit.chipWhite,note,new Vector3(0,.125f,0),new Vector3(.245f,.25f,.004f));
        Label(note,"NoteTitle","CLOSEOUT",0,.225f,-.004f,.22f,.03f,1.4f,Color.black);
        Label(note,"Counts","2  empty racks\n4  sealed rolls\n6  loose chips\n8  paid tabs",0,.137f,-.004f,.22f,.125f,1.25f,Color.black);
        Label(note,"Clue","PIN: last count first.",0,.043f,-.004f,.225f,.034f,1.05f,Color.black);
        var puzzle=root.AddComponent<BarPuzzle>();
        TableKit.SetRefs(puzzle,"keys",keys);
        TableKit.SetRef(puzzle,"display",display); TableKit.SetRef(puzzle,"status",status);
        var prefab=TableKit.SavePrefab(root,PrefabPath);
        var scene=SceneManager.GetSceneByPath(CasinoPath);
        bool opened=!scene.isLoaded;
        if(opened) scene=EditorSceneManager.OpenScene(CasinoPath,OpenSceneMode.Additive);
        foreach(var obj in scene.GetRootGameObjects())
            if(obj.name=="Bar_NightcapPuzzle" || obj.name=="Bar_PinPuzzle" || PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(obj)==PrefabPath)
                Object.DestroyImmediate(obj);
        var instance=(GameObject)PrefabUtility.InstantiatePrefab(prefab,scene);
        instance.name="Bar_PinPuzzle";
        // Front edge of the bar, within reach and angled toward a standing player.
        instance.transform.position=new Vector3(-4.35f,1.105f,8.57f);
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        if(opened) EditorSceneManager.CloseScene(scene,true);
        AssetDatabase.SaveAssets();
        Debug.Log("[BarPuzzle] Compact PIN pad installed in MVP_Casino.");
    }

    static void Part(string name,Material mat,Transform parent,Vector3 pos,Vector3 scale) =>
        TableKit.Part(mat,parent,name,PrimitiveType.Cube,pos,scale,false,false);
    static TextMeshPro Label(Transform parent,string name,string text,float x,float y,float z,float w,float h,float size,Color color)
    {
        var label=TableKit.Text(parent,name,new Vector3(x,y,z),Quaternion.identity,new Vector2(w,h),.1f,size,color);
        label.text=text;
        return label;
    }
}
