using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

namespace HighStakes.Environment
{
    // Reject floor landings that would place the player's body inside furniture.
    public class CasinoTeleportationArea : TeleportationArea
    {
        readonly Collider[] overlaps = new Collider[32];

        protected override bool GenerateTeleportRequest(IXRInteractor interactor,
            RaycastHit hit, ref TeleportRequest request)
        {
            if (!base.GenerateTeleportRequest(interactor, hit, ref request)) return false;
            const float radius = 0.25f;
            var bottom = hit.point + Vector3.up * (radius + 0.05f);
            var top = hit.point + Vector3.up * (1.8f - radius);
            int count = Physics.OverlapCapsuleNonAlloc(bottom, top, radius, overlaps,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            if (count == overlaps.Length) return false;
            for (int i = 0; i < count; i++)
                if (overlaps[i] != hit.collider) return false;
            return true;
        }
    }
}
