using System.Collections;
using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit;

namespace HighStakes.Environment
{
    /// <summary>
    /// Gives every area something to teleport onto, without a single teleport object being placed in a scene by hand.
    /// Lives in MVP_Main next to <see cref="SceneFlow"/> (<see cref="SoloTestRoot"/> adds one when an area scene is
    /// played on its own) and after each area loads it maps the floor the player can actually stand on: flood out from
    /// the rig's feet over a half-metre grid, keeping cells that have floor under them and room for a standing body
    /// (<see cref="TeleportClearance"/>), then cover those cells with a few rectangles (<see cref="WalkableGridLogic"/>)
    /// and put one <see cref="TeleportSurface"/> on each.
    /// <br/>
    /// Flooding from the player instead of scanning the whole scene keeps the cost to the room they are in and means
    /// closed-off space (the street outside the entrance, the inside of the vault before it opens) is never a target.
    /// </summary>
    public sealed class TeleportSurfaces : MonoBehaviour
    {
        /// <summary>
        /// Physics layer the generated surfaces sit on. The teleport ray's raycast mask already includes layer 31 and
        /// the layer collides with nothing (set up by LocomotionBuilder), so the surfaces are aim-only: they never
        /// block the player, a dropped chip, or another system's raycast.
        /// </summary>
        public const int Layer = 31;

        [Header("Grid")]
        [Tooltip("Size of one grid cell (m). The style kit snaps to 0.5 m, so a cell is one snap step.")]
        [SerializeField] float cellSize = StyleScale.GridSnap;
        [Tooltip("Most cells one area may use. A 12 x 12 m room is 576; the casino floor about 1,500.")]
        [SerializeField] int maxCells = 8000;
        [Tooltip("Cells tested per frame while mapping, so a big room doesn't drop a frame.")]
        [SerializeField] int cellsPerFrame = 400;

        [Header("Player body")]
        [Tooltip("Half the shoulder width to keep clear of walls and furniture (m).")]
        [SerializeField] float bodyRadius = 0.25f;
        [SerializeField] float bodyHeight = StyleScale.FigureHeight;
        [Tooltip("Obstacles lower than this are stepped over, not blocked (matches the rig's step height).")]
        [SerializeField] float stepHeight = StyleScale.MaxStepHeight;
        [Tooltip("How far below the rig's feet a cell's floor may be and still count as the same floor (m).")]
        [SerializeField] float floorDrop = 1.5f;

        [Header("Surfaces")]
        [Tooltip("How far above the floor the surfaces sit (m): enough for the ray to hit them, too little to trip on.")]
        [SerializeField] float surfaceLift = 0.02f;
        [SerializeField] float surfaceThickness = 0.02f;

        public static TeleportSurfaces Instance { get; private set; }

        /// <summary>Rectangles built for the area now loaded. For the play check and the Inspector readout.</summary>
        public int SurfaceCount { get; private set; }

        /// <summary>Walkable cells found in the area now loaded.</summary>
        public int CellCount { get; private set; }

        /// <summary>The area <see cref="SurfaceCount"/> was built for, or null before the first map.</summary>
        public string BuiltFor { get; private set; }

        readonly Dictionary<long, float> walkable = new Dictionary<long, float>();
        readonly Queue<long> pending = new Queue<long>();
        readonly Collider[] overlaps = new Collider[16];
        readonly RaycastHit[] downHits = new RaycastHit[8];

        Transform root;
        XROrigin origin;
        XRInteractionManager manager;
        Coroutine mapping;
        int blockingLayers;
        float gridOriginX, gridOriginZ;
        float nextStrayCheck;

