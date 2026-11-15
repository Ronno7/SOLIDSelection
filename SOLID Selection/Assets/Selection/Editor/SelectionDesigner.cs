using UnityEditor;
using UnityEngine;

namespace SolidSelection.Editor
{
    [CustomPropertyDrawer(typeof(RequiresInterfaceAttribute))]
    public sealed class InterfaceReferenceDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var requirement = (RequiresInterfaceAttribute)attribute;
            EditorGUI.BeginProperty(position, label, property);
            label.tooltip = "Assign a component implementing " + requirement.InterfaceType.Name;
            var candidate = EditorGUI.ObjectField(position, label, property.objectReferenceValue, typeof(MonoBehaviour), true);
            if (candidate == null || requirement.InterfaceType.IsInstanceOfType(candidate))
                property.objectReferenceValue = candidate;
            else
                Debug.LogWarning($"{candidate.name} must implement {requirement.InterfaceType.Name}.", candidate);
            EditorGUI.EndProperty();
        }
    }

    [CustomEditor(typeof(SelectionManager))]
    public sealed class SelectionManagerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            serializedObject.Update();
            foreach (var name in new[] { "rayProvider", "selector", "selectionResponse" })
                if (serializedObject.FindProperty(name).objectReferenceValue == null)
                    EditorGUILayout.HelpBox("Assign " + name + " before playing.", MessageType.Warning);
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Compare strategies", EditorStyles.boldLabel);
            var manager = (SelectionManager)target;
            using (new EditorGUILayout.HorizontalScope())
            {
                AssignButton<RaycastBasedSelector>(manager, "Raycast", "selector");
                AssignButton<DotProductBasedSelector>(manager, "Dot product", "selector");
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                AssignButton<HighlightSelectionResponse>(manager, "Highlight", "selectionResponse");
                AssignButton<OutlineSelectionResponse>(manager, "Outline", "selectionResponse");
            }
            serializedObject.ApplyModifiedProperties();
            EditorGUILayout.HelpBox("Add Selectable to a prop root. Child colliders resolve to that root. Tune the shared Selection Settings asset on either selector.", MessageType.Info);
        }

        private void AssignButton<T>(SelectionManager manager, string text, string property) where T : MonoBehaviour
        {
            var component = manager.GetComponent<T>();
            using (new EditorGUI.DisabledScope(component == null))
                if (GUILayout.Button(text)) serializedObject.FindProperty(property).objectReferenceValue = component;
        }
    }

    [CustomEditor(typeof(DotProductBasedSelector))]
    public sealed class DotProductSelectorEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var selector = (DotProductBasedSelector)target;
            if (selector.Settings == null) EditorGUILayout.HelpBox("Assign a Selection Settings asset.", MessageType.Warning);
            else EditorGUILayout.HelpBox($"Minimum dot score: {Mathf.Cos(selector.Settings.aimAngle * Mathf.Deg2Rad):0.0000}. Select this rig to see its aim cone in Scene view.", MessageType.Info);
        }

        private void OnSceneGUI()
        {
            var selector = (DotProductBasedSelector)target;
            var provider = selector.GetComponent<ViewportRayProvider>();
            if (selector.Settings == null || provider == null || !provider.TryGetRay(out var ray)) return;
            var settings = selector.Settings;
            var forward = ray.direction.normalized;
            var right = Vector3.Cross(forward, Vector3.up).normalized;
            if (right.sqrMagnitude < 0.001f) right = Vector3.right;
            var up = Vector3.Cross(right, forward).normalized;
            float angle = settings.aimAngle * Mathf.Deg2Rad;
            float radius = Mathf.Sin(angle) * settings.maxDistance;
            var center = ray.origin + forward * (Mathf.Cos(angle) * settings.maxDistance);
            Handles.color = new Color(0.2f, 1f, 0.8f, 0.65f);
            Handles.DrawWireDisc(center, forward, radius);
            foreach (var axis in new[] { right, -right, up, -up }) Handles.DrawLine(ray.origin, center + axis * radius);
        }
    }

    public static class SelectionDesigner
    {
        [MenuItem("Tools/SOLID Selection/Make Selected Objects Selectable")]
        private static void MakeSelectable()
        {
            foreach (var item in UnityEditor.Selection.gameObjects)
                if (item.scene.IsValid() && item.GetComponent<Selectable>() == null)
                    Undo.AddComponent<Selectable>(item);
        }

        [MenuItem("Tools/SOLID Selection/Make Selected Objects Selectable", true)]
        private static bool CanMakeSelectable() => UnityEditor.Selection.gameObjects.Length > 0;
    }
}
