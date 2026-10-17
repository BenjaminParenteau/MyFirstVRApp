using HighStakes.UI.Mocks;
using UnityEditor;
using UnityEngine;

/// <summary>Play-mode buttons on MockInventoryEvents so the notebook can be tested without the real inventory.</summary>
[CustomEditor(typeof(MockInventoryEvents))]
public class MockInventoryEventsEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var mock = (MockInventoryEvents)target;
        EditorGUILayout.Space();
        using (new EditorGUI.DisabledScope(!Application.isPlaying))
        {
            foreach (var item in mock.SampleItems ?? new HighStakes.Core.ItemData[0])
            {
                if (item != null && GUILayout.Button("Pick up: " + item.displayName))
                    mock.PickUp(item);
            }
        }
        if (!Application.isPlaying)
            EditorGUILayout.HelpBox("Enter Play mode to pick up items.", MessageType.Info);
    }
}
