using UnityEngine;

namespace SolidSelection
{
    [AddComponentMenu("SOLID Selection/Viewport Ray Provider")]
    public sealed class ViewportRayProvider : MonoBehaviour, IRayProvider
    {
        [SerializeField] private Camera selectionCamera;
        public Camera SelectionCamera { get => selectionCamera; set => selectionCamera = value; }

        public bool TryGetRay(out Ray ray)
        {
            if (selectionCamera == null) selectionCamera = Camera.main;
            if (selectionCamera == null || !selectionCamera.isActiveAndEnabled)
            {
                ray = default;
                return false;
            }
            ray = selectionCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            return true;
        }
    }
}
