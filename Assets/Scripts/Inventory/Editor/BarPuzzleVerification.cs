using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using HighStakes.Core;
using HighStakes.Economy;
using HighStakes.Inventory;
using HighStakes.TableGames;
using TMPro;
using UnityEditor;
using UnityEngine;

/// <summary>Editor smoke check of the real prefab and shared wallet, without altering saved scenes.</summary>
public static class BarPuzzleVerification
{
    [MenuItem("High Stakes/Puzzles/Verify Bar PIN Pad Puzzle")]
    public static void Verify()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Content/Progression/BarPuzzle.prefab");
        GameObject walletObject = null, instance = null, returned = null;
        var existingWallets = new List<GameObject>();
        foreach(var component in UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
            if(component is IChipWallet) { existingWallets.Add(component.gameObject); component.gameObject.SetActive(false); }
        try
        {
            walletObject = new GameObject("PuzzleVerificationWallet");
            var wallet = walletObject.AddComponent<ChipWallet>();
            instance = UnityEngine.Object.Instantiate(prefab);
            var puzzle = instance.GetComponent<BarPuzzle>();
            Call(puzzle,"Start");
            Press(puzzle,"Submit");
            Require(wallet.Balance == 0,"Wrong PIN awarded money");
            SetPin(puzzle);
            Press(puzzle,"Submit");
            Require(wallet.Balance == 500,"Correct PIN did not add exactly $500");
            Press(puzzle,"Submit");
            Require(wallet.Balance == 500,"Repeated submission paid twice");
            returned = UnityEngine.Object.Instantiate(prefab);
            var revisit = returned.GetComponent<BarPuzzle>();
            Call(revisit,"Start"); SetPin(revisit); Press(revisit,"Submit");
            Require(wallet.Balance == 500,"Re-entering the casino paid twice");
            Require((bool)Field("completed").GetValue(revisit),"Completed puzzle controls still active");
            UnityEngine.Object.DestroyImmediate(walletObject); walletObject=null;
            UnityEngine.Object.DestroyImmediate(returned); returned=null;
            returned=UnityEngine.Object.Instantiate(prefab);
            var noWallet=returned.GetComponent<BarPuzzle>(); Call(noWallet,"Start"); SetPin(noWallet); Press(noWallet,"Submit");
            Require(!(bool)Field("completed").GetValue(noWallet),"Missing wallet consumed the reward");
            walletObject=new GameObject("NewSessionWallet"); var fresh=walletObject.AddComponent<ChipWallet>();
            Press(noWallet,"Submit"); Require(fresh.Balance == 500,"Unpaid solve could not be claimed later");
            Directory.CreateDirectory("Temp/BarPuzzleQA");
            File.WriteAllText("Temp/BarPuzzleQA/verification.txt","PASS: wrong PIN, exact $500 payout, repeat press, casino revisit, missing wallet, delayed claim, and new wallet/session.");
            Debug.Log("[BarPuzzle] VERIFICATION PASSED");
        }
        finally
        {
            if(instance) UnityEngine.Object.DestroyImmediate(instance);
            if(returned) UnityEngine.Object.DestroyImmediate(returned);
            if(walletObject) UnityEngine.Object.DestroyImmediate(walletObject);
            foreach(var existing in existingWallets) if(existing) existing.SetActive(true);
        }
    }

    [MenuItem("High Stakes/Puzzles/Render Bar PIN Pad Preview")]
    public static void RenderPreview()
    {
        var cameraObject=new GameObject("TemporaryPuzzlePreviewCamera");
        var target=new RenderTexture(1600,1000,24);
        var previous=RenderTexture.active;
        try
        {
            var camera=cameraObject.AddComponent<Camera>();
            camera.transform.position=new Vector3(-4.53f,1.75f,7.74f);
            camera.transform.LookAt(new Vector3(-4.53f,1.36f,8.62f));
            camera.fieldOfView=48; camera.nearClipPlane=.05f;
            camera.targetTexture=target; camera.Render();
            RenderTexture.active=target;
            var texture=new Texture2D(1600,1000,TextureFormat.RGB24,false);
            texture.ReadPixels(new Rect(0,0,1600,1000),0,0); texture.Apply();
            Directory.CreateDirectory("Temp/BarPuzzleQA");
            File.WriteAllBytes("Temp/BarPuzzleQA/preview.png",texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
        }
        finally { RenderTexture.active=previous; UnityEngine.Object.DestroyImmediate(cameraObject); target.Release(); UnityEngine.Object.DestroyImmediate(target); }
    }

    static void SetPin(BarPuzzle puzzle)
    {
        Field("entry").SetValue(puzzle,"8642");
    }
    static FieldInfo Field(string name) => typeof(BarPuzzle).GetField(name,BindingFlags.Instance|BindingFlags.NonPublic);
    static void Call(BarPuzzle puzzle,string name) => typeof(BarPuzzle).GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public).Invoke(puzzle,null);
    static void Press(BarPuzzle puzzle,string name) { Field("nextPress").SetValue(puzzle,-1f); Call(puzzle,name); }
    static void Require(bool condition,string message) { if(!condition) throw new InvalidOperationException(message); }
}
