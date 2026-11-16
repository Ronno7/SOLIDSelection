using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SolidSelection
{
    [DisallowMultipleComponent]
    [AddComponentMenu("SOLID Selection/Selection Response Switcher")]
    public sealed class SelectionResponseSwitcher : MonoBehaviour
    {
        [SerializeField] private SelectionManager manager;
        [Tooltip("Press E to cycle in this order. Empty, disabled, and incompatible entries are skipped.")]
        [RequiresInterface(typeof(ISelectionResponse)), SerializeField]
        private MonoBehaviour[] responses = Array.Empty<MonoBehaviour>();

        public void Configure(SelectionManager selectionManager, params MonoBehaviour[] orderedResponses)
        {
            manager = selectionManager;
            responses = orderedResponses ?? Array.Empty<MonoBehaviour>();
        }

        private void Reset()
        {
            manager = GetComponent<SelectionManager>();
            var available = new List<MonoBehaviour>();
            foreach (var component in GetComponents<MonoBehaviour>())
                if (component is ISelectionResponse) available.Add(component);
            responses = available.ToArray();
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
                CycleNext();
        }

        public bool CycleNext()
        {
            if (manager == null || responses == null || responses.Length == 0) return false;
            // Read the actual response so H/O and Inspector changes stay in sync with cycling.
            var current = manager.ResponseComponent;
            int currentIndex = Array.IndexOf(responses, current);
            for (int step = 1; step <= responses.Length; step++)
            {
                var next = responses[(currentIndex + step) % responses.Length];
                if (next == null || next == current || !next.isActiveAndEnabled ||
                    !(next is ISelectionResponse)) continue;
                manager.SetResponse(next);
                return true;
            }
            return false;
        }
    }
}
