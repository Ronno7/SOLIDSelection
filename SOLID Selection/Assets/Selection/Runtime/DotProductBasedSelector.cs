using UnityEngine;

namespace SolidSelection
{
    [AddComponentMenu("SOLID Selection/Dot Product Based Selector")]
    public sealed class DotProductBasedSelector : MonoBehaviour, ISelector
    {
        [SerializeField] private SelectionSettings settings;
        public SelectionSettings Settings { get => settings; set => settings = value; }

        public Selectable Select(Ray ray)
        {
            if (settings == null || ray.direction.sqrMagnitude < 0.000001f) return null;
            Vector3 forward = ray.direction.normalized;
            float minimumDot = Mathf.Cos(settings.aimAngle * Mathf.Deg2Rad);
            float bestDot = -1f;
            float bestDistance = float.PositiveInfinity;
            Selectable best = null;
            foreach (var candidate in Selectable.All)
            {
                if (!settings.Allows(candidate)) continue;
                Vector3 offset = candidate.AimPosition - ray.origin;
                float distance = offset.magnitude;
                if (distance < 0.0001f || distance > settings.maxDistance) continue;
                // Both operands are unit vectors: score = cos(angle), independent of distance.
                float dot = Vector3.Dot(forward, offset / distance);
                if (dot < minimumDot) continue;
                if (settings.requireLineOfSight && Physics.Raycast(ray.origin, offset / distance,
                        out var hit, distance, settings.blockingLayers, QueryTriggerInteraction.Ignore) &&
                    hit.collider.GetComponentInParent<Selectable>() != candidate) continue;

                bool tied = Mathf.Abs(dot - bestDot) <= 0.000001f;
                if (dot > bestDot + 0.000001f || (tied && (distance < bestDistance ||
                    (Mathf.Approximately(distance, bestDistance) &&
                     (best == null || candidate.GetInstanceID() < best.GetInstanceID())))))
                {
                    best = candidate;
                    bestDot = dot;
                    bestDistance = distance;
                }
            }
            return best;
        }
    }
}
