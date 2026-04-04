using UnityEngine;

namespace NeonSerpent.UI
{
    /// <summary>
    /// Repositions this RectTransform to respect the device's safe area so that
    /// UI content is never obscured by notches, rounded corners, or system bars.
    /// Attach to the root panel of any HUD or menu canvas that contains interactive
    /// or important content near screen edges.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class SafeAreaPanel : MonoBehaviour
    {
        private RectTransform _rectTransform;
        private Rect          _lastSafeArea;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            ApplySafeArea();
        }

#if UNITY_EDITOR
        // Re-apply every frame in the Editor so the Simulator device changes are reflected
        // immediately without requiring a re-enter of Play mode.
        private void Update()
        {
            if (Screen.safeArea != _lastSafeArea)
                ApplySafeArea();
        }
#endif

        /// <summary>
        /// Reads <see cref="Screen.safeArea"/>, converts it to normalised anchor values,
        /// and applies them to the attached RectTransform.
        /// </summary>
        public void ApplySafeArea()
        {
            Rect safeArea  = Screen.safeArea;
            _lastSafeArea  = safeArea;

            Vector2 screenSize = new Vector2(Screen.width, Screen.height);

            // Guard against a zero screen size during unit tests or very early init.
            if (screenSize.x <= 0f || screenSize.y <= 0f) return;

            Vector2 anchorMin = safeArea.position / screenSize;
            Vector2 anchorMax = (safeArea.position + safeArea.size) / screenSize;

            // Clamp to [0,1] to protect against edge cases on some device simulators.
            anchorMin.x = Mathf.Clamp01(anchorMin.x);
            anchorMin.y = Mathf.Clamp01(anchorMin.y);
            anchorMax.x = Mathf.Clamp01(anchorMax.x);
            anchorMax.y = Mathf.Clamp01(anchorMax.y);

            _rectTransform.anchorMin = anchorMin;
            _rectTransform.anchorMax = anchorMax;
            _rectTransform.offsetMin = Vector2.zero;
            _rectTransform.offsetMax = Vector2.zero;
        }
    }
}
