using System;
using System.IO;
using System.Text.RegularExpressions;
using HighStakes.Environment;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class VaultConnectionBuilder
{
    [MenuItem("High Stakes/Vault/Connect Actual Vault Room")]
    public static void Build()
    {
        const string path="Assets/Scenes/MVP/MVP_Vault.unity";
        var scene=SceneManager.GetSceneByPath(path);bool opened=!scene.isLoaded;
        if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
        try
        {
            DoorPortal hall=null,inside=null;
            foreach(var root in scene.GetRootGameObjects())
                foreach(var portal in root.GetComponentsInChildren<DoorPortal>(true))
                {if(portal.portalId=="HallC_End")hall=portal;if(portal.portalId=="RoomC_Door")inside=portal;}
            if(!hall || !inside)throw new InvalidOperationException("Vault doorway anchors missing.");
            var room=inside.transform.parent;
            var destination=hall.transform.rotation*Quaternion.Euler(0,180,0);
            var rotation=destination*Quaternion.Inverse(inside.transform.rotation);
            room.rotation=rotation*room.rotation;
            room.position+=hall.transform.position-inside.transform.position;
            hall.physicalPassage=inside.physicalPassage=true;
            PrefabUtility.RecordPrefabInstancePropertyModifications(hall);
            PrefabUtility.RecordPrefabInstancePropertyModifications(inside);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            Debug.Log("[Vault] Connected actual room at "+room.position+"; one continuous walk-through doorway.");
        }
        finally {if(opened)EditorSceneManager.CloseScene(scene,true);}
        // Visibility baked for the old separated islands hides the room when viewed through the new opening.
        string yaml=File.ReadAllText(path);
        yaml=Regex.Replace(yaml,@"(?m)^  m_OcclusionCullingData:[^\r\n]*","  m_OcclusionCullingData: {fileID: 0}");
        File.WriteAllText(path,yaml);AssetDatabase.ImportAsset(path);
    }
}
