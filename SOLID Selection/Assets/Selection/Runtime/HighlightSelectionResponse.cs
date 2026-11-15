using System.Collections.Generic;
using UnityEngine;

namespace SolidSelection
{
    [AddComponentMenu("SOLID Selection/Highlight Selection Response")]
    public sealed class HighlightSelectionResponse : MonoBehaviour, ISelectionResponse
    {
        [SerializeField] private Material highlightMaterial;
        public Material HighlightMaterial { get => highlightMaterial; set => highlightMaterial = value; }
        private readonly Dictionary<Renderer, Material[]> originals = new Dictionary<Renderer, Material[]>();

        public void OnSelect(Selectable selection)
        {
            Restore();
            if (selection == null || highlightMaterial == null) return;
            foreach (var renderer in selection.Renderers)
            {
                if (renderer == null || originals.ContainsKey(renderer)) continue;
                var materials = renderer.sharedMaterials;
                originals.Add(renderer, materials);
                var highlighted = new Material[materials.Length];
                for (int i = 0; i < highlighted.Length; i++) highlighted[i] = highlightMaterial;
                renderer.sharedMaterials = highlighted;
            }
        }

        public void OnDeselect(Selectable selection) => Restore();
        private void OnDisable() => Restore();

        private void Restore()
        {
            foreach (var entry in originals)
                if (entry.Key != null) entry.Key.sharedMaterials = entry.Value;
            originals.Clear();
        }
    }
}
