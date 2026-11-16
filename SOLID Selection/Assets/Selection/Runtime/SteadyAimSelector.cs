using UnityEngine;

namespace SolidSelection
{
    [AddComponentMenu("SOLID Selection/Steady Aim Selector")]
    public sealed class SteadyAimSelector : MonoBehaviour, ISelector
    {
        [Tooltip("Finds the object to focus on. The demo uses the dot-product selector.")]
        [RequiresInterface(typeof(ISelector)), SerializeField] private MonoBehaviour candidateSelector;
        [Min(0.1f), Tooltip("Keep aiming at the same object for this many seconds to select it.")]
        [SerializeField] private float holdSeconds = 0.6f;

        private Selectable pending;
        private float focusStarted;
        private int lastFrame = -1;
        public float Progress { get; private set; }

        public MonoBehaviour CandidateSelector
        {
            get => candidateSelector;
            set { candidateSelector = value; ResetFocus(); }
        }

        public float HoldSeconds
        {
            get => holdSeconds;
            set { holdSeconds = Mathf.Max(0.1f, value); ResetFocus(); }
        }

        public Selectable Select(Ray ray)
        {
            // A different selection mode (or a lost camera) may have been used in between.
            if (lastFrame >= 0 && Time.frameCount - lastFrame > 1) ResetFocus();
            lastFrame = Time.frameCount;
            if (candidateSelector == null || candidateSelector == this ||
                !candidateSelector.isActiveAndEnabled || !(candidateSelector is ISelector source))
            {
                ResetFocus();
                return null;
            }

            var candidate = source.Select(ray);
            if (candidate == null || !candidate.isActiveAndEnabled)
            {
                ResetFocus();
                return null;
            }
            if (candidate != pending)
            {
                pending = candidate;
                focusStarted = Time.time;
            }
            Progress = Mathf.Clamp01((Time.time - focusStarted) / Mathf.Max(0.1f, holdSeconds));
            return Progress >= 1f ? pending : null;
        }

        private void ResetFocus()
        {
            pending = null;
            lastFrame = -1;
            Progress = 0f;
        }

        private void OnDisable() => ResetFocus();

        private void OnValidate()
        {
            holdSeconds = Mathf.Max(0.1f, holdSeconds);
            ResetFocus();
        }
    }
}
