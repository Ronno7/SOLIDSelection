using UnityEngine;

namespace SolidSelection
{
    [CreateAssetMenu(menuName = "SOLID Selection/Selection Settings", fileName = "Selection Settings")]
    public sealed class SelectionSettings : ScriptableObject
    {
        [Min(0.1f), Tooltip("Maximum selection distance from the ray origin, in metres.")]
        public float maxDistance = 20f;
        [Range(0.1f, 45f), Tooltip("Dot selector cone half-angle. Smaller values require more precise aim.")]
        public float aimAngle = 8f;
        [Tooltip("Layers of selectable roots AND their colliders. Triggers are ignored.")]
        public LayerMask selectableLayers = Physics.DefaultRaycastLayers;
        [Tooltip("Layers that block selection, including selectable objects for mutual occlusion.")]
        public LayerMask blockingLayers = Physics.DefaultRaycastLayers;
        [Tooltip("Dot selection checks a clear path to the target's aim point.")]
        public bool requireLineOfSight = true;

        public bool Allows(Selectable target) => target != null && target.isActiveAndEnabled &&
            (selectableLayers.value & (1 << target.gameObject.layer)) != 0;

        private void OnValidate()
        {
            maxDistance = Mathf.Max(0.1f, maxDistance);
            aimAngle = Mathf.Clamp(aimAngle, 0.1f, 45f);
        }
    }
}
