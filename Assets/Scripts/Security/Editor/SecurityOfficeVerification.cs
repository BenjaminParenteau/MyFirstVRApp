using System;
using System.IO;
using HighStakes.Environment;
using HighStakes.Security;
using HighStakes.TableGames;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

/// <summary>Exercises real scene loading, render targets, hold/resume and leaving/revisiting the office.</summary>
[InitializeOnLoad]
public static class SecurityOfficeVerification
{
    const string Pending="SecurityOffice.PlayCheck";
    static double deadline, next;
    static int stage, frames;
    static SecurityMonitorWall wall;
    static SecurityOfficeVerification()
    {
        EditorApplication.playModeStateChanged+=state=>
        {
            if(state==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending,false))
            {
                stage=0; deadline=EditorApplication.timeSinceStartup+150;
                EditorApplication.update+=Tick;
            }
            if(state==PlayModeStateChange.EnteredEditMode && SessionState.GetBool(Pending,false))
            {
                SessionState.SetBool(Pending,false);
                EditorSceneManager.OpenScene(SecurityOfficeBuilder.ScenePath);
            }
        };
    }

    [MenuItem("High Stakes/Security/Run Office Live Feed Play Check")]
    public static void Run()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode) return;
        if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        Directory.CreateDirectory("Temp/SecurityOfficeQA");
        SessionState.SetBool(Pending,true);
        EditorSceneManager.OpenScene("Assets/Scenes/MVP/MVP_Main.unity");
        EditorApplication.isPlaying=true;
    }

    static void Tick()
    {
        try
        {
            if(EditorApplication.timeSinceStartup>deadline) throw new Exception("Timed out at stage "+stage);
            if(EditorApplication.timeSinceStartup<next) return;
            var flow=SceneFlow.Instance;
            if(!flow || flow.Busy) return;
            if(stage==0)
            {
                Require(flow.Go("MVP_SecurityOffice","RoomA_Door"),"Office travel failed"); stage=1; return;
            }
            if(stage==1 || stage==6)
            {
                wall=Object.FindFirstObjectByType<SecurityMonitorWall>();
                if(!wall || wall.RemoteSceneCount!=4 || wall.RenderedFrames<18) return;
                Require(Object.FindObjectsByType<XROrigin>(FindObjectsSortMode.None).Length==1,"Duplicate player rigs");
                Require(Object.FindObjectsByType<HighStakes.Economy.ChipWallet>(FindObjectsSortMode.None).Length==1,"Duplicate wallets");
                Require(Camera.main && (Camera.main.cullingMask & (15<<26))==0,"Remote rooms visible to player camera");
                if(stage==6) { Finish("PASS: Six updating live feeds; one rig and wallet; hold/resume; office -> hub -> office; remote scenes released and reloaded."); return; }
                for(int i=0;i<wall.feeds.Length;i++)
                {
                    var f=wall.feeds[i]; Require(f.texture && f.texture.IsCreated(),"Missing render target "+i);
                    Save(f.texture,"Temp/SecurityOfficeQA/feed-"+(i+1)+".png");
                }
                CaptureOffice();
                wall.GetComponentInChildren<PressableButton>().Pressed.Invoke();
                Require(wall.paused,"Real HOLD button did not pause feeds");
                frames=wall.RenderedFrames; stage=2; next=EditorApplication.timeSinceStartup+1; return;
            }
            if(stage==2)
            {
                Require(wall.RenderedFrames==frames,"Paused feeds kept rendering");
                wall.GetComponentInChildren<PressableButton>().Pressed.Invoke();
                Require(!wall.paused,"Real HOLD button did not resume feeds");
                stage=3; next=EditorApplication.timeSinceStartup+1; return;
            }
            if(stage==3)
            {
                Require(wall.RenderedFrames>frames,"Resumed feeds stopped rendering");
                Require(flow.Go("MVP_SecurityHall","Hub_A"),"Hub travel failed"); stage=4; return;
            }
            if(stage==4)
            {
                Require(SceneManager.sceneCount==2,"Capture scenes remained loaded after exit: "+SceneManager.sceneCount);
                Require(Object.FindObjectsByType<XROrigin>(FindObjectsSortMode.None).Length==1,"Duplicate rigs after exit");
                Require((Camera.main.cullingMask & (15<<26))==(15<<26),"Player camera mask was not restored");
                Require(flow.Go("MVP_SecurityOffice","RoomA_Door"),"Revisit failed"); stage=6;
            }
        }
        catch(Exception e) { Debug.LogException(e); Finish("FAIL: "+e); }
    }

    static void CaptureOffice()
    {
        var go=new GameObject("QA mono overview"); var camera=go.AddComponent<Camera>();
        camera.enabled=false;
        camera.cullingMask=Camera.main.cullingMask; camera.fieldOfView=78; camera.nearClipPlane=.05f;
        camera.transform.position=new Vector3(43,1.75f,.3f);
        camera.transform.LookAt(new Vector3(43,1.5f,5.3f));
        camera.GetUniversalAdditionalCameraData().renderPostProcessing=false;
        camera.GetUniversalAdditionalCameraData().allowXRRendering=false;
        var target=new RenderTexture(1600,1000,24); target.Create();
        RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest {destination=target});
        Save(target,"Temp/SecurityOfficeQA/office.png");
        camera.transform.position=new Vector3(45.5f,1.7f,5.4f);
        camera.transform.LookAt(new Vector3(40.5f,1.3f,1.3f));
        RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest {destination=target});
        Save(target,"Temp/SecurityOfficeQA/office-storage.png");
        target.Release(); Object.Destroy(target); Object.Destroy(go);
    }

    static void Save(RenderTexture target,string path)
    {
        var previous=RenderTexture.active; RenderTexture.active=target;
        var texture=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);
        texture.ReadPixels(new Rect(0,0,target.width,target.height),0,0); texture.Apply();
        File.WriteAllBytes(path,texture.EncodeToPNG());
        RenderTexture.active=previous; Object.Destroy(texture);
    }

    static void Require(bool ok,string message) { if(!ok) throw new Exception(message); }
    static void Finish(string result)
    {
        File.WriteAllText("Temp/SecurityOfficeQA/play-check.txt",result);
        Debug.Log("[SecurityOffice] "+result);
        EditorApplication.update-=Tick; EditorApplication.isPlaying=false;
    }
}