        void Awake()
        {
            Instance = this;
            blockingLayers = TeleportClearance.BlockingLayers();
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void Start() => Remap();

        void Update()
        {
            // Mid-swap the old area is already unloaded and the rig's move to the new door is still queued, so there is
            // nothing worth mapping until SceneFlow is done.
            var flow = SceneFlow.Instance;
            if (flow != null && flow.Busy) return;

            // A new area needs a new map; so does walking somewhere the map doesn't reach (through a door inside the
            // same scene, or past the cell budget).
            string area = flow != null ? flow.CurrentScene : SceneManager.GetActiveScene().name;
            if (area != BuiltFor) { Remap(); return; }
            if (mapping != null || Time.time < nextStrayCheck) return;
            nextStrayCheck = Time.time + 0.5f;
            if (Feet(out var feet) && !walkable.ContainsKey(Key(Cell(feet.x, gridOriginX), Cell(feet.z, gridOriginZ))))
                Remap();
        }

        /// <summary>True when <paramref name="position"/> is over a cell the map found walkable. For the play check.</summary>
        public bool Covers(Vector3 position) =>
            walkable.ContainsKey(Key(Cell(position.x, gridOriginX), Cell(position.z, gridOriginZ)));

        /// <summary>True while an area is still being mapped.</summary>
        public bool Mapping => mapping != null;

        /// <summary>Maps the area the player is standing in again, replacing the surfaces it built last time.</summary>
        public void Remap()
        {
            if (!isActiveAndEnabled) return;
            if (mapping != null) StopCoroutine(mapping);
            mapping = StartCoroutine(Map());
        }

        /// <summary>Maps now rather than over several frames. For the Editor play check; a big area costs a frame.</summary>
        public void RemapImmediate()
        {
            if (mapping != null) { StopCoroutine(mapping); mapping = null; }
            var map = Map(immediate: true);
            while (map.MoveNext()) { }
        }

        IEnumerator Map(bool immediate = false)
        {
            Clear();

            // Nothing to stand on until an area is loaded, and nothing to aim at while the start screen is up.
            var flow = SceneFlow.Instance;
            if (flow != null && (flow.WaitingForStart || flow.CurrentScene == null))
            {
                mapping = null;
                yield break;
            }
            BuiltFor = flow != null ? flow.CurrentScene : SceneManager.GetActiveScene().name;

            if (!Feet(out var feet))
            {
                Debug.LogWarning("[Teleport] No XR Origin in the scene; no teleport surfaces were built.");
                mapping = null;
                yield break;
            }

            gridOriginX = feet.x - cellSize * 0.5f; // the rig's own cell is centred on it
            gridOriginZ = feet.z - cellSize * 0.5f;
            float feetY = feet.y;

            if (!Seed(feet, feetY))
            {
                Debug.LogWarning($"[Teleport] Found no floor under the player in {BuiltFor}; no teleport surfaces were built.");
                mapping = null;
                yield break;
            }

            int tested = 0;
            while (pending.Count > 0 && walkable.Count < maxCells)
            {
                long key = pending.Dequeue();
                int x = KeyX(key), z = KeyZ(key);
                if (walkable.ContainsKey(key)) continue;

                if (Walkable(x, z, feetY, out float floorY))
                {
                    walkable[key] = floorY;
                    pending.Enqueue(Key(x + 1, z));
                    pending.Enqueue(Key(x - 1, z));
                    pending.Enqueue(Key(x, z + 1));
                    pending.Enqueue(Key(x, z - 1));
                }

                if (!immediate && ++tested >= cellsPerFrame)
                {
                    tested = 0;
                    yield return null;
                }
            }

            if (walkable.Count >= maxCells)
                Debug.LogWarning($"[Teleport] {BuiltFor} hit the {maxCells}-cell budget; the far end of the area has no teleport surface.");

            Build();
            mapping = null;
        }

        // Grid cells are keyed by their (x, z) pair so the flood fill doesn't need a sized array up front.
        static long Key(int x, int z) => ((long)x << 32) ^ (uint)z;
        static int KeyX(long key) => (int)(key >> 32);
        static int KeyZ(long key) => (int)(key & 0xFFFFFFFF);

        int Cell(float world, float origin) => WalkableGridLogic.CellOf(world, origin, cellSize);

        bool Feet(out Vector3 feet)
        {
            if (origin == null) origin = FindFirstObjectByType<XROrigin>();
            if (origin == null) { feet = Vector3.zero; return false; }
            feet = origin.transform.position;
            return true;
        }

        /// <summary>
        /// Starts the fill at the player's own cell. Standing against a wall or a table can make that cell fail the
        /// body check, so a few rings around it are tried before giving up.
        /// </summary>
        bool Seed(Vector3 feet, float feetY)
        {
            int cx = Cell(feet.x, gridOriginX), cz = Cell(feet.z, gridOriginZ);
            for (int ring = 0; ring <= 3; ring++)
            {
                for (int dx = -ring; dx <= ring; dx++)
                    for (int dz = -ring; dz <= ring; dz++)
                    {
                        if (ring > 0 && Mathf.Abs(dx) != ring && Mathf.Abs(dz) != ring) continue;
                        if (!Walkable(cx + dx, cz + dz, feetY, out float floorY)) continue;
                        long key = Key(cx + dx, cz + dz);
                        walkable[key] = floorY;
                        pending.Enqueue(Key(cx + dx + 1, cz + dz));
                        pending.Enqueue(Key(cx + dx - 1, cz + dz));
                        pending.Enqueue(Key(cx + dx, cz + dz + 1));
                        pending.Enqueue(Key(cx + dx, cz + dz - 1));
                        return true;
                    }
            }
            return false;
        }

        /// <summary>Is there floor in this cell, with room for a standing body on it?</summary>
        bool Walkable(int x, int z, float feetY, out float floorY)
        {
            floorY = 0f;
            var above = new Vector3(WalkableGridLogic.CellCentre(x, gridOriginX, cellSize), feetY + stepHeight,
                WalkableGridLogic.CellCentre(z, gridOriginZ, cellSize));

            // Cast from just above the rig's feet, so a table top or a shelf overhead is never mistaken for the floor,
            // and take the highest upward-facing surface the ray found.
            int count = Physics.RaycastNonAlloc(above, Vector3.down, downHits, stepHeight + floorDrop,
                blockingLayers, QueryTriggerInteraction.Ignore);
            bool found = false;
            var rig = origin != null ? origin.transform : null;
            for (int i = 0; i < count; i++)
            {
                if (downHits[i].normal.y < 0.9f) continue; // a wall face or a slope too steep to stand on
                if (rig != null && downHits[i].transform.IsChildOf(rig)) continue; // the player's own body
                if (found && downHits[i].point.y <= floorY) continue;
                floorY = downHits[i].point.y;
                found = true;
            }
            if (!found) return false;

            var floor = new Vector3(above.x, floorY, above.z);
            return TeleportClearance.IsClear(floor, bodyRadius, bodyHeight, stepHeight, blockingLayers, overlaps,
                origin != null ? origin.transform : null);
        }

        void Clear()
        {
            walkable.Clear();
            pending.Clear();
            SurfaceCount = 0;
            CellCount = 0;
            if (root != null) Destroy(root.gameObject);
            root = null;
        }

        /// <summary>
        /// One <see cref="TeleportSurface"/> per rectangle. Cells are grouped by floor height first (to the centimetre)
        /// so a step or a threshold never ends up inside one flat surface.
        /// </summary>
        void Build()
        {
            CellCount = walkable.Count;
            if (CellCount == 0) return;

            if (manager == null) manager = FindFirstObjectByType<XRInteractionManager>();
            var holder = new GameObject("Teleport_Surfaces");
            SceneManager.MoveGameObjectToScene(holder, gameObject.scene);
            root = holder.transform;

            var levels = new Dictionary<int, List<long>>();
            foreach (var cell in walkable)
            {
                int level = Mathf.RoundToInt(cell.Value * 100f);
                if (!levels.TryGetValue(level, out var cells)) levels[level] = cells = new List<long>();
                cells.Add(cell.Key);
            }

            foreach (var level in levels)
            {
                int minX = int.MaxValue, minZ = int.MaxValue, maxX = int.MinValue, maxZ = int.MinValue;
                foreach (long key in level.Value)
                {
                    minX = Mathf.Min(minX, KeyX(key));
                    maxX = Mathf.Max(maxX, KeyX(key));
                    minZ = Mathf.Min(minZ, KeyZ(key));
                    maxZ = Mathf.Max(maxZ, KeyZ(key));
                }

                var grid = new bool[maxX - minX + 1, maxZ - minZ + 1];
                foreach (long key in level.Value) grid[KeyX(key) - minX, KeyZ(key) - minZ] = true;

                float y = level.Key / 100f;
                foreach (var rect in WalkableGridLogic.Rectangles(grid))
                    Surface(rect, minX, minZ, y);
            }

            SurfaceCount = root.childCount;
        }

        void Surface(GridRect rect, int minX, int minZ, float floorY)
        {
            var go = new GameObject($"Surface_{SurfaceCount}") { layer = Layer };
            go.transform.SetParent(root, false);
            float centreX = WalkableGridLogic.CellCentre(rect.X + minX, gridOriginX, cellSize) + (rect.Width - 1) * cellSize * 0.5f;
            float centreZ = WalkableGridLogic.CellCentre(rect.Z + minZ, gridOriginZ, cellSize) + (rect.Height - 1) * cellSize * 0.5f;
            go.transform.position = new Vector3(centreX, floorY + surfaceLift, centreZ);

            var box = go.AddComponent<BoxCollider>();
            box.size = new Vector3(rect.Width * cellSize, surfaceThickness, rect.Height * cellSize);

            var surface = go.AddComponent<TeleportSurface>();
            surface.interactionManager = manager;
            surface.interactionLayers = 1 << Layer; // the teleport ray's own interaction layer
            surface.matchOrientation = UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation.MatchOrientation.WorldSpaceUp;
            surface.teleportTrigger = TeleportSurface.TeleportTrigger.OnSelectExited;
            surface.bodyRadius = bodyRadius;
            surface.bodyHeight = bodyHeight;
            surface.stepHeight = stepHeight;
            surface.rig = origin != null ? origin.transform : null;
            SurfaceCount++;
        }
    }
}
