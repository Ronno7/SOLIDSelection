using UnityEngine;

namespace SolidSelection
{
    public interface IRayProvider
    {
        bool TryGetRay(out Ray ray);
    }

    public interface ISelector
    {
        Selectable Select(Ray ray);
    }

    public interface ISelectionResponse
    {
        void OnSelect(Selectable selection);
        void OnDeselect(Selectable selection);
    }

    // Unity serializes the component reference; the Inspector enforces its contract.
    public sealed class RequiresInterfaceAttribute : PropertyAttribute
    {
        public System.Type InterfaceType { get; }
        public RequiresInterfaceAttribute(System.Type interfaceType) => InterfaceType = interfaceType;
    }
}
