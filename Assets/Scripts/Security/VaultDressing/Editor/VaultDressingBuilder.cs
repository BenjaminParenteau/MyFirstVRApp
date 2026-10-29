using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HighStakes.Security;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object=UnityEngine.Object;

public static class VaultDressingBuilder
{
    const string Folder="Assets/Content/Progression/VaultDressing";
    const string MetalTextures="Assets/Content/Environment/Casino/Textures/T_BrushedMetal_";
    static Material steel,dark,gold,paper,cash,band,rubber,amber,screen;
    static Mesh ingot,ring,wheel;
    static Cubemap environment;
    static VaultDressingModule manifest;
    static System.Random random;

    [MenuItem("High Stakes/Vault/Build Treasury Dressing")]
    public static void Build()
    {
        foreach(string sub in new[]{"Textures","Materials","Meshes","Resources"})Directory.CreateDirectory(Folder+"/"+sub);
        AssetDatabase.Refresh();random=new System.Random(327);
        environment=BuildEnvironment();
        PaperEdges();
        steel=Material("Brushed steel",new Color(.72f,.77f,.8f),.9f,.58f,true);
        dark=Material("Gunmetal",new Color(.12f,.17f,.2f),.8f,.38f,true);
        gold=Material("Cast gold",new Color(1f,.7f,.24f),1f,.72f,true);
        paper=Material("Paper edges",Color.white,0,.1f,false);
        paper.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+"/Textures/T_CashEdges.png"));
        cash=Material("Engraved banknote",Color.white,0,.12f,false);
        cash.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+"/Textures/T_MeridianCash.png"));
        band=Material("Cash bands",new Color(.87f,.8f,.65f),0,.13f,false);
        rubber=Material("Rubber and recesses",new Color(.022f,.028f,.03f),0,.18f,false);
        amber=Material("Seal red",new Color(.34f,.045f,.035f),.1f,.3f,false);
        screen=Material("Counter display",new Color(.015f,.06f,.07f),.1f,.5f,false);
        screen.EnableKeyword("_EMISSION");screen.SetColor("_EmissionColor",new Color(.015f,.13f,.16f));
        ingot=SaveMesh(Ingot(),"CastIngot");ring=SaveMesh(Torus(1.27f,.095f,56,8),"DoorRing");wheel=SaveMesh(Torus(.3f,.026f,32,8),"HandleWheel");
        var preview=EditorSceneManager.NewPreviewScene();
        var root=new GameObject("VaultDressingModule");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root,preview);
        try
        {
            manifest=root.AddComponent<VaultDressingModule>();
            for(int n=0;n<4;n++)
            {
                Cabinets(root.transform,new Vector3(-.62f,0,.8f+n*1.55f),-90,3,6);
                Cabinets(root.transform,new Vector3(6.62f,0,.8f+n*1.55f),90,3,6);
            }
            Cabinets(root.transform,new Vector3(.38f,0,6.62f),0,2,6);
            Cabinets(root.transform,new Vector3(5.65f,0,6.62f),0,2,6);
            // Keep the entrance reusable for the hallway side of the portal as well.
            var entrance=VaultDoor(root.transform);
            var doorPrefab=PrefabUtility.SaveAsPrefabAsset(entrance.gameObject,Folder+"/Resources/VaultEntranceDoor.prefab");
            Object.DestroyImmediate(entrance.gameObject);
            Cart(root.transform,new Vector3(.8f,0,2.05f),-8,false);
            Cart(root.transform,new Vector3(5.1f,0,2.5f),12,true);
            Cart(root.transform,new Vector3(.9f,0,4.2f),8,false);
            CountingDesk(root.transform);
            BullionPallet(root.transform,new Vector3(5.25f,0,5.25f));
            CashCrate(root.transform,new Vector3(1.15f,0,.45f));
            CashCrate(root.transform,new Vector3(5.25f,0,.35f));
            CashPile(root.transform,new Vector3(1.65f,.018f,3.25f),2,2,2);
            Combine(root.transform);
            PrefabUtility.SaveAsPrefabAsset(root,Folder+"/Resources/VaultDressingModule.prefab");
            AssetDatabase.SaveAssets();
            Debug.Log($"[VaultDressing] {manifest.cashBundles} cash bundles, {manifest.goldBars} gold bars, {manifest.depositDrawers} deposit drawers, {manifest.cashCarts} carts; {manifest.triangles} triangles. Shared scenes unchanged.");
        }
        finally {Object.DestroyImmediate(root);EditorSceneManager.ClosePreviewScene(preview);}
    }

    static Material Material(string name,Color tint,float metallic,float smooth,bool textured)
    {
        string path=Folder+"/Materials/"+name+".mat";
        var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!m){m=new Material(Shader.Find("High Stakes/Treasury Surface"));AssetDatabase.CreateAsset(m,path);}
        m.shader=Shader.Find("High Stakes/Treasury Surface");m.SetTexture("_Environment",environment);
        m.SetColor("_BaseColor",tint);m.SetFloat("_Metallic",metallic);m.SetFloat("_Smoothness",smooth);
        if(textured)
        {
            m.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(MetalTextures+"Albedo.jpg"));
            m.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(MetalTextures+"Normal.jpg"));m.EnableKeyword("_NORMALMAP");m.SetFloat("_BumpScale",.35f);
        }
        m.enableInstancing=true;EditorUtility.SetDirty(m);return m;
    }

    static Transform Group(Transform parent,string name,Vector3 position,float yaw=0)
    {
        var t=new GameObject(name).transform;t.SetParent(parent,false);t.localPosition=position;t.localRotation=Quaternion.Euler(0,yaw,0);return t;
    }

    static GameObject Part(Transform parent,string name,PrimitiveType primitive,Vector3 pos,Vector3 scale,Material mat,bool collision=false)
    {
        var go=GameObject.CreatePrimitive(primitive);go.name=name;go.transform.SetParent(parent,false);go.transform.localPosition=pos;go.transform.localScale=scale;
        go.GetComponent<Renderer>().sharedMaterial=mat;
        if(!collision)Object.DestroyImmediate(go.GetComponent<Collider>());
        return go;
    }

    static GameObject Box(Transform p,string name,Vector3 pos,Vector3 size,Material mat,bool collision=false)=>Part(p,name,PrimitiveType.Cube,pos,size,mat,collision);
    static void MeshPart(Transform p,string name,Vector3 pos,Mesh mesh,Material mat,Quaternion rotation)
    {
        var go=new GameObject(name);go.transform.SetParent(p,false);go.transform.localPosition=pos;go.transform.localRotation=rotation;go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=mat;
    }

    static void Label(Transform parent,string words,Vector3 position,float fontSize,Vector2 size,Color color,Quaternion rotation)
    {
        var label=new GameObject("Marking "+words).AddComponent<TextMeshPro>();label.transform.SetParent(parent,false);label.transform.localPosition=position;label.transform.localRotation=rotation;
        label.font=TMP_Settings.defaultFontAsset;label.fontSize=fontSize;label.color=color;label.alignment=TextAlignmentOptions.Center;label.rectTransform.sizeDelta=size;label.text=words;label.ForceMeshUpdate();
    }

    static void Cabinets(Transform parent,Vector3 pos,float yaw,int cols,int rows)
    {
        var g=Group(parent,"Deposit cabinet",pos,yaw);float width=cols*.46f+.12f;
        Box(g,"Armored cabinet body",new Vector3(0,1.1f,0),new Vector3(width,2.2f,.48f),dark,true);
        Box(g,"Recess backing",new Vector3(0,1.13f,-.249f),new Vector3(width-.07f,2.02f,.015f),rubber);
        for(int row=0;row<rows;row++)for(int col=0;col<cols;col++)
        {
            float x=(col-(cols-1)*.5f)*.46f,y=.30f+row*.32f;manifest.depositDrawers++;
            bool open=pos.x<0&&row==2&&col==1&&pos.z>2&&pos.z<3;
            float front=open?-.46f:-.27f;
            Box(g,"Deposit drawer "+manifest.depositDrawers,new Vector3(x,y,front),new Vector3(.435f,.294f,.035f),(row+col)%3==0?steel:dark);
            Box(g,"Inset escutcheon",new Vector3(x,y,front-.023f),new Vector3(.23f,.065f,.014f),steel);
            Box(g,"Recessed pull",new Vector3(x,y-.012f,front-.04f),new Vector3(.13f,.018f,.025f),rubber);
            var lockGo=Part(g,"Dual key lock",PrimitiveType.Cylinder,new Vector3(x+.15f,y,front-.026f),new Vector3(.025f,.009f,.025f),steel);
            lockGo.transform.localRotation=Quaternion.Euler(90,0,0);
            if(col==0)Label(g,(1000+manifest.depositDrawers).ToString(),new Vector3(x-.12f,y+.085f,front-.022f),.045f,new Vector2(.14f,.04f),new Color(.8f,.83f,.84f),Quaternion.identity);
            if(open){Box(g,"Extended drawer tray",new Vector3(x,y-.10f,-.34f),new Vector3(.41f,.022f,.30f),steel);CashPile(g,new Vector3(x,y-.07f,-.39f),2,2,2);}
        }
        Box(g,"Cabinet kick plate",new Vector3(0,.085f,-.26f),new Vector3(width,.10f,.03f),steel);
    }

    static Transform VaultDoor(Transform p)
    {
        var g=Group(p,"Reinforced treasury door",Vector3.zero);
        // A real open frame: the original solid backing would remain across the opening when the leaf swings.
        foreach(float side in new[]{-1f,1f})
            Box(g,"Armored door surround jamb",new Vector3(side*1.425f,1.42f,.19f),new Vector3(.2f,2.84f,.18f),dark);
        Box(g,"Armored door surround lintel",new Vector3(0,2.75f,.19f),new Vector3(3.05f,.18f,.18f),dark);
        Box(g,"Armored door surround threshold",new Vector3(0,.04f,.19f),new Vector3(3.05f,.08f,.18f),dark);
        var slab=Part(g,"Circular steel door",PrimitiveType.Cylinder,new Vector3(0,1.4f,0),new Vector3(2.52f,.13f,2.52f),steel);slab.transform.localRotation=Quaternion.Euler(90,0,0);
        MeshPart(g,"Machined outer ring",new Vector3(0,1.4f,-.15f),ring,dark,Quaternion.identity);
        for(int n=0;n<12;n++)
        {
            float a=n*Mathf.PI/6;Vector3 pos=new Vector3(Mathf.Cos(a)*1.05f,1.4f+Mathf.Sin(a)*1.05f,-.18f);
            var bolt=Part(g,"Radial locking bolt",PrimitiveType.Cylinder,pos,new Vector3(.075f,.045f,.075f),dark);bolt.transform.localRotation=Quaternion.Euler(90,0,0);
        }
        MeshPart(g,"Handwheel",new Vector3(0,1.4f,-.36f),wheel,steel,Quaternion.identity);
        for(int n=0;n<5;n++)
        {
            float a=n*72;var spoke=Box(g,"Wheel spoke",new Vector3(Mathf.Sin(a*Mathf.Deg2Rad)*.14f,1.4f+Mathf.Cos(a*Mathf.Deg2Rad)*.14f,-.36f),new Vector3(.025f,.29f,.035f),dark);spoke.transform.localRotation=Quaternion.Euler(0,0,-a);
        }
        var hub=Part(g,"Wheel hub",PrimitiveType.Cylinder,new Vector3(0,1.4f,-.41f),new Vector3(.13f,.065f,.13f),dark);hub.transform.localRotation=Quaternion.Euler(90,0,0);
        var grip=Part(g,"Handwheel crank grip",PrimitiveType.Cylinder,new Vector3(.28f,1.4f,-.44f),new Vector3(.045f,.07f,.045f),dark);grip.transform.localRotation=Quaternion.Euler(90,0,0);
        for(int n=0;n<3;n++){Box(g,"Heavy hinge",new Vector3(-1.38f,.55f+n*.85f,-.04f),new Vector3(.18f,.27f,.29f),steel);}
        Box(g,"Door serial plate",new Vector3(0,.77f,-.17f),new Vector3(.58f,.16f,.018f),dark);
        return g;
    }

    static void CashPile(Transform p,Vector3 origin,int nx,int nz,int layers)
    {
        for(int y=0;y<layers;y++)for(int z=0;z<nz;z++)for(int x=0;x<nx;x++)
        {
            var pos=origin+new Vector3((x-(nx-1)*.5f)*.172f,y*.034f,(z-(nz-1)*.5f)*.08f);
            pos.x+=(float)(random.NextDouble()-.5)*.008f;pos.z+=(float)(random.NextDouble()-.5)*.006f;
            var g=Group(p,"Banded cash bundle",pos,(float)(random.NextDouble()-.5)*5f);manifest.cashBundles++;
            Box(g,"Layered paper",Vector3.zero,new Vector3(.16f,.031f,.07f),paper);
            var top=Part(g,"Engraved note",PrimitiveType.Quad,new Vector3(0,.0158f,0),new Vector3(.16f,.07f,1),cash);top.transform.localRotation=Quaternion.Euler(90,0,0);
            Box(g,"Paper strap",new Vector3(0,.0005f,0),new Vector3(.024f,.033f,.072f),band);
            Box(g,"Reserve seal",new Vector3(0,.0175f,0),new Vector3(.012f,.001f,.055f),amber);
        }
    }

    static void GoldPile(Transform p,Vector3 origin,int nx,int nz,int layers)
    {
        for(int y=0;y<layers;y++)for(int z=0;z<nz;z++)for(int x=0;x<nx;x++)
        {
            var pos=origin+new Vector3((x-(nx-1)*.5f)*.12f+(y%2)*.018f,y*.057f,(z-(nz-1)*.5f)*.28f);
            MeshPart(p,"Cast bullion bar",pos,ingot,gold,Quaternion.identity);manifest.goldBars++;
            if(y==layers-1 && x%2==0)Label(p,"999.9\nFINE GOLD",pos+new Vector3(0,.056f,0),.055f,new Vector2(.075f,.12f),new Color(.29f,.17f,.035f),Quaternion.Euler(90,0,0));
        }
    }

    static void Cart(Transform parent,Vector3 pos,float yaw,bool bullion)
    {
        var g=Group(parent,bullion?"Bullion transport trolley":"Loaded cash trolley",pos,yaw);manifest.cashCarts++;
        foreach(float x in new[]{-.43f,.43f})foreach(float z in new[]{-.3f,.3f})
        {
            Box(g,"Tubular upright",new Vector3(x,.64f,z),new Vector3(.035f,.99f,.035f),steel);
            var caster=Part(g,"Rubber caster",PrimitiveType.Cylinder,new Vector3(x,.10f,z),new Vector3(.14f,.032f,.14f),rubber);caster.transform.localRotation=Quaternion.Euler(0,0,90);
            Box(g,"Caster fork",new Vector3(x,.19f,z),new Vector3(.055f,.15f,.06f),steel);
        }
        foreach(float y in new[]{.35f,.95f})
        {
            Box(g,"Riveted tray",new Vector3(0,y,0),new Vector3(.98f,.035f,.73f),dark);
            foreach(float z in new[]{-.36f,.36f})Box(g,"Tray lip",new Vector3(0,y+.04f,z),new Vector3(.98f,.07f,.025f),steel);
            if(bullion)GoldPile(g,new Vector3(0,y+.02f,0),6,2,3);else CashPile(g,new Vector3(0,y+.035f,0),5,6,4);
        }
        Box(g,"Push handle",new Vector3(.49f,1.1f,0),new Vector3(.035f,.035f,.65f),steel);
        var collider=g.gameObject.AddComponent<BoxCollider>();collider.center=new Vector3(0,.62f,0);collider.size=new Vector3(1.05f,1.2f,.8f);
    }

    static void CountingDesk(Transform p)
    {
        var g=Group(p,"Cash verification station",new Vector3(.55f,0,5.7f));
        Box(g,"Counting counter",new Vector3(0,.84f,0),new Vector3(1.5f,.09f,.78f),steel,true);
        foreach(float x in new[]{-.60f,.60f})Box(g,"Counter pedestal",new Vector3(x,.40f,0),new Vector3(.22f,.8f,.64f),dark,true);
        Box(g,"Counting machine",new Vector3(.26f,1.03f,.08f),new Vector3(.43f,.31f,.35f),dark);
        var screenObj=Box(g,"Machine display",new Vector3(.26f,1.12f,-.101f),new Vector3(.32f,.11f,.01f),screen);
        Label(g,"COUNT  01200",new Vector3(.26f,1.12f,-.109f),.049f,new Vector2(.30f,.08f),new Color(.5f,.95f,1f),Quaternion.identity);
        for(int n=0;n<5;n++)Box(g,"Machine key",new Vector3(.12f+n*.065f,.98f,-.104f),new Vector3(.04f,.022f,.012f),rubber);
        CashPile(g,new Vector3(-.39f,.91f,.07f),2,4,5);
        Box(g,"Open cash register drawer",new Vector3(.13f,.63f,-.36f),new Vector3(.61f,.055f,.6f),steel);
        for(int n=0;n<4;n++)Box(g,"Till divider",new Vector3(-.14f+n*.18f,.69f,-.4f),new Vector3(.012f,.09f,.47f),dark);
        CashPile(g,new Vector3(.13f,.68f,-.46f),3,3,2);
        Label(g,"RESERVE PROCESSING / 03",new Vector3(0,.80f,-.4f),.07f,new Vector2(1.1f,.10f),new Color(.7f,.78f,.8f),Quaternion.identity);
    }

    static void BullionPallet(Transform p,Vector3 pos)
    {
        var g=Group(p,"Bullion reserve pallet",pos,-7);
        for(int n=0;n<5;n++)Box(g,"Pallet slat",new Vector3((n-2)*.23f,.13f,0),new Vector3(.20f,.07f,1.25f),dark);
        foreach(float z in new[]{-.45f,.45f})Box(g,"Pallet runner",new Vector3(0,.055f,z),new Vector3(1.15f,.11f,.13f),dark);
        var collider=g.gameObject.AddComponent<BoxCollider>();collider.center=new Vector3(0,.26f,0);collider.size=new Vector3(1.2f,.52f,1.3f);
        GoldPile(g,new Vector3(0,.17f,0),7,4,5);
    }

    static void CashCrate(Transform p,Vector3 pos)
    {
        var g=Group(p,"Open cash transit trunk",pos,-4);
        Box(g,"Trunk base",new Vector3(0,.07f,0),new Vector3(.85f,.14f,.52f),dark,true);
        foreach(float x in new[]{-.42f,.42f})Box(g,"Trunk wall",new Vector3(x,.24f,0),new Vector3(.035f,.34f,.52f),steel);
        foreach(float z in new[]{-.25f,.25f})Box(g,"Trunk wall",new Vector3(0,.24f,z),new Vector3(.85f,.34f,.035f),steel);
        CashPile(g,new Vector3(0,.16f,0),4,5,7);
    }

    static void Light(Transform parent,Vector3 pos,Color color,float intensity)
    {
        var t=Group(parent,"Treasury accent light",pos);var light=t.gameObject.AddComponent<UnityEngine.Light>();light.type=LightType.Point;light.color=color;light.intensity=intensity;light.range=5.8f;light.shadows=LightShadows.None;
    }

    static Cubemap BuildEnvironment()
    {
        string path=Folder+"/Textures/TreasuryReflections.asset";
        var existing=AssetDatabase.LoadAssetAtPath<Cubemap>(path);if(existing)return existing;
        const int size=128;var cube=new Cubemap(size,TextureFormat.RGBAHalf,true);cube.name="Treasury reflection environment";
        for(int face=0;face<6;face++)for(int y=0;y<size;y++)for(int x=0;x<size;x++)
        {
            float u=(x+.5f)/size*2-1,v=(y+.5f)/size*2-1;Vector3 d;
            switch(face){case 0:d=new Vector3(1,-v,-u);break;case 1:d=new Vector3(-1,-v,u);break;case 2:d=new Vector3(u,1,v);break;case 3:d=new Vector3(u,-1,-v);break;case 4:d=new Vector3(u,-v,1);break;default:d=new Vector3(-u,-v,-1);break;}
            d.Normalize();var color=Color.Lerp(new Color(.06f,.07f,.08f),new Color(.40f,.46f,.52f),(d.y+1)*.5f);
            float panel=Mathf.Exp(-Mathf.Pow((Mathf.Abs(d.x)-.36f)*11,2))*Mathf.SmoothStep(.25f,.8f,d.y);
            float rim=Mathf.Exp(-Mathf.Pow((Mathf.Abs(d.z)-.72f)*14,2))*Mathf.SmoothStep(.1f,.8f,d.y);
            color+=new Color(1f,.96f,.86f)*(panel*3.5f+rim*2.2f);cube.SetPixel((CubemapFace)face,x,y,color);
        }
        cube.Apply(true,false);AssetDatabase.CreateAsset(cube,path);return cube;
    }

    static void PaperEdges()
    {
        string path=Folder+"/Textures/T_CashEdges.png";
        if(!File.Exists(path))
        {
            var texture=new Texture2D(256,128,TextureFormat.RGB24,false);var pixels=new Color[256*128];
            for(int y=0;y<128;y++)for(int x=0;x<256;x++){float shade=(y%3==0?.56f:.84f)+(float)Math.Sin(x*.13+y*.7)*.025f;pixels[y*256+x]=new Color(shade,shade*.99f,shade*.90f);}
            texture.SetPixels(pixels);texture.Apply();File.WriteAllBytes(path,texture.EncodeToPNG());Object.DestroyImmediate(texture);AssetDatabase.ImportAsset(path);
        }
        foreach(string texturePath in new[]{path,Folder+"/Textures/T_MeridianCash.png"})
        {
            var importer=(TextureImporter)AssetImporter.GetAtPath(texturePath);if(importer==null)continue;importer.maxTextureSize=texturePath==path?256:2048;importer.mipmapEnabled=true;importer.SaveAndReimport();
        }
    }

    static Mesh Ingot()
    {
        var mesh=new Mesh();var vertices=new List<Vector3>();var triangles=new List<int>();
        float[] ys={0,.006f,.047f,.055f},xs={.049f,.055f,.044f,.038f},zs={.129f,.135f,.119f,.113f};
        for(int r=0;r<4;r++){vertices.Add(new Vector3(-xs[r],ys[r],-zs[r]));vertices.Add(new Vector3(xs[r],ys[r],-zs[r]));vertices.Add(new Vector3(xs[r],ys[r],zs[r]));vertices.Add(new Vector3(-xs[r],ys[r],zs[r]));}
        for(int r=0;r<3;r++)for(int n=0;n<4;n++){int a=r*4+n,b=r*4+(n+1)%4,c=b+4,d=a+4;triangles.AddRange(new[]{a,d,c,a,c,b});}
        triangles.AddRange(new[]{0,1,2,0,2,3,12,14,13,12,15,14});mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.SetUVs(0,vertices.Select(v=>new Vector2(v.x*5,v.z*5)).ToList());mesh.RecalculateNormals();mesh.RecalculateBounds();mesh.RecalculateTangents();return mesh;
    }

    static Mesh Torus(float radius,float tube,int segments,int sides)
    {
        var mesh=new Mesh();var vertices=new List<Vector3>();var triangles=new List<int>();var uv=new List<Vector2>();
        for(int i=0;i<=segments;i++)for(int j=0;j<=sides;j++){float a=i*Mathf.PI*2/segments,b=j*Mathf.PI*2/sides;vertices.Add(new Vector3((radius+tube*Mathf.Cos(b))*Mathf.Cos(a),(radius+tube*Mathf.Cos(b))*Mathf.Sin(a),tube*Mathf.Sin(b)));uv.Add(new Vector2((float)i/segments,(float)j/sides));}
        for(int i=0;i<segments;i++)for(int j=0;j<sides;j++){int a=i*(sides+1)+j,b=a+sides+1;triangles.AddRange(new[]{a,b,a+1,a+1,b,b+1});}
        mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.SetUVs(0,uv);mesh.RecalculateNormals();mesh.RecalculateBounds();mesh.RecalculateTangents();return mesh;
    }

    static Mesh SaveMesh(Mesh source,string name)
    {
        string path=Folder+"/Meshes/"+name+".asset";source.name=name;
        var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(existing){EditorUtility.CopySerialized(source,existing);Object.DestroyImmediate(source);EditorUtility.SetDirty(existing);return existing;}
        AssetDatabase.CreateAsset(source,path);return source;
    }

    static void Combine(Transform root)
    {
        foreach(var text in root.GetComponentsInChildren<TextMeshPro>())text.ForceMeshUpdate();
        var groups=new Dictionary<Material,List<CombineInstance>>();
        var sources=root.GetComponentsInChildren<MeshFilter>();
        foreach(var filter in sources)
        {
            var renderer=filter.GetComponent<MeshRenderer>();if(!renderer||!filter.sharedMesh||!renderer.sharedMaterial)continue;
            var mat=renderer.sharedMaterial;if(!groups.TryGetValue(mat,out var list)){list=new List<CombineInstance>();groups[mat]=list;}
            list.Add(new CombineInstance{mesh=filter.sharedMesh,transform=root.worldToLocalMatrix*filter.transform.localToWorldMatrix});
        }
        int number=0;
        foreach(var pair in groups)
        {
            var mesh=new Mesh{indexFormat=IndexFormat.UInt32};mesh.CombineMeshes(pair.Value.ToArray(),true,true);mesh=SaveMesh(mesh,"Combined_"+(number++));manifest.triangles+=mesh.triangles.Length/3;
            MeshPart(root,"Batched "+pair.Key.name,Vector3.zero,mesh,pair.Key,Quaternion.identity);
        }
        foreach(var text in root.GetComponentsInChildren<TextMeshPro>())Object.DestroyImmediate(text);
        foreach(var filter in sources){if(!filter)continue;var renderer=filter.GetComponent<MeshRenderer>();if(renderer)Object.DestroyImmediate(renderer);if(filter)Object.DestroyImmediate(filter);}
        foreach(var t in root.GetComponentsInChildren<Transform>().Reverse())
            if(t!=root && t.childCount==0 && t.GetComponents<Component>().Length==1)Object.DestroyImmediate(t.gameObject);
    }
}
