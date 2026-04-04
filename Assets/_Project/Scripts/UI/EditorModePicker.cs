using UnityEngine;
using UnityEngine.UI;
using TMPro;
using NeonSerpent.Core;

namespace NeonSerpent.UI
{
    /// <summary>
    /// Editor-only mode picker. Shown in the Game scene when Bootstrap has not been loaded
    /// (i.e., the developer pressed Play directly from the Game scene).
    /// Hidden automatically in full Bootstrap flow where GameManager already exists.
    /// </summary>
    public class EditorModePicker : MonoBehaviour
    {
        [SerializeField] private GameObject _panel;
        [SerializeField] private Button     _classicBtn;
        [SerializeField] private Button     _timeAttackBtn;
        [SerializeField] private Button     _campaignBtn;

        private void Start()
        {
            // If Bootstrap is loaded GameManager exists — hide picker and do nothing
            if (GameManager.Instance != null)
            {
                if (_panel != null) _panel.SetActive(false);
                Destroy(this);
                return;
            }

            if (_panel != null) _panel.SetActive(true);
            _classicBtn?.onClick.AddListener(   () => Pick(GameMode.ClassicEndless));
            _timeAttackBtn?.onClick.AddListener(() => Pick(GameMode.TimeAttack));
            _campaignBtn?.onClick.AddListener(  () => Pick(GameMode.Campaign));
        }

        private void Pick(GameMode mode)
        {
            if (_panel != null) _panel.SetActive(false);

            // Pass the chosen mode so StartSessionInternal uses the correct level config
            // (Time Attack needs _timeAttackLevel; Campaign needs _selectedLevel).
            var session = FindFirstObjectByType<GameSession>();
            session?.BeginEditorSession(mode);
        }
    }
}
