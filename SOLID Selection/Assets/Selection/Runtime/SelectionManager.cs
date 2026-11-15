using UnityEngine;

namespace SolidSelection
{
    /// <summary>Coordinates transitions; knows nothing about input, physics, or materials.</summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("SOLID Selection/Selection Manager")]
    public sealed class SelectionManager : MonoBehaviour
    {
        [RequiresInterface(typeof(IRayProvider)), SerializeField] private MonoBehaviour rayProvider;
        [RequiresInterface(typeof(ISelector)), SerializeField] private MonoBehaviour selector;
        [RequiresInterface(typeof(ISelectionResponse)), SerializeField] private MonoBehaviour selectionResponse;

        private MonoBehaviour activeResponseComponent;
        private ISelectionResponse activeResponse;
        private Selectable current;
        public Selectable Current => current;
        public MonoBehaviour SelectorComponent => selector;
        public MonoBehaviour ResponseComponent => selectionResponse;

        public void Configure(MonoBehaviour provider, MonoBehaviour selectionStrategy, MonoBehaviour response)
        {
            ClearSelection();
            rayProvider = provider;
            selector = selectionStrategy;
            selectionResponse = response;
        }

        public void SetSelector(MonoBehaviour strategy)
        {
            if (!(strategy is ISelector)) throw new System.ArgumentException("Component must implement ISelector.");
            ClearSelection();
            selector = strategy;
        }

        public void SetResponse(MonoBehaviour response)
        {
            if (!(response is ISelectionResponse)) throw new System.ArgumentException("Component must implement ISelectionResponse.");
            ClearSelection();
            selectionResponse = response;
        }

        private void LateUpdate() => RefreshSelection();

        public void RefreshSelection()
        {
            if (!isActiveAndEnabled || rayProvider == null || !rayProvider.isActiveAndEnabled ||
                selector == null || !selector.isActiveAndEnabled || selectionResponse == null ||
                !selectionResponse.isActiveAndEnabled || !(rayProvider is IRayProvider rays) ||
                !(selector is ISelector selections) || !(selectionResponse is ISelectionResponse response))
            {
                ClearSelection();
                return;
            }

            // Inspector changes during play must clean up the previous response too.
            if (activeResponseComponent != selectionResponse) ClearSelection();
            activeResponseComponent = selectionResponse;
            activeResponse = response;
            var next = rays.TryGetRay(out var ray) ? selections.Select(ray) : null;
            if (next == null || !next.isActiveAndEnabled) next = null;
            if (ReferenceEquals(next, current)) return;
            ClearSelection();
            activeResponseComponent = selectionResponse;
            activeResponse = response;
            current = next;
            if (current != null) activeResponse.OnSelect(current);
        }

        public void ClearSelection()
        {
            // ReferenceEquals also catches a Unity object destroyed since the last frame.
            if (!ReferenceEquals(current, null) && activeResponseComponent != null)
                activeResponse?.OnDeselect(current);
            current = null;
            activeResponse = null;
            activeResponseComponent = null;
        }

        private void OnDisable() => ClearSelection();
    }
}
