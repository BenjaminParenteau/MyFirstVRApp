using TMPro;
using UnityEngine;

namespace HighStakes.UI
{
    /// <summary>
    /// One boxed row in a notebook list (items, leads): a circle on the left and a line of text in a pencilled box.
    /// Highlighted rows get an inked box; a ticked row gets its circle filled in; a faded row's text is lighter. Built
    /// by NotebookBuilder; filled by NotebookDisplay and NotebookCaseFile.
    /// </summary>
    public class NotebookRow : MonoBehaviour
    {
        [SerializeField] private TMP_Text label;
        [Tooltip("The inked box, shown on the highlighted row.")]
        [SerializeField] private GameObject inkBox;
        [Tooltip("The dot inside the circle, shown on a ticked row.")]
        [SerializeField] private GameObject tick;
        [SerializeField] private Color inkColor = new Color(0.24f, 0.16f, 0.09f, 1f);
        [SerializeField] private Color fadedColor = new Color(0.24f, 0.16f, 0.09f, 0.45f);

        public void Show(string text, bool highlighted, bool ticked, bool faded)
        {
            gameObject.SetActive(true);
            if (label != null)
            {
                label.text = text;
                label.color = faded ? fadedColor : inkColor;
            }
            if (inkBox != null) inkBox.SetActive(highlighted);
            if (tick != null) tick.SetActive(ticked);
        }

        public void Hide() => gameObject.SetActive(false);
    }
}
