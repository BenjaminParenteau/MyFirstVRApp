using UnityEngine;

namespace HighStakes.Environment
{
    /// <summary>
    /// Fast travel from the watch: once an area has been reached on foot, the player can jump straight back to it
    /// instead of walking the hallways again. Lives in MVP_Main next to <see cref="SceneFlow"/>, which does the
    /// actual work — this only decides whether a jump is allowed and which door to land at.
    /// <br/>
    /// It is deliberately not a locomotion provider and adds nothing to any scene (StyleGuide §10): a jump is the
    /// same faded scene swap a <see cref="DoorPortal"/> performs, so it inherits the fade, the arrival placement and
    /// the <see cref="TeleportSurfaces"/> remap that follows, and there is still exactly one way to move through the
    /// world. The rules are in <see cref="FastTravelLogic"/>; the watch UI only displays what it reports.
    /// </summary>
    public sealed class FastTravel : MonoBehaviour
    {
        [Tooltip("Master switch. Off means the watch's travel page is hidden and nothing can jump — used to hold the " +
                 "feature back for a user-testing round that is measuring walking.")]
        [SerializeField] bool allowed = true;

        [Tooltip("Smallest gap between two jumps (s), so a double press can't start a second swap on top of the first.")]
        [SerializeField] float cooldown = 1f;

        public static FastTravel Instance { get; private set; }

        /// <summary>Why the last press did nothing, or null if it travelled. For the watch's reply line.</summary>
        public string LastRefusal { get; private set; }

        float nextAllowed;

        /// <summary>Is the travel page available at all? False also while the start screen is up or a swap is running.</summary>
        public bool Available
        {
            get
            {
                if (!allowed) return false;
                var flow = SceneFlow.Instance;
                return flow != null && !flow.WaitingForStart && !flow.Busy;
            }
        }

        public int DestinationCount => FastTravelLogic.Destinations.Length;

        public TravelDestination DestinationAt(int index) => FastTravelLogic.Destinations[index];

        void Awake() => Instance = this;

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>What the watch should show for this row right now.</summary>
        public TravelState StateOf(int index)
        {
            var flow = SceneFlow.Instance;
            if (flow == null || index < 0 || index >= FastTravelLogic.Destinations.Length) return TravelState.Unknown;

            var destination = FastTravelLogic.Destinations[index];
            bool hasAccess = destination.RequiredAccess == null ||
                             HighStakes.Inventory.KeycardAccess.Has(destination.RequiredAccess);
            return FastTravelLogic.StateOf(destination, flow.HasVisited(destination.Scene), hasAccess,
                flow.CurrentScene);
        }

        /// <summary>
        /// Jumps to the destination on row <paramref name="index"/>, if it is allowed. Returns false and sets
        /// <see cref="LastRefusal"/> otherwise, so a press on a locked row can say why instead of failing silently.
        /// </summary>
        public bool TravelTo(int index)
        {
            LastRefusal = null;
            if (index < 0 || index >= FastTravelLogic.Destinations.Length) return false;

            if (!allowed)
            {
                LastRefusal = "UNAVAILABLE";
                return false;
            }

            var flow = SceneFlow.Instance;
            if (flow == null || flow.WaitingForStart || flow.Busy || Time.time < nextAllowed)
            {
                LastRefusal = "STAND BY";
                return false;
            }

            var state = StateOf(index);
            if (!FastTravelLogic.CanTravel(state))
            {
                LastRefusal = FastTravelLogic.RefusalFor(state);
                return false;
            }

            var destination = FastTravelLogic.Destinations[index];
            if (!flow.Go(destination.Scene, destination.ArrivalDoor))
            {
                LastRefusal = "STAND BY";
                return false;
            }

            nextAllowed = Time.time + cooldown;
            return true;
        }

        /// <summary>Jumps to an area by scene name. False if it isn't a destination or isn't allowed yet.</summary>
        public bool TravelTo(string scene) => TravelTo(FastTravelLogic.IndexOf(scene));
    }
}
