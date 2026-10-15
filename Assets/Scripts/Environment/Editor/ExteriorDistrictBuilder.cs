using System.Collections.Generic;
using HighStakes.Environment;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>Actual street geometry around the casino frontage, combined by material for Quest.</summary>
public static class ExteriorDistrictBuilder
{
    const string Root="Assets/Content/Environment/Casino";
    public static void Build(Transform entrance,SharedStyleKit kit,Material brick,Material warm,Material sky)
    {
        var district=new GameObject("ExteriorDistrict").transform; district.SetParent(entrance,false);
        var pavement=Mat("M_DistrictSidewalk",kit.concreteCool,new Color(.43f,.45f,.46f),new Color(.035f,.04f,.05f));
        var trim=Mat("M_DistrictStone",kit.concreteCool,new Color(.53f,.50f,.44f),new Color(.045f,.045f,.05f));
        var dark=Mat("M_DistrictWindowDark",kit.steelCool,new Color(.055f,.09f,.12f),new Color(.015f,.025f,.04f));
        var amber=Mat("M_DistrictWindowAmber",warm,Color.black,new Color(.55f,.30f,.12f));
        var soft=Mat("M_DistrictWindowSoft",warm,Color.black,new Color(.24f,.25f,.23f));
        var lamp=Mat("M_DistrictLamp",warm,Color.black,new Color(1.8f,1.35f,.75f));
        var red=Mat("M_DistrictTailLight",warm,Color.black,new Color(.7f,.018f,.008f));
        var road=Road(kit.tileCool);
        var facades=new Material[4];
        Color[] colors={new Color(.65f,.60f,.52f),new Color(.46f,.39f,.33f),new Color(.38f,.42f,.46f),new Color(.55f,.51f,.46f)};
        for(int i=0;i<4;i++)
        {
            facades[i]=Mat("M_DistrictBrick"+i,brick,colors[i],new Color(.075f,.08f,.095f));
            facades[i].SetTextureScale("_BaseMap",new Vector2(2.2f,5));
        }
        var geometry=new List<GameObject>();
        Box("Ground",road,new Vector3(0,-.16f,-62),new Vector3(190,.12f,130));
        // Forecourt, road along the casino and a long avenue through two intersections.
        Box("CasinoSidewalk",pavement,new Vector3(0,-.035f,-2.1f),new Vector3(110,.15f,4.2f));
        Box("FrontCurb",trim,new Vector3(0,.015f,-4.25f),new Vector3(110,.23f,.18f));
        foreach(float side in new[]{-1f,1f})
        {
            Box("AvenueSidewalk",pavement,new Vector3(side*6.2f,-.015f,-65),new Vector3(2.4f,.19f,112));
            Box("AvenueCurb",trim,new Vector3(side*5.0f,.015f,-65),new Vector3(.16f,.23f,112));
        }
        for(int z=13;z<118;z+=6)
        {
            Box("LaneDivider",kit.plasterCream,new Vector3(-.11f,-.089f,-z),new Vector3(.075f,.006f,2.7f));
            Box("LaneDivider",kit.plasterCream,new Vector3(.11f,-.089f,-z),new Vector3(.075f,.006f,2.7f));
        }
        for(int x=-50;x<=50;x+=6) if(Mathf.Abs(x)>6)
            Box("CrossStreetDivider",kit.plasterCream,new Vector3(x,-.089f,-8),new Vector3(2.7f,.006f,.10f));
        foreach(float z in new[]{-11.3f,-40f,-77f})
            for(int i=0;i<9;i++) Box("Crosswalk",kit.chipWhite,new Vector3(-4.25f+i*1.06f,-.083f,z),new Vector3(.55f,.008f,2));
        // Nearby blocks flank the sightline instead of closing it off with one row.
        int block=0;
        for(int side=-1;side<=1;side+=2)
        {
            for(int row=0;row<7;row++)
                Building(side*(11.4f+(row%2)*1.0f),-19-row*15,7.2f+(row%3),9+(row%2)*2,3+(row*3+side+4)%6,block++);
            for(int col=0;col<4;col++)
                Building(side*(23+col*12),-18-(col%2)*2,9+(col%2)*2,11,3+(col*2)%5,block++);
            for(int row=0;row<4;row++)
                Building(side*(27+(row%2)*5),-43-row*20,11,13,5+(row%3)*2,block++);
        }
        for(int i=0;i<9;i++) Building((i-4)*13,-130-(i%3)*7,8+(i%2)*3,10,6+(i*3)%8,block++);
        // Street lamps, bus stop, benches and bollards give the foreground a real scale.
        for(int side=-1;side<=1;side+=2)
            for(int i=0;i<7;i++)
            {
                float x=side*5.7f,z=-13-i*16;
                Box("LampPost",kit.chipBlack,new Vector3(x,2.35f,z),new Vector3(.10f,4.7f,.10f));
                Box("LampArm",kit.chipBlack,new Vector3(x-side*.45f,4.65f,z),new Vector3(1,.085f,.085f));
                Box("LampHousing",kit.chipBlack,new Vector3(x-side*.88f,4.60f,z),new Vector3(.42f,.16f,.56f));
                Box("LampLens",lamp,new Vector3(x-side*.88f,4.51f,z),new Vector3(.33f,.025f,.46f));
            }
        for(int x=-12;x<=12;x+=3) if(Mathf.Abs(x)>2)
        {
            Box("Bollard",kit.steelCool,new Vector3(x,.48f,-3.6f),new Vector3(.10f,.96f,.10f));
            Box("BollardCap",warm,new Vector3(x,.94f,-3.6f),new Vector3(.11f,.025f,.11f));
        }
        foreach(float x in new[]{-8f,8f})
        {
            Box("BenchSeat",kit.woodDark,new Vector3(x,.46f,-2),new Vector3(2,.12f,.53f));
            Box("BenchBack",kit.woodDark,new Vector3(x,.82f,-2.25f),new Vector3(2,.52f,.085f));
            foreach(float offset in new[]{-.7f,.7f}) Box("BenchLeg",kit.chipBlack,new Vector3(x+offset,.22f,-2),new Vector3(.08f,.44f,.42f));
        }
        var carPaint=new[]{Mat("M_DistrictCarBlue",kit.steelCool,new Color(.08f,.14f,.20f),new Color(.018f,.025f,.035f)),Mat("M_DistrictCarSilver",kit.steelCool,new Color(.40f,.43f,.45f),new Color(.04f,.045f,.05f)),Mat("M_DistrictCarRed",kit.steelCool,new Color(.22f,.035f,.025f),new Color(.025f,.008f,.006f))};
        for(int i=0;i<8;i++) Car((i%2==0?-1:1)*3.8f,-19-i*10,carPaint[i%3]);
        // Two modest practical lights illuminate the nearest street without realtime shadows.
        for(int side=-1;side<=1;side+=2)
        {
            var lightObject=new GameObject("ExteriorStreetLight");lightObject.transform.SetParent(district,false);
            lightObject.transform.localPosition=new Vector3(side*5.5f,4.4f,-13);
            var light=lightObject.AddComponent<Light>();light.type=LightType.Point;light.color=new Color(1,.77f,.48f);
            light.intensity=2.8f;light.range=14;light.shadows=LightShadows.None;light.renderMode=LightRenderMode.ForcePixel;
        }
        var skyTexture=Import("T_ExteriorNightSky",false);
        var night=Mat("M_DistrictStarrySky",sky,Color.black,new Color(.045f,.065f,.11f));
        night.SetTexture("_BaseMap",skyTexture);night.SetTexture("_EmissionMap",skyTexture);
        night.SetTextureScale("_BaseMap",new Vector2(-1,1));night.SetTextureOffset("_BaseMap",new Vector2(1,0));
        night.SetFloat("_Cull",(float)CullMode.Front);night.SetColor("_SpecColor",Color.black);
        var dome=TableKit.Part(night,district,"StarryNightSky",PrimitiveType.Sphere,new Vector3(0,0,-50),new Vector3(600,600,600),false,false);
        geometry.Add(dome);
        Combine(district,geometry);
        Debug.Log($"[ExteriorDistrict] Built {block} 3D buildings, 8 cars and a 130m street.");

        void Box(string name,Material material,Vector3 position,Vector3 size,Transform parent=null)
        {
            var go=TableKit.Part(material,parent?parent:district,name,PrimitiveType.Cube,position,size,false,false);
            geometry.Add(go);
        }
        void Quad(string name,Material material,Transform parent,Vector3 position,Vector2 size)
        {
            var go=TableKit.Part(material,parent,name,PrimitiveType.Quad,position,new Vector3(size.x,size.y,1),false,false);
            geometry.Add(go);
        }
        void Building(float x,float z,float width,float depth,int floors,int index)
        {
            var building=new GameObject("District_Block_"+index).transform;building.SetParent(district,false);
            building.localPosition=new Vector3(x,0,z);float height=3.3f+floors*2.65f;
            Box("BrickBuilding",facades[index%4],new Vector3(0,height/2,0),new Vector3(width,height,depth),building);
            Box("StoneBase",trim,new Vector3(0,.28f,0),new Vector3(width+.08f,.56f,depth+.08f),building);
            Box("RoofCornice",trim,new Vector3(0,height-.12f,0),new Vector3(width+.22f,.24f,depth+.22f),building);
            Box("RoofPlant",kit.steelCool,new Vector3(width*.20f,height+.40f,0),new Vector3(1.5f,.80f,2),building);
            if(index%3==0) Box("SetbackTower",facades[(index+1)%4],new Vector3(-width*.12f,height+1.3f,0),new Vector3(width*.55f,2.6f,depth*.6f),building);
            for(int face=0;face<4;face++)
            {
                float span=face%2==0?width:depth;
                var faceRoot=new GameObject("Facade").transform;faceRoot.SetParent(building,false);
                faceRoot.localPosition=face==0?new Vector3(0,0,depth/2+.02f):face==1?new Vector3(width/2+.02f,0,0):face==2?new Vector3(0,0,-depth/2-.02f):new Vector3(-width/2-.02f,0,0);
                faceRoot.localRotation=Quaternion.Euler(0,face==0?180:face==1?-90:face==2?0:90,0);
                int columns=Mathf.Max(3,Mathf.FloorToInt(span/1.65f));
                for(int row=0;row<floors;row++) for(int col=0;col<columns;col++)
                {
                    float wx=(col-(columns-1)*.5f)*(span/(columns+.5f)),wy=4.6f+row*2.65f;
                    Quad("WindowReveal",kit.chipBlack,faceRoot,new Vector3(wx,wy,-.025f),new Vector2(1.12f,1.65f));
                    int seed=(index*73+face*17+row*13+col*23)%11;
                    Quad("WindowPane",seed<6?dark:seed<9?amber:soft,faceRoot,new Vector3(wx,wy,-.035f),new Vector2(.94f,1.43f));
                    if(row==0) Box("StoneSill",trim,new Vector3(wx,wy-.84f,-.11f),new Vector3(1.25f,.09f,.25f),faceRoot);
                    if(seed%3==0) Box("Mullion",kit.chipBlack,new Vector3(wx,wy,-.046f),new Vector3(.025f,1.45f,.012f),faceRoot);
                }
                for(int col=0;col<columns;col++)
                {
                    float wx=(col-(columns-1)*.5f)*(span/(columns+.5f));
                    Quad("Storefront",dark,faceRoot,new Vector3(wx,1.65f,-.04f),new Vector2(1.25f,2.30f));
                    Box("ShopCanopy",index%2==0?kit.fabricBurgundy:kit.feltGreen,new Vector3(wx,3.05f,-.38f),new Vector3(1.52f,.16f,.85f),faceRoot);
                }
            }
        }
        void Car(float x,float z,Material paint)
        {
            var car=new GameObject("ParkedCar").transform;car.SetParent(district,false);car.localPosition=new Vector3(x,.02f,z);
            Box("CarBody",paint,new Vector3(0,.55f,0),new Vector3(1.72f,.56f,4.25f),car);
            Box("Cabin",dark,new Vector3(0,1.02f,-.1f),new Vector3(1.46f,.57f,2.15f),car);
            Box("Roof",paint,new Vector3(0,1.32f,-.1f),new Vector3(1.48f,.06f,1.95f),car);
            foreach(float side in new[]{-1f,1f})
            {
                Box("DoorPillar",paint,new Vector3(side*.75f,1.03f,0),new Vector3(.055f,.58f,.10f),car);
                foreach(float wz in new[]{-1.3f,1.3f})
                {
                    var wheel=TableKit.Part(kit.chipBlack,car,"Wheel",PrimitiveType.Cylinder,new Vector3(side*.85f,.36f,wz),new Vector3(.65f,.13f,.65f),false,false);
                    wheel.transform.localRotation=Quaternion.Euler(0,0,90);geometry.Add(wheel);
                }
                Box("Headlight",lamp,new Vector3(side*.58f,.65f,2.14f),new Vector3(.38f,.17f,.02f),car);
                Box("TailLight",red,new Vector3(side*.58f,.65f,-2.14f),new Vector3(.35f,.15f,.02f),car);
            }
        }
    }

