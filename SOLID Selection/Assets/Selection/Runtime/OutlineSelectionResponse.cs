using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace SolidSelection
{
    [AddComponentMenu("SOLID Selection/Outline Selection Response")]
    public sealed class OutlineSelectionResponse : MonoBehaviour, ISelectionResponse
    {
        [Tooltip("Use the supplied URP Selection Outline material. Supports static MeshRenderers.")]
        [SerializeField] private Material outlineMaterial;
        [SerializeField] private Color outlineColor = new Color(0.25f, 1f, 0.8f);
        [Min(0.001f), SerializeField] private float width = 0.045f;
        public Material OutlineMaterial { get => outlineMaterial; set => outlineMaterial = value; }
        private readonly List<GameObject> shells = new List<GameObject>();

        public void OnSelect(Selectable selection)
        {
            Clear();
            if (selection == null || outlineMaterial == null) return;
            foreach (var source in selection.Renderers)
            {
                if (!(source is MeshRenderer) || !source.enabled) continue;
                var filter = source.GetComponent<MeshFilter>();
                if (filter == null || filter.sharedMesh == null) continue;
                var shell = new GameObject("Selection outline") { hideFlags = HideFlags.DontSave, layer = source.gameObject.layer };
                shell.transform.SetParent(source.transform, false);
                shell.AddComponent<MeshFilter>().sharedMesh = filter.sharedMesh;
                var renderer = shell.AddComponent<MeshRenderer>();
                var materials = new Material[filter.sharedMesh.subMeshCount];
                for (int i = 0; i < materials.Length; i++) materials[i] = outlineMaterial;
                renderer.sharedMaterials = materials;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                var properties = new MaterialPropertyBlock();
                properties.SetColor("_OutlineColor", outlineColor);
                properties.SetFloat("_OutlineWidth", width);
                properties.SetVector("_OutlineCenter", filter.sharedMesh.bounds.center);
                renderer.SetPropertyBlock(properties);
                shells.Add(shell);
            }
        }

        public void OnDeselect(Selectable selection) => Clear();
        private void OnDisable() => Clear();

        private void Clear()
        {
            foreach (var shell in shells)
            {
                if (shell == null) continue;
                shell.SetActive(false);
                if (Application.isPlaying) Destroy(shell);
                else DestroyImmediate(shell);
            }
            shells.Clear();
        }
    }
}
