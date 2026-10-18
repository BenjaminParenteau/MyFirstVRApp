using System.IO;
using HighStakes.Environment;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

/// <summary>Closes the entrance, preserving the casino and its baked lighting.</summary>
public static class CasinoEntranceBuilder
{
    const string ScenePath = "Assets/Scenes/MVP/MVP_Casino.unity";
    const string RootPath = "Assets/Content/Environment/Casino";
    const string PrefabPath = RootPath + "/Prefabs/Env_CasinoEntrance.prefab";

    [MenuItem("High Stakes/Environment/Build Casino Entrance")]
    public static void Build()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode) return;
        var kit=TableKit.Kit(out _);
        if(!kit) return;
        var wallpaper=AssetDatabase.LoadAssetAtPath<Material>(RootPath+"/Materials/M_Wallpaper.mat");
        var wood=AssetDatabase.LoadAssetAtPath<Material>(RootPath+"/Materials/M_WoodPanel.mat");
        var wallMat=Variant("M_EntranceWallpaper",wallpaper);
        wallMat.SetTextureScale("_BaseMap",new Vector2(16.3f,5.6f));
        var woodMat=Variant("M_EntranceWood",wood);
        woodMat.SetTextureScale("_BaseMap",new Vector2(8.15f,1.3f));
        var doorMat=Variant("M_EntranceDoorWood",wood);
        doorMat.SetTextureScale("_BaseMap",new Vector2(1.4f,2.5f));
        var facade=BuildingBrick(kit.concreteCool);
        var sky=Variant("M_EntranceNightSky",kit.screenGlow);
        sky.SetColor("_BaseColor",Color.black); sky.SetColor("_EmissionColor",new Color(.012f,.025f,.045f));
        var windows=Variant("M_EntranceWindows",kit.screenGlow);
        windows.SetColor("_BaseColor",Color.black); windows.SetColor("_EmissionColor",new Color(.85f,.49f,.18f));
        var glass=Variant("M_EntranceGlass",kit.steelCool);
        glass.SetColor("_BaseColor",new Color(.35f,.55f,.63f,.04f));
        glass.SetFloat("_BlendModePreserveSpecular",0);
        glass.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        glass.SetFloat("_Surface",1); glass.SetFloat("_Blend",0);
        glass.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);
        glass.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);
        glass.SetFloat("_ZWrite",0); glass.SetFloat("_Cull",0);
        glass.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        glass.SetOverrideTag("RenderType","Transparent"); glass.renderQueue=3000;
        glass.SetShaderPassEnabled("ShadowCaster",false);
        var root=new GameObject("Casino_Entrance");
        var t=root.transform;
        foreach(float side in new[]{-1f,1f})
        {
            Box("WallBelowWindows",wallMat,new Vector3(side*4.925f,.65f,0),new Vector3(8.15f,1.3f,.20f),true);
            Box("WallAboveWindows",wallMat,new Vector3(side*4.925f,3.95f,0),new Vector3(8.15f,.5f,.20f),true);
            for(int panel=0;panel<3;panel++)
            {
                float x=side*(.85f+(panel+.5f)*(8.15f/3f));
                Box("EntranceGlassPanel",glass,new Vector3(x,2.5f,0),new Vector3(8.15f/3f,2.4f,.008f),true);
            }
            for(int post=0;post<=3;post++)
            {
                float x=side*(.85f+post*(8.15f/3f));
                Box("WindowFrame",kit.woodDark,new Vector3(x,2.5f,.05f),new Vector3(.085f,2.45f,.15f));
                Box("WindowFrameInlay",kit.brassGold,new Vector3(x,2.5f,.13f),new Vector3(.018f,2.45f,.012f));
            }
            foreach(float y in new[]{1.32f,3.70f})
                Box("WindowRail",kit.brassGold,new Vector3(side*4.925f,y,.12f),new Vector3(8.15f,.05f,.04f));
            Box("WoodPaneling",woodMat,new Vector3(side*4.925f,.65f,.13f),new Vector3(8.15f,1.3f,.08f));
            Box("ChairRail",kit.brassGold,new Vector3(side*4.925f,1.32f,.19f),new Vector3(8.15f,.06f,.06f));
            Box("Skirting",kit.woodDark,new Vector3(side*4.925f,.08f,.19f),new Vector3(8.15f,.16f,.08f));
            Box("DoorJamb",kit.woodDark,new Vector3(side*.78f,1.3f,.08f),new Vector3(.16f,2.6f,.24f),true);
            Box("JambInlay",kit.brassGold,new Vector3(side*.78f,1.3f,.21f),new Vector3(.025f,2.6f,.012f));
        }
        Box("WallAboveDoor",wallMat,new Vector3(0,3.4f,0),new Vector3(1.7f,1.6f,.20f),true);
        Box("CrownMolding",kit.brassGold,new Vector3(0,3.85f,.16f),new Vector3(18,.18f,.12f));
        Box("DoorLintel",kit.woodDark,new Vector3(0,2.57f,.08f),new Vector3(1.72f,.16f,.24f),true);
        Box("LintelInlay",kit.brassGold,new Vector3(0,2.57f,.21f),new Vector3(1.7f,.025f,.012f));
        Box("Threshold",kit.brassGold,new Vector3(0,.015f,.04f),new Vector3(1.4f,.03f,.30f));
        // Four door pieces leave a real opening; the exterior has proper stereo parallax.
        Box("DoorLower",doorMat,new Vector3(0,.715f,0),new Vector3(1.4f,1.43f,.12f));
        Box("DoorUpper",doorMat,new Vector3(0,2.285f,0),new Vector3(1.4f,.43f,.12f));
        foreach(float side in new[]{-1f,1f})
        {
            Box("DoorSide",doorMat,new Vector3(side*.52f,1.75f,0),new Vector3(.36f,.64f,.12f));
            Box("WindowSideTrim",kit.brassGold,new Vector3(side*.35f,1.75f,.075f),new Vector3(.035f,.70f,.025f));
            Box("LowerDoorInlay",kit.brassGold,new Vector3(side*.55f,.70f,.068f),new Vector3(.015f,.98f,.009f));
        }
        foreach(float y in new[]{1.415f,2.085f})
            Box("WindowHorizontalTrim",kit.brassGold,new Vector3(0,y,.075f),new Vector3(.735f,.035f,.025f));
        Box("WindowGlass",glass,new Vector3(0,1.75f,0),new Vector3(.68f,.64f,.008f));
        Box("WindowMullion",kit.brassGold,new Vector3(0,1.75f,.075f),new Vector3(.018f,.64f,.018f));
        Box("HandlePlate",kit.brassGold,new Vector3(.55f,1.12f,.075f),new Vector3(.08f,.23f,.025f));
        Box("Handle",kit.brassGold,new Vector3(.47f,1.14f,.13f),new Vector3(.20f,.035f,.035f));
        var collision=new GameObject("ClosedDoorCollision"); collision.transform.SetParent(t,false);
        collision.transform.localPosition=new Vector3(0,1.25f,0);
        collision.AddComponent<BoxCollider>().size=new Vector3(1.4f,2.5f,.12f);
        Box("DoorPlaque",kit.chipBlack,new Vector3(0,2.32f,.07f),new Vector3(.70f,.13f,.015f));
        var title=TableKit.Text(t,"DoorName",new Vector3(0,2.32f,.081f),Quaternion.Euler(0,180,0),new Vector2(.65f,.10f),.1f,2f,new Color32(232,228,216,255));
        title.text="THE MERIDIAN";

        ExteriorDistrictBuilder.Build(t,kit,facade,windows,sky);
        var prefab=TableKit.SavePrefab(root,PrefabPath);
        var scene=SceneManager.GetSceneByPath(ScenePath); bool opened=!scene.isLoaded;
        if(opened) scene=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Additive);
        foreach(var obj in scene.GetRootGameObjects()) if(obj.name=="Casino_Entrance") Object.DestroyImmediate(obj);
        var instance=(GameObject)PrefabUtility.InstantiatePrefab(prefab,scene);
        instance.name="Casino_Entrance"; instance.transform.position=new Vector3(0,0,-9);
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        if(opened) EditorSceneManager.CloseScene(scene,true);
        AssetDatabase.SaveAssets(); Debug.Log("[CasinoEntrance] Built enclosed entrance and exterior window view.");

        void Box(string name,Material mat,Vector3 position,Vector3 size,bool collider=false)
        {
            var go=TableKit.Part(mat,t,name,PrimitiveType.Cube,position,size,false,collider);
            GameObjectUtility.SetStaticEditorFlags(go,StaticEditorFlags.BatchingStatic);
            go.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;
        }
    }

    static Material Variant(string name,Material parent)
    {
        string path=RootPath+"/Materials/"+name+".mat";
        var material=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!material) { material=new Material(parent); AssetDatabase.CreateAsset(material,path); }
        material.parent=parent; EditorUtility.SetDirty(material); return material;
    }
    static Material BuildingBrick(Material parent)
    {
        Texture2D Import(string name,bool normal)
        {
            string path=RootPath+"/Textures/"+name+".jpg";
            AssetDatabase.ImportAsset(path);
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType=normal?TextureImporterType.NormalMap:TextureImporterType.Default;
            importer.sRGBTexture=!normal; importer.mipmapEnabled=true; importer.maxTextureSize=1024;
            importer.wrapMode=TextureWrapMode.Repeat; importer.anisoLevel=2;
            var android=importer.GetPlatformTextureSettings("Android");
            android.overridden=true; android.maxTextureSize=1024; android.format=TextureImporterFormat.ASTC_6x6;
            importer.SetPlatformTextureSettings(android); importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
        var albedo=Import("T_ExteriorBrick_Albedo",false);
        var normalMap=Import("T_ExteriorBrick_Normal",true);
        var material=Variant("M_ExteriorBuildingBrick",parent);
        material.SetTexture("_BaseMap",albedo); material.SetTexture("_BumpMap",normalMap);
        material.SetTextureScale("_BaseMap",new Vector2(1.5f,3));
        material.SetColor("_BaseColor",new Color(.65f,.63f,.60f));
        material.SetFloat("_BumpScale",.65f); material.SetFloat("_Smoothness",.12f);
        material.EnableKeyword("_NORMALMAP");
        // A faint textured emission keeps the distant facade readable without extra lights.
        material.SetTexture("_EmissionMap",albedo);
        material.SetColor("_EmissionColor",new Color(.09f,.10f,.12f)); material.EnableKeyword("_EMISSION");
        return material;
    }

    [MenuItem("High Stakes/Environment/Verify Casino Entrance")]
    public static void Verify()
    {
        var root=GameObject.Find("Casino_Entrance");
        if(!root) throw new System.InvalidOperationException("Casino entrance is not loaded.");
        Physics.SyncTransforms();
        var colliders=root.GetComponentsInChildren<Collider>();
        int rays=0;
        foreach(float y in new[]{.25f,.75f,1.75f,2.55f,3.75f})
            for(float x=-8.75f;x<=8.75f;x+=.25f)
            {
                bool blocked=false;
                var ray=new Ray(new Vector3(x,y,-8),Vector3.back);
                foreach(var collider in colliders) if(collider.Raycast(ray,out _,2f)) { blocked=true; break; }
                if(!blocked) throw new System.InvalidOperationException($"Entrance collision gap at {x}, {y}.");
                rays++;
            }
        if(root.GetComponentsInChildren<Camera>().Length!=0 || root.GetComponentsInChildren<Light>().Length>2)
            throw new System.InvalidOperationException("Entrance added an unexpected camera or light.");
        if(!root.transform.Find("ExteriorDistrict")) throw new System.InvalidOperationException("3D street district missing.");
        int panels=0;
        foreach(var renderer in root.GetComponentsInChildren<Renderer>()) if(renderer.name=="EntranceGlassPanel") panels++;
        if(panels!=6) throw new System.InvalidOperationException("Expected six entrance glass panels.");
        if(!root.transform.Find("WindowGlass")) throw new System.InvalidOperationException("Door glass missing.");
        Directory.CreateDirectory("Temp/EntranceQA");
        File.WriteAllText("Temp/EntranceQA/verification.txt",$"PASS: {rays} collision rays cover the entrance, including door and jambs; door glass and six large glass panels present; 3D street district present; no extra cameras and at most two exterior lights.");
        Debug.Log("[CasinoEntrance] Verification passed.");
    }

    [MenuItem("High Stakes/Environment/Render Entrance Preview")]
    public static void Preview()
    {
        var hidden=new System.Collections.Generic.List<GameObject>();
        foreach(var obj in SceneManager.GetActiveScene().GetRootGameObjects())
            if(obj.activeSelf && (obj.name.Contains("PitBoss") || obj.name=="_SoloTest")) { hidden.Add(obj); obj.SetActive(false); }
        var go=new GameObject("TemporaryEntrancePreview"); var camera=go.AddComponent<Camera>();
        var target=new RenderTexture(1500,1100,24); var previous=RenderTexture.active;
        try
        {
            camera.transform.position=new Vector3(.7f,1.75f,-2.7f);
            camera.transform.LookAt(new Vector3(0,1.75f,-9)); camera.fieldOfView=62;
            camera.targetTexture=target; camera.Render(); RenderTexture.active=target;
            var image=new Texture2D(1500,1100,TextureFormat.RGB24,false);
            image.ReadPixels(new Rect(0,0,1500,1100),0,0); image.Apply();
            Directory.CreateDirectory("Temp/EntranceQA"); File.WriteAllBytes("Temp/EntranceQA/entrance.png",image.EncodeToPNG());
            Object.DestroyImmediate(image);
            camera.transform.position=new Vector3(.10f,1.75f,-8.55f);
            camera.transform.LookAt(new Vector3(0,1.75f,-12)); camera.fieldOfView=62;
            camera.Render();
            image=new Texture2D(1500,1100,TextureFormat.RGB24,false); image.ReadPixels(new Rect(0,0,1500,1100),0,0); image.Apply();
            File.WriteAllBytes("Temp/EntranceQA/window.png",image.EncodeToPNG()); Object.DestroyImmediate(image);
        }
        finally { RenderTexture.active=previous; Object.DestroyImmediate(go); target.Release(); Object.DestroyImmediate(target); foreach(var obj in hidden) if(obj) obj.SetActive(true); }
    }
}
