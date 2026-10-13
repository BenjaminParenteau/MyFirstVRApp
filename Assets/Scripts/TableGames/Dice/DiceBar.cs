using System.Collections;
using TMPro;
using UnityEngine;

namespace HighStakes.TableGames.Dice
{
    /// <summary>
    /// The 0 to 100 bar lying on the felt: red up to the target, green past it, and a marker that slides to each roll
    /// and turns green or red. Display only; the target is set with the table's - and + buttons.
    /// </summary>
    public sealed class DiceBar : MonoBehaviour
    {
        [SerializeField] Transform trackLose;
        [SerializeField] Transform trackWin;
        [Tooltip("Slides along local X; its Y and Z stay put.")]
        [SerializeField] Transform marker;
        [SerializeField] Renderer markerCube;
        [SerializeField] TMP_Text markerLabel;
        [SerializeField] float length = 0.9f;
        [SerializeField] float slideSeconds = 0.35f;

        [Header("Marker look")]
        [Tooltip("Kit materials swapped onto the marker cube, so it stays on the shared materials.")]
        [SerializeField] Material idleMaterial;
        [SerializeField] Material winMaterial;
        [SerializeField] Material loseMaterial;
        [SerializeField] Color idleColor = Color.white;
        [SerializeField] Color winColor = new Color(0.2f, 0.85f, 0.4f);
        [SerializeField] Color loseColor = new Color(0.9f, 0.2f, 0.2f);

        void Awake()
        {
            Tint(idleMaterial, idleColor);
            markerLabel.text = "";
        }

        public void ShowTarget(int target)
        {
            var v = target / (float)DiceGame.MaxRoll;
            Segment(trackLose, 0f, v);
            Segment(trackWin, v, 1f);
        }

        /// <summary>Slides the marker to the roll with an ease-out, then colours it for the outcome.</summary>
        public IEnumerator SlideMarkerTo(int roll, bool won)
        {
            Tint(idleMaterial, idleColor);
            markerLabel.text = roll.ToString();
            var from = marker.localPosition;
            var to = new Vector3(X(roll / (float)DiceGame.MaxRoll), from.y, from.z);
            for (var t = 0f; t < slideSeconds; t += Time.deltaTime)
            {
                var k = 1f - Mathf.Pow(1f - t / slideSeconds, 3f);
                marker.localPosition = Vector3.LerpUnclamped(from, to, k);
                yield return null;
            }
            marker.localPosition = to;
            if (won) Tint(winMaterial, winColor);
            else Tint(loseMaterial, loseColor);
        }

        float X(float value) => -length / 2f + value * length;

        void Segment(Transform segment, float from, float to)
        {
            var p = segment.localPosition;
            var s = segment.localScale;
            segment.localPosition = new Vector3((X(from) + X(to)) / 2f, p.y, p.z);
            segment.localScale = new Vector3((to - from) * length, s.y, s.z);
        }

        void Tint(Material material, Color color)
        {
            markerCube.sharedMaterial = material;
            markerLabel.color = color;
        }
    }
}
