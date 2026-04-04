using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

namespace NeonSerpent.UI
{
    /// <summary>
    /// Adds neon hover glow and press punch-scale to a main-menu Button.
    /// Attach alongside a Button component on each menu button.
    /// Uses the EventSystem pointer interfaces — works with the new Input System.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class MenuButtonAnimator : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler,
        IPointerDownHandler,  IPointerUpHandler
    {
        [SerializeField] private Color _normalColor = new Color(0.75f, 0.75f, 0.75f, 1f);
        [SerializeField] private Color _hoverColor  = Color.white;
        [SerializeField] private float _hoverScale  = 1.08f;
        [SerializeField] private float _pressScale  = 0.94f;
        [SerializeField] private float _animSpeed   = 14f;

        private RectTransform _rect;
        private TMP_Text      _label;
        private Coroutine     _scaleCoroutine;
        private bool          _isHovered;

        private void Awake()
        {
            _rect  = GetComponent<RectTransform>();
            _label = GetComponentInChildren<TMP_Text>();
        }

        private void OnEnable()
        {
            _isHovered = false;
            if (_rect  != null) _rect.localScale  = Vector3.one;
            if (_label != null) _label.color = _normalColor;
        }

        private void OnDisable()
        {
            if (_scaleCoroutine != null)
            {
                StopCoroutine(_scaleCoroutine);
                _scaleCoroutine = null;
            }
            if (_rect  != null) _rect.localScale  = Vector3.one;
            if (_label != null) _label.color = _normalColor;
        }

        public void OnPointerEnter(PointerEventData _)
        {
            _isHovered = true;
            if (_label != null) _label.color = _hoverColor;
            AnimateTo(_hoverScale);
        }

        public void OnPointerExit(PointerEventData _)
        {
            _isHovered = false;
            if (_label != null) _label.color = _normalColor;
            AnimateTo(1f);
        }

        public void OnPointerDown(PointerEventData _) => AnimateTo(_pressScale);

        public void OnPointerUp(PointerEventData _) => AnimateTo(_isHovered ? _hoverScale : 1f);

        private void AnimateTo(float target)
        {
            if (_scaleCoroutine != null) StopCoroutine(_scaleCoroutine);
            _scaleCoroutine = StartCoroutine(ScaleRoutine(target));
        }

        private IEnumerator ScaleRoutine(float target)
        {
            while (!Mathf.Approximately(_rect.localScale.x, target))
            {
                float s = Mathf.Lerp(_rect.localScale.x, target, Time.unscaledDeltaTime * _animSpeed);
                _rect.localScale = new Vector3(s, s, 1f);
                yield return null;
            }
            _rect.localScale = new Vector3(target, target, 1f);
            _scaleCoroutine  = null;
        }
    }
}
