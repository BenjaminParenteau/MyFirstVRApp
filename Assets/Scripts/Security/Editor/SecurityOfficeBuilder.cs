using System;
using System.Collections.Generic;
using System.IO;
using HighStakes.Environment;
using HighStakes.Security;
using HighStakes.TableGames;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

/// <summary>Additive furnishing pass: preserves the security office shell, doors, rig and lighting.</summary>
public static class SecurityOfficeBuilder
{
    public const string ScenePath = "Assets/Scenes/MVP/MVP_SecurityOffice.unity";
    public const string Folder = "Assets/Content/Progression/SecurityOffice";
    const string RootName = "SecurityOffice_Operations";
    static SharedStyleKit kit;
    static Material dark, steel, cyan, paper, green, feedMaterial;

    [MenuItem("High Stakes/Security/Build Furnished Security Office")]
    public static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        BuildOffice();
    }

    public static void BuildOffice()
    {
        Directory.CreateDirectory(Folder);
        AssetDatabase.Refresh();
        kit = AssetDatabase.LoadAssetAtPath<SharedStyleKit>("Assets/Content/StyleKit/SharedStyleKit.asset");
        if (!kit) throw new InvalidOperationException("Shared style kit is missing.");
        dark = Variant("M_Equipment", kit.chipBlack, new Color(.035f,.052f,.067f));
        steel = Variant("M_BlueSteel", kit.steelCool, new Color(.19f,.27f,.32f));
        cyan = kit.screenGlow; paper = kit.keycardPlastic; green = kit.chipGreen;
        feedMaterial = Variant("M_CCTV", kit.screenGlow, Color.black);
        feedMaterial.EnableKeyword("_EMISSION");
        feedMaterial.SetColor("_EmissionColor", Color.white * 2.5f);
        feedMaterial.SetFloat("_Smoothness", 0f);

        var scene = EditorSceneManager.GetSceneByPath(ScenePath);
        if (!scene.isLoaded) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
        SceneManager.SetActiveScene(scene);
        Transform room = null;
        foreach (var root in scene.GetRootGameObjects())
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                if (t.name == "RoomA (Security Office)") room = t;
        if (!room) throw new InvalidOperationException("Security office room anchor not found.");
        RenderSettings.ambientMode=AmbientMode.Flat;
        RenderSettings.ambientLight=new Color(.3f,.36f,.44f);
        foreach(var renderer in room.GetComponentsInChildren<MeshRenderer>())
            if(renderer.name.StartsWith("Kit_CeilingTile"))
            {
                renderer.sharedMaterial=kit.concreteCool;
                PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
            }
        var existing = room.Find(RootName);
        if (existing) Object.DestroyImmediate(existing.gameObject);
        // Remove only the original placeholder furniture; leave architecture and baked lights alone.
        var remove = new List<GameObject>();
        foreach (Transform t in room)
            if (t.name == "Screen" || t.name.StartsWith("Kit_Table_Round") || t.name.StartsWith("Kit_Chair")) remove.Add(t.gameObject);
        foreach (var go in remove) Object.DestroyImmediate(go);
        var content = new GameObject(RootName).transform;
        content.SetParent(room, false);

        // Rear wall: six 16:9 monitors, steel recess, ceiling cable tray and cyan header.
        Box(content,"Monitor wall backing",new Vector3(3,1.85f,6.68f),new Vector3(5.9f,2.05f,.16f),steel);
        Text(content,"MERIDIAN / SECURITY OPERATIONS",new Vector3(3,2.87f,5.97f),new Vector2(5.7f,.12f),.12f,kit.accentCyan);
        var wall = content.gameObject.AddComponent<SecurityMonitorWall>();
        wall.screenMaterial = feedMaterial;
        wall.feeds = new SecurityMonitorWall.Feed[6];
        string[] names = {"01 / CASINO FLOOR", "02 / VIP LOUNGE", "03 / STAFF HALL", "04 / VAULT APPROACH", "05 / CASHIER", "06 / VIP TABLES"};
        string[] scenes = {"MVP_Casino","MVP_VIP","MVP_SecurityHall","MVP_Vault","MVP_Casino","MVP_VIP"};
        Vector3[] positions = {new Vector3(-7,2.65f,-6), new Vector3(-4,2.65f,10),new Vector3(-.65f,2.65f,2),new Vector3(40.3f,2.65f,-.1f),new Vector3(8,2.65f,6.5f),new Vector3(5,2.65f,19)};
        Vector3[] targets = {new Vector3(0,.9f,4), new Vector3(2,.8f,17),new Vector3(1.5f,1.1f,9),new Vector3(43,1.1f,6),new Vector3(6.7f,1,10.3f),new Vector3(0,.8f,14)};
        for (int i=0;i<6;i++)
        {
            float x=1.1f+(i%3)*1.9f, y=i<3?2.24f:1.21f;
            Box(content,"Monitor "+(i+1)+" bezel",new Vector3(x,y,6.53f),new Vector3(1.79f,1f,.12f),dark);
            var screen=GameObject.CreatePrimitive(PrimitiveType.Quad);
            screen.name="CCTV "+names[i]; screen.transform.SetParent(content,false);
            screen.transform.localPosition=new Vector3(x,y+.035f,6.46f);
            screen.transform.localScale=new Vector3(1.68f,.945f,1);
            Object.DestroyImmediate(screen.GetComponent<Collider>());
            screen.GetComponent<Renderer>().sharedMaterial=feedMaterial;
            var caption=Text(content,names[i]+"  /  LIVE",new Vector3(x,y+.39f,6.42f),new Vector2(1.59f,.12f),.068f,Color.white);
            Box(content,"Online indicator",new Vector3(x-.8f,y-.39f,6.43f),new Vector3(.025f,.025f,.015f),green);
            wall.feeds[i]=new SecurityMonitorWall.Feed {label=names[i],sceneName=scenes[i],position=positions[i],lookAt=targets[i],screen=screen.GetComponent<Renderer>(),caption=caption};
        }
        // Operator console: two workstations; a clear centre aisle leads all the way back to the portal.
        for(int i=0;i<2;i++)
        {
            float x=1.55f+i*2.9f;
            Desk(content,x,5.65f);
            Chair(content,new Vector3(x,0,4.35f));
            Keyboard(content,new Vector3(x,.79f,5.36f));
            Box(content,"Mouse mat",new Vector3(x+.67f,.77f,5.33f),new Vector3(.31f,.015f,.3f),dark);
            Box(content,"Mouse",new Vector3(x+.67f,.8f,5.33f),new Vector3(.065f,.045f,.1f),steel);
            Box(content,"Dispatch panel",new Vector3(x-.73f,.81f,5.6f),new Vector3(.34f,.12f,.27f),dark);
            for(int k=0;k<3;k++) Box(content,"Radio channel",new Vector3(x-.82f+k*.09f,.88f,5.54f),new Vector3(.04f,.012f,.04f),k==0?green:steel);
            Box(content,"Radio antenna",new Vector3(x-.82f,1.02f,5.66f),new Vector3(.015f,.33f,.015f),dark);
        }
        var hold=Box(content,"HOLD LIVE FEEDS button",new Vector3(3,.84f,5.3f),new Vector3(.4f,.12f,.28f),cyan,true);
        var press=hold.AddComponent<PressableButton>();
        UnityEventTools.AddPersistentListener(press.Pressed,wall.TogglePause);
        Text(content,"LIVE / HOLD",new Vector3(3,.93f,5.1f),new Vector2(.65f,.15f),.075f,Color.white);
        // Right wall equipment racks with individual servers, ventilation, patch sockets and cable routing.
        for(int rack=0;rack<2;rack++)
        {
            float z=4.8f-rack*1.25f;
            Box(content,"Equipment rack",new Vector3(6.36f,1.08f,z),new Vector3(.85f,2.16f,1f),dark,true);
            for(int u=0;u<8;u++)
            {
                float y=.24f+u*.23f;
                Box(content,"Recorder / network unit",new Vector3(5.91f,y,z),new Vector3(.035f,.18f,.89f),steel);
                for(int v=0;v<5;v++) Box(content,"Rack vent",new Vector3(5.885f,y,z-.29f+v*.13f),new Vector3(.015f,.025f,.075f),dark);
                Box(content,"Status LED",new Vector3(5.88f,y+.045f,z+.36f),new Vector3(.015f,.025f,.025f),u%3==0?cyan:green);
            }
        }
        // Left: staff storage and an evidence/incident desk.
        for(int i=0;i<3;i++)
        {
            float z=.2f+i*.67f;
            Box(content,"Staff locker "+(i+1),new Vector3(-.48f,1,z),new Vector3(.75f,2,.62f),steel,true);
            Box(content,"Locker inset",new Vector3(-.085f,1,z),new Vector3(.025f,1.86f,.53f),dark);
            Box(content,"Locker handle",new Vector3(-.04f,1.05f,z+.15f),new Vector3(.05f,.16f,.025f),kit.steelCool);
            for(int v=0;v<4;v++) Box(content,"Locker vent",new Vector3(-.06f,1.62f+v*.055f,z),new Vector3(.025f,.018f,.3f),steel);
        }
        var side=new GameObject("Incident desk").transform; side.SetParent(content,false);
        side.localPosition=new Vector3(-.05f,0,3.6f); side.localRotation=Quaternion.Euler(0,-90,0);
        Desk(side,0,0,2.2f);
        Keyboard(side,new Vector3(0,.79f,-.1f));
        Box(side,"Terminal base",new Vector3(0,.79f,.25f),new Vector3(.36f,.04f,.22f),dark);
        Box(side,"Terminal stem",new Vector3(0,1.02f,.3f),new Vector3(.065f,.44f,.065f),steel);
        Box(side,"Terminal bezel",new Vector3(0,1.27f,.32f),new Vector3(.77f,.48f,.06f),dark);
        Text(side,"MERIDIAN / DISPATCH\n\nCCTV NETWORK ONLINE\nNIGHT SHIFT / LOG STATION",new Vector3(0,1.27f,.23f),new Vector2(.69f,.4f),.046f,kit.accentCyan);
        Box(side,"Report clipboard",new Vector3(.72f,.79f,-.08f),new Vector3(.29f,.03f,.39f),dark);
        Box(side,"Incident report",new Vector3(.72f,.809f,-.08f),new Vector3(.25f,.006f,.34f),paper);
        Box(side,"Desk phone base",new Vector3(-.7f,.83f,.09f),new Vector3(.3f,.15f,.27f),dark);
        Box(side,"Telephone handset",new Vector3(-.7f,.93f,.09f),new Vector3(.36f,.06f,.075f),steel);
        // Shift board faces the room from the right wall; grounded narrative dressing, no puzzle state.
        var board=new GameObject("Duty board").transform; board.SetParent(content,false);
        board.localPosition=new Vector3(6.8f,1.85f,1.2f); board.localRotation=Quaternion.Euler(0,90,0);
        Box(board,"Noticeboard frame",Vector3.zero,new Vector3(2.5f,1.25f,.08f),steel);
        Box(board,"Noticeboard face",new Vector3(0,0,-.05f),new Vector3(2.35f,1.1f,.025f),dark);
        Text(board,"NIGHT SHIFT / OPERATIONS",new Vector3(0,.38f,-.075f),new Vector2(2.2f,.2f),.12f,kit.accentCyan);
        Text(board,"01   CASINO & CASHIER\n02   VIP ACCESS CHECKS\n03   STAFF HALL / VAULT\n\nLOG INCIDENTS BEFORE HANDOVER",new Vector3(0,-.09f,-.075f),new Vector2(2.1f,.7f),.1f,Color.white);
        // Storage cabinet and evidence trays at the front, leaving the 2 m door opening unobstructed.
        Box(content,"Evidence cabinet",new Vector3(5.65f,.58f,-.35f),new Vector3(1.6f,1.16f,.7f),steel,true);
        for(int d=0;d<3;d++)
        {
            Box(content,"File drawer",new Vector3(5.65f,.22f+d*.35f,.018f),new Vector3(1.48f,.29f,.025f),dark);
            Box(content,"Drawer pull",new Vector3(5.65f,.22f+d*.35f,.05f),new Vector3(.25f,.035f,.04f),kit.steelCool);
        }
        for(int i=0;i<3;i++) Box(content,"Evidence archive box",new Vector3(5.13f+i*.5f,1.32f,-.35f),new Vector3(.44f,.3f,.5f),paper);
        Box(content,"Ceiling cable tray",new Vector3(3,2.87f,6.15f),new Vector3(6.8f,.1f,.3f),steel);
        Box(content,"Cable riser",new Vector3(6.53f,1.55f,6.35f),new Vector3(.12f,2.5f,.12f),dark);
        // Purely emissive strip fixtures: no additional realtime lights.
        for(int i=0;i<3;i++)
        {
            Box(content,"Fluorescent housing",new Vector3(1+i*2,2.87f,3),new Vector3(.25f,.09f,2.2f),steel);
            Box(content,"Cool diffuser",new Vector3(1+i*2,2.815f,3),new Vector3(.17f,.025f,2.05f),cyan);
        }
        CombineDetails(content);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Selection.activeGameObject=content.gameObject;
        SceneView.lastActiveSceneView?.LookAt(room.position+new Vector3(3,1.4f,3),Quaternion.Euler(18,0,0),7);
        Debug.Log("[SecurityOffice] Furnished office saved. Six live cameras observe four isolated remote scenes, with live patrol animation and dormant interactions.");
    }

    static Material Variant(string name,Material parent,Color color)
    {
        string path=Folder+"/"+name+".mat";
        var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!m) { m=new Material(parent) { name=name, parent=parent }; AssetDatabase.CreateAsset(m,path); }
        m.SetColor("_BaseColor",color);
        EditorUtility.SetDirty(m); return m;
    }

    [MenuItem("High Stakes/Security/Use Last Play Check as Editor Screen Previews")]
    public static void InstallPreviews()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode) return;
        var wall=Object.FindFirstObjectByType<SecurityMonitorWall>();
        if(!wall) throw new InvalidOperationException("Open the security office first.");
        foreach(var text in wall.GetComponentsInChildren<TMP_Text>())
        {
            if(text.text=="LIVE / HOLD") PlaceText(text,new Vector3(3,.93f,5.1f));
            if(text.text.StartsWith("MERIDIAN / DISPATCH")) PlaceText(text,new Vector3(0,1.27f,.23f));
            if(text.text=="MERIDIAN / SECURITY OPERATIONS")
            {
                PlaceText(text,new Vector3(3,2.87f,5.97f));
                text.rectTransform.sizeDelta=new Vector2(5.7f,.12f);
            }
            FitText(text);
            text.fontSharedMaterial=FontMaterial(text.fontSharedMaterial);
            EditorUtility.SetDirty(text);
        }
        wall.screenMaterial.SetColor("_EmissionColor",Color.white*2.5f);
        EditorUtility.SetDirty(wall.screenMaterial);
        foreach(var f in wall.feeds)
        {
            var pos=f.screen.transform.localPosition;
            pos.y=f.screen.transform.localPosition.y+.355f;
            pos.z=6.42f;
            PlaceText(f.caption,pos);
        }
        for(int i=0;i<wall.feeds.Length;i++)
        {
            string source="Temp/SecurityOfficeQA/feed-"+(i+1)+".png";
            if(!File.Exists(source)) continue;
            string path=Folder+"/CCTV_Preview_"+(i+1)+".png";
            File.Copy(source,path,true); AssetDatabase.ImportAsset(path);
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            importer.mipmapEnabled=false; importer.maxTextureSize=512;
            importer.textureCompression=TextureImporterCompression.Compressed;
            importer.SaveAndReimport();
            var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            string materialPath=Folder+"/M_CCTV_Preview_"+(i+1)+".mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if(!material)
            {
                material=new Material(wall.screenMaterial) { parent=wall.screenMaterial };
                AssetDatabase.CreateAsset(material,materialPath);
            }
            material.SetTexture("_EmissionMap",texture);
            wall.feeds[i].screen.sharedMaterial=material;
            EditorUtility.SetDirty(material);
        }
        EditorSceneManager.MarkSceneDirty(wall.gameObject.scene);
        EditorSceneManager.SaveScene(wall.gameObject.scene);
        AssetDatabase.SaveAssets();
        var view=SceneView.lastActiveSceneView;
        if(view) view.LookAt(new Vector3(43,1.65f,4.4f),Quaternion.Euler(8,0,0),2.5f);
        // The stock test rig uses built-in Default-Line ray materials; validate authored room content.
        var solo=Array.Find(wall.gameObject.scene.GetRootGameObjects(),root=>root.name=="_SoloTest");
        bool wasActive=solo && solo.activeSelf;
        try { if(wasActive) solo.SetActive(false); StyleKitBuilder.ValidateOpenScene(); }
        finally { if(wasActive) solo.SetActive(true); }
    }

    static Material FontMaterial(Material source)
    {
        string path=Folder+"/M_OfficeLabels.mat";
        var material=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!material)
        {
            material=new Material(source) {parent=source};
            AssetDatabase.CreateAsset(material,path);
        }
        return material;
    }

    static GameObject Box(Transform parent,string name,Vector3 pos,Vector3 size,Material material,bool solid=false)
    {
        var go=GameObject.CreatePrimitive(PrimitiveType.Cube); go.name=name;
        go.transform.SetParent(parent,false); go.transform.localPosition=pos; go.transform.localScale=size;
        go.GetComponent<Renderer>().sharedMaterial=material;
        if(!solid) Object.DestroyImmediate(go.GetComponent<Collider>());
        go.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;
        return go;
    }

    static TMP_Text Text(Transform parent,string value,Vector3 pos,Vector2 size,float height,Color color)
    {
        var text=new GameObject(value.Split('\n')[0]).AddComponent<TextMeshPro>();
        text.transform.SetParent(parent,false); PlaceText(text,pos);
        text.rectTransform.sizeDelta=size; text.text=value; text.fontSize=height*10;
        text.fontSharedMaterial=FontMaterial(text.fontSharedMaterial);
        text.alignment=TextAlignmentOptions.Center; text.color=color;
        FitText(text);
        return text;
    }

    static void FitText(TMP_Text text)
    {
        text.enableAutoSizing=true;
        text.fontSizeMax=text.fontSize;
        text.fontSizeMin=.15f;
        text.textWrappingMode=text.text.Contains("\n")?TextWrappingModes.Normal:TextWrappingModes.NoWrap;
        text.margin=new Vector4(.015f,.008f,.015f,.008f);
    }

    static void PlaceText(TMP_Text text,Vector3 position)
    {
        text.rectTransform.anchoredPosition3D=position;
        EditorUtility.SetDirty(text.rectTransform);
    }

    static void Desk(Transform parent,float x,float z,float width=2.55f)
    {
        Box(parent,"Console desktop",new Vector3(x,.72f,z),new Vector3(width,.06f,.9f),steel,true);
        Box(parent,"Desk front edge",new Vector3(x,.72f,z-.46f),new Vector3(width,.085f,.045f),dark);
        for(int i=-1;i<=1;i+=2) Box(parent,"Desk pedestal",new Vector3(x+i*(width/2-.23f),.35f,z+.02f),new Vector3(.4f,.7f,.7f),dark,true);
    }

    static void Chair(Transform parent,Vector3 pos)
    {
        var chair=new GameObject("Operator chair").transform; chair.SetParent(parent,false); chair.localPosition=pos;
        Box(chair,"Seat",new Vector3(0,.45f,0),new Vector3(.53f,.12f,.5f),dark,true);
        Box(chair,"Backrest",new Vector3(0,.82f,-.22f),new Vector3(.51f,.62f,.1f),dark,true);
        Box(chair,"Stem",new Vector3(0,.22f,0),new Vector3(.07f,.4f,.07f),kit.steelCool);
        Box(chair,"Base X",new Vector3(0,.06f,0),new Vector3(.65f,.07f,.08f),steel);
        Box(chair,"Base Z",new Vector3(0,.06f,0),new Vector3(.08f,.07f,.65f),steel);
        for(int i=-1;i<=1;i+=2) Box(chair,"Armrest",new Vector3(i*.31f,.65f,0),new Vector3(.055f,.055f,.38f),steel);
    }

    static void Keyboard(Transform parent,Vector3 pos)
    {
        Box(parent,"Keyboard",pos,new Vector3(.57f,.035f,.2f),dark);
        for(int row=0;row<3;row++)
            for(int key=0;key<11;key++) Box(parent,"Key",pos+new Vector3(-.24f+key*.048f,.024f,-.065f+row*.05f),new Vector3(.034f,.012f,.032f),steel);
        Box(parent,"Space bar",pos+new Vector3(0,.024f,.075f),new Vector3(.25f,.012f,.025f),steel);
    }

    static void CombineDetails(Transform root)
    {
        var groups=new Dictionary<Material,List<CombineInstance>>();
        var originals=new List<MeshRenderer>();
        foreach(var renderer in root.GetComponentsInChildren<MeshRenderer>())
        {
            if(renderer.GetComponent<TMP_Text>() || renderer.GetComponent<PressableButton>() || renderer.name.StartsWith("CCTV ")) continue;
            var filter=renderer.GetComponent<MeshFilter>();
            if(!filter || renderer.sharedMaterials.Length!=1) continue;
            var material=renderer.sharedMaterial;
            if(!groups.TryGetValue(material,out var list)) groups[material]=list=new List<CombineInstance>();
            list.Add(new CombineInstance {mesh=filter.sharedMesh,transform=root.worldToLocalMatrix*filter.transform.localToWorldMatrix});
            originals.Add(renderer);
        }
        foreach(var pair in groups)
        {
            var mesh=new Mesh {name="Office_"+pair.Key.name,indexFormat=IndexFormat.UInt32};
            mesh.CombineMeshes(pair.Value.ToArray());
            Unwrapping.GenerateSecondaryUVSet(mesh);
            string path=Folder+"/"+mesh.name+".asset";
            var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(saved) { EditorUtility.CopySerialized(mesh,saved); Object.DestroyImmediate(mesh); mesh=saved; }
            else AssetDatabase.CreateAsset(mesh,path);
            var go=new GameObject(mesh.name); go.transform.SetParent(root,false);
            GameObjectUtility.SetStaticEditorFlags(go,StaticEditorFlags.ContributeGI | StaticEditorFlags.BatchingStatic);
            go.AddComponent<MeshFilter>().sharedMesh=mesh;
            var renderer=go.AddComponent<MeshRenderer>(); renderer.sharedMaterial=pair.Key; renderer.shadowCastingMode=ShadowCastingMode.Off;
        }
        foreach(var renderer in originals) { Object.DestroyImmediate(renderer.GetComponent<MeshFilter>()); Object.DestroyImmediate(renderer); }
    }
}
