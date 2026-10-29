using System.Collections.Generic;
using UnityEngine;

namespace HighStakes.Security
{
    /// <summary>Door leaf swings on its left hinge; the heavy armored surround stays on the wall.</summary>
    public sealed class VaultDoorMotion : MonoBehaviour
    {
        public Transform Leaf { get; private set; }
        public float OpenFraction { get; private set; }

        public void Prepare()
        {
            if (Leaf) return;
            var children = new List<Transform>();
            foreach (Transform child in transform) children.Add(child);
            Leaf = new GameObject("Hinged steel leaf").transform;
            Leaf.SetParent(transform, false);Leaf.localPosition = new Vector3(-1.38f, 0, 0);
            foreach (var child in children)
                if (!child.name.StartsWith("Armored door surround") && child.name != "Marking MERIDIAN RESERVE") child.SetParent(Leaf, true);
        }

        void Update()
        {
            if (!Leaf) return;
            OpenFraction = Mathf.MoveTowards(OpenFraction, VaultKeySession.Lock.IsOpen ? 1 : 0, Time.deltaTime / 2.4f);
            Leaf.localRotation = Quaternion.Euler(0, -100 * Mathf.SmoothStep(0, 1, OpenFraction), 0);
        }
    }
}
