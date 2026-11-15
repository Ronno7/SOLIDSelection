using System.Collections.Generic;
using UnityEngine;

namespace SolidSelection
{
    [DisallowMultipleComponent]
    [AddComponentMenu("SOLID Selection/Selectable")]
    public sealed class Selectable : MonoBehaviour
    {
        private static readonly HashSet<Selectable> Active = new HashSet<Selectable>();
        public static IEnumerable<Selectable> All => Active;

        [Tooltip("Optional aim point. Otherwise the combined renderer bounds center is used.")]
        [SerializeField] private Transform aimPoint;
        [Tooltip("Renderers affected by selection. Leave empty to discover child renderers.")]
        [SerializeField] private Renderer[] targetRenderers;

        public Renderer[] Renderers
        {
            get
            {
                if (targetRenderers == null || targetRenderers.Length == 0)
                    targetRenderers = GetComponentsInChildren<Renderer>(true);
                return targetRenderers;
            }
        }

        public Vector3 AimPosition
        {
            get
            {
                if (aimPoint != null) return aimPoint.position;
                var bounds = new Bounds(transform.position, Vector3.zero);
                bool found = false;
                foreach (var renderer in Renderers)
                {
                    if (renderer == null) continue;
                    if (!found) { bounds = renderer.bounds; found = true; }
                    else bounds.Encapsulate(renderer.bounds);
                }
                return bounds.center;
            }
        }

        private void OnEnable() => Active.Add(this);
        private void OnDisable() => Active.Remove(this);
        private void OnDestroy() => Active.Remove(this);

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(AimPosition, 0.12f);
        }
    }
}
