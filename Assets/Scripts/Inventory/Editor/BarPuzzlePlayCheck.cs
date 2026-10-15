using System;
using System.IO;
using System.Reflection;
using HighStakes.Core;
using HighStakes.Inventory;
using HighStakes.TableGames;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Checks actual button events and the live wallet after MVP_Main loads the casino.</summary>
[InitializeOnLoad]
public static class BarPuzzlePlayCheck
{
    static int stage;
    static double next, deadline;
    static int startingBalance;
    static BarPuzzle puzzle;
    static IChipWallet wallet;
    const string Pending = "BarPuzzle.PlayCheck";

    static BarPuzzlePlayCheck()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if(state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending,false))
            {
                stage=0; deadline=EditorApplication.timeSinceStartup+45;
                EditorApplication.update += Tick;
            }
            if(state == PlayModeStateChange.EnteredEditMode && SessionState.GetBool(Pending,false))
            {
                SessionState.SetBool(Pending,false);
                EditorSceneManager.OpenScene("Assets/Scenes/MVP/MVP_Casino.unity");
            }
        };
    }

    [MenuItem("High Stakes/Puzzles/Run Bar PIN Pad Play Check")]
    public static void Run()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode) return;
        if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        SessionState.SetBool(Pending,true);
        EditorSceneManager.OpenScene("Assets/Scenes/MVP/MVP_Main.unity");
        EditorApplication.isPlaying=true;
    }

    static void Tick()
    {
        try
        {
            if(EditorApplication.timeSinceStartup>deadline) throw new Exception("Timed out loading casino");
            if(EditorApplication.timeSinceStartup<next) return;
            if(stage==0)
            {
                puzzle=UnityEngine.Object.FindFirstObjectByType<BarPuzzle>();
                if(!puzzle) return;
                foreach(var component in UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
                    if(component is IChipWallet candidate) wallet=candidate;
                if(wallet==null) return;
                startingBalance=wallet.Balance;
                stage++; next=EditorApplication.timeSinceStartup+.5; return;
            }
            var buttons=(PressableButton[])Field("keys").GetValue(puzzle);
            var serve=buttons[11];
            switch(stage)
            {
                case 1: serve.Pressed.Invoke(); break;
                case 2: Require(wallet.Balance==startingBalance,"Incomplete PIN changed bankroll"); buttons[7].Pressed.Invoke(); break;
                case 3: buttons[5].Pressed.Invoke(); break;
                case 4: buttons[3].Pressed.Invoke(); break;
                case 5: buttons[1].Pressed.Invoke(); break;
                case 6: serve.Pressed.Invoke(); break;
                case 7: Require(wallet.Balance==startingBalance+500,"Live PIN did not pay $500"); serve.Pressed.Invoke(); break;
                case 8:
                    Require(wallet.Balance==startingBalance+500,"Live repeated press paid again");
                    File.WriteAllText("Temp/BarPuzzleQA/play-check.txt","PASS: MVP_Main loaded casino; real keypad button events entered 8642; wallet increased exactly $500; repeated submission paid nothing.");
                    Debug.Log("[BarPuzzle] PLAY CHECK PASSED"); Finish(); return;
            }
            stage++; next=EditorApplication.timeSinceStartup+.3;
        }
        catch(Exception e) { Debug.LogException(e); File.WriteAllText("Temp/BarPuzzleQA/play-check.txt","FAIL: "+e); Finish(); }
    }

    static FieldInfo Field(string name)=>typeof(BarPuzzle).GetField(name,BindingFlags.NonPublic|BindingFlags.Instance);
    static void Require(bool ok,string message) { if(!ok) throw new Exception(message); }
    static void Finish() { EditorApplication.update-=Tick; EditorApplication.isPlaying=false; }
}