    static Material Mat(string name,Material parent,Color color,Color emission)
    {
        string path=Root+"/Materials/"+name+".mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!material){material=new Material(parent);AssetDatabase.CreateAsset(material,path);}material.parent=parent;
        material.SetColor("_BaseColor",color);material.SetColor("_EmissionColor",emission);material.EnableKeyword("_EMISSION");
        material.SetFloat("_Smoothness",.24f);EditorUtility.SetDirty(material);return material;
    }
    static Material Road(Material parent)
    {
        var albedo=Import("T_ExteriorAsphalt_Albedo",false);var normal=Import("T_ExteriorAsphalt_Normal",true);
        var material=Mat("M_DistrictAsphalt",parent,new Color(.43f,.45f,.48f),new Color(.018f,.022f,.03f));
        material.SetTexture("_BaseMap",albedo);material.SetTexture("_EmissionMap",albedo);material.SetTexture("_BumpMap",normal);
        material.SetTextureScale("_BaseMap",new Vector2(55,40));material.EnableKeyword("_NORMALMAP");material.SetFloat("_Smoothness",.38f);
        return material;
    }
    static Texture2D Import(string name,bool normal)
    {
        string path=Root+"/Textures/"+name+".jpg";AssetDatabase.ImportAsset(path);var importer=(TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType=normal?TextureImporterType.NormalMap:TextureImporterType.Default;importer.sRGBTexture=!normal;
        importer.mipmapEnabled=true;importer.maxTextureSize=name=="T_ExteriorNightSky"?4096:1024;importer.wrapMode=TextureWrapMode.Repeat;
        var settings=importer.GetPlatformTextureSettings("Android");settings.overridden=true;settings.maxTextureSize=name=="T_ExteriorNightSky"?4096:1024;settings.format=TextureImporterFormat.ASTC_6x6;
        importer.SetPlatformTextureSettings(settings);importer.SaveAndReimport();return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }
    static void Combine(Transform root,List<GameObject> geometry)
    {
        var groups=new Dictionary<Material,List<CombineInstance>>();
        foreach(var go in geometry)
        {
            var renderer=go.GetComponent<MeshRenderer>();var material=renderer.sharedMaterial;
            if(!groups.TryGetValue(material,out var list)){list=new List<CombineInstance>();groups.Add(material,list);}
            list.Add(new CombineInstance{mesh=go.GetComponent<MeshFilter>().sharedMesh,transform=root.worldToLocalMatrix*go.transform.localToWorldMatrix});
        }
        const string folder=Root+"/Meshes";TableKit.EnsureFolder(folder);
        foreach(var pair in groups)
        {
            var mesh=new Mesh{name="District_"+pair.Key.name,indexFormat=IndexFormat.UInt32};mesh.CombineMeshes(pair.Value.ToArray(),true,true);mesh.RecalculateBounds();
            string path=folder+"/"+mesh.name+".asset";var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(existing){EditorUtility.CopySerialized(mesh,existing);Object.DestroyImmediate(mesh);mesh=existing;EditorUtility.SetDirty(mesh);}else AssetDatabase.CreateAsset(mesh,path);
            var go=new GameObject(mesh.name);go.transform.SetParent(root,false);go.AddComponent<MeshFilter>().sharedMesh=mesh;
            var renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterial=pair.Key;renderer.shadowCastingMode=ShadowCastingMode.Off;
            GameObjectUtility.SetStaticEditorFlags(go,StaticEditorFlags.BatchingStatic);
        }
        foreach(var go in geometry) Object.DestroyImmediate(go);
    }
}

