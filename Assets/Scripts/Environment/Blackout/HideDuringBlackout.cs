using UnityEngine;

namespace HighStakes.Environment.Blackout
{
    /// <summary>
    /// Switches this object's renderers off while the power is out, and back on when it returns.
    /// <para>
    /// <b>For the other slots:</b> if you own something that emits light — the watch hologram, the notebook pages,
    /// a monitor, a slot-machine cabinet, the start console — <b>add this component to it</b> and you are done. No
    /// code, no reference to Slot D's files, nothing to coordinate. A glowing watch face in a blackout breaks the
    /// whole mode (StyleGuide §11), and this is the cheapest way to not be the thing that breaks it.
    /// </para>
    /// Put it on the object itself to hide just that, or on a parent with <see cref="includeChildren"/> to hide a
    /// whole assembly. If you need something subtler than off — dimming, or hiding only part of a UI — subscribe
    /// to <see cref="BlackoutMode.Changed"/> from your own component instead.
    /// </summary>
    public class HideDuringBlackout : MonoBehaviour
    {
        [Tooltip("Also hide every renderer underneath this object. On for a whole prop, off for just this one.")]
        [SerializeField] private bool includeChildren = true;

        [Tooltip("Also silence AudioSources on this object. Normally off — a thing that hums should keep humming " +
                 "in the dark, since that is how the player finds it.")]
        [SerializeField] private bool alsoMute;

        Renderer[] renderers;
        AudioSource[] sources;
        bool[] wasEnabled;

        void OnEnable()
        {
            Collect();
            BlackoutMode.Changed += OnBlackoutChanged;
            // If we were spawned (or an area loaded) while it is already dark, start hidden.
            if (BlackoutMode.IsDark) OnBlackoutChanged(true);
        }

        void OnDisable()
        {
            BlackoutMode.Changed -= OnBlackoutChanged;
        }

        void Collect()
        {
            renderers = includeChildren ? GetComponentsInChildren<Renderer>(true) : GetComponents<Renderer>();
            wasEnabled = new bool[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
                wasEnabled[i] = renderers[i] != null && renderers[i].enabled;

            sources = alsoMute
                ? (includeChildren ? GetComponentsInChildren<AudioSource>(true) : GetComponents<AudioSource>())
                : System.Array.Empty<AudioSource>();
        }

        void OnBlackoutChanged(bool dark)
        {
            if (renderers == null) Collect();

            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null) continue;
                // Restore to what it was, not to "on" — something already hidden for its own reasons stays hidden.
                renderers[i].enabled = dark ? false : wasEnabled[i];
            }

            foreach (var source in sources)
                if (source != null) source.mute = dark;
        }
    }
}
