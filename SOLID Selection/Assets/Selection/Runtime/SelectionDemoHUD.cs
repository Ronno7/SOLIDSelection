using UnityEngine;
using UnityEngine.InputSystem;

namespace SolidSelection
{
    /// <summary>Demo controls only. The selection manager has no dependency on this UI.</summary>
    public sealed class SelectionDemoHUD : MonoBehaviour
    {
        public SelectionManager manager;
        public RaycastBasedSelector raycastSelector;
        public DotProductBasedSelector dotSelector;
        public SteadyAimSelector steadyAimSelector;
        public HighlightSelectionResponse highlightResponse;
        public OutlineSelectionResponse outlineResponse;
        private GUIStyle label;
        private GUIStyle crosshair;

        private void Update()
        {
            if (Keyboard.current == null || manager == null) return;
            var keys = Keyboard.current;
            if (keys.digit1Key.wasPressedThisFrame && raycastSelector != null) manager.SetSelector(raycastSelector);
            if (keys.digit2Key.wasPressedThisFrame && dotSelector != null) manager.SetSelector(dotSelector);
            if (keys.digit3Key.wasPressedThisFrame && steadyAimSelector != null) manager.SetSelector(steadyAimSelector);
            if (keys.hKey.wasPressedThisFrame && highlightResponse != null) manager.SetResponse(highlightResponse);
            if (keys.oKey.wasPressedThisFrame && outlineResponse != null) manager.SetResponse(outlineResponse);
        }

        private void OnGUI()
        {
            if (manager == null) return;
            if (label == null)
            {
                label = new GUIStyle(GUI.skin.label) { fontSize = 16 };
                label.normal.textColor = Color.white;
                crosshair = new GUIStyle(label) { fontSize = 20, alignment = TextAnchor.MiddleCenter };
            }
            var oldMatrix = GUI.matrix;
            float scale = Mathf.Max(0.1f, Mathf.Min(Screen.width / 1280f, Screen.height / 720f));
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scale, scale, 1f));
            float w = Screen.width / scale;
            float h = Screen.height / scale;
            string selection = manager.SelectorComponent == dotSelector ? "Dot product" : "Raycast";
            if (steadyAimSelector != null && manager.SelectorComponent == steadyAimSelector)
                selection = $"Steady aim ({steadyAimSelector.Progress * 100f:0}%)";
            string display = manager.ResponseComponent == outlineResponse ? "Outline" : "Highlight";
            string selected = manager.Current != null ? manager.Current.name : "None";
            DrawText(new Rect(12, 12, 760, 24), $"Selection: {selection}    (1: Raycast, 2: Dot product, 3: Steady aim)", label);
            DrawText(new Rect(12, 36, 650, 24), $"Display: {display}    (E: next, H: Highlight, O: Outline)", label);
            DrawText(new Rect(12, 60, 500, 24), $"Selected: {selected}", label);

            DrawText(new Rect(w * 0.5f - 12, h * 0.5f - 12, 24, 24), "+", crosshair);
            if (Cursor.lockState != CursorLockMode.Locked)
                DrawText(new Rect(12, h - 58, w - 24, 24), "Click to look around.", label);
            DrawText(new Rect(12, h - 32, w - 24, 24),
                "WASD: move    Mouse: look    Shift: run    R: reset    Esc: free mouse", label);
            GUI.matrix = oldMatrix;
        }

        private static void DrawText(Rect rect, string text, GUIStyle style)
        {
            var previous = GUI.color;
            // A small shadow keeps plain text readable against the scene.
            GUI.color = Color.black;
            GUI.Label(new Rect(rect.x + 1, rect.y + 1, rect.width, rect.height), text, style);
            GUI.color = Color.white;
            GUI.Label(rect, text, style);
            GUI.color = previous;
        }
    }
}
