using UnityEngine;

namespace SolidSelection
{
    [AddComponentMenu("SOLID Selection/Raycast Based Selector")]
    public sealed class RaycastBasedSelector : MonoBehaviour, ISelector
    {
        [SerializeField] private SelectionSettings settings;
        public SelectionSettings Settings { get => settings; set => settings = value; }

        public Selectable Select(Ray ray)
        {
            if (settings == null || ray.direction.sqrMagnitude < 0.000001f) return null;
            // Include blockers so a wall cannot be skipped by a selectable-only mask.
            int mask = settings.selectableLayers.value | settings.blockingLayers.value;
            if (!Physics.Raycast(ray, out var hit, settings.maxDistance, mask, QueryTriggerInteraction.Ignore))
                return null;
            var candidate = hit.collider.GetComponentInParent<Selectable>();
            return settings.Allows(candidate) ? candidate : null;
        }
    }
}
