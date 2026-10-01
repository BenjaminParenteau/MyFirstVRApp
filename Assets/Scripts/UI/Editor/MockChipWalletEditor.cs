using HighStakes.UI.Mocks;
using UnityEditor;
using UnityEngine;

/// <summary>Play-mode buttons on MockChipWallet so UI can be tested without the real economy.</summary>
[CustomEditor(typeof(MockChipWallet))]
public class MockChipWalletEditor : Editor
{
    public override bool RequiresConstantRepaint() => Application.isPlaying;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var wallet = (MockChipWallet)target;
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Balance", Application.isPlaying ? wallet.Balance.ToString("N0") : "(enter Play mode)");

        using (new EditorGUI.DisabledScope(!Application.isPlaying))
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                foreach (int amount in new[] { 100, 1000, 5000 })
                {
                    if (GUILayout.Button($"+{amount:N0}"))
                        wallet.Add(amount);
                }
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                foreach (int amount in new[] { 100, 1000, 5000 })
                {
                    if (GUILayout.Button($"-{amount:N0}"))
                        wallet.TrySpend(Mathf.Min(amount, wallet.Balance));
                }
            }
        }
    }
}
