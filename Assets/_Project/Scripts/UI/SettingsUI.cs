using UnityEngine;
using UnityEngine.UI;
using TMPro;
using NeonSerpent.Ads;
using NeonSerpent.Audio;
using NeonSerpent.Core;
using NeonSerpent.SaveData;
using NeonSerpent.Utilities;

namespace NeonSerpent.UI
{
    /// <summary>
    /// Settings scene root UI controller.
    /// Manages SFX/music volume sliders, a vibration toggle, and a danger-zone
    /// "Delete Save" flow that requires explicit confirmation before wiping data.
    /// All volume changes are immediately applied to <see cref="AudioManager"/> and
    /// persisted to PlayerPrefs by that class.
    /// </summary>
    public class SettingsUI : MonoBehaviour
    {
        // -------------------------------------------------------------------------
        #region Inspector Fields

        [Header("Audio")]
        [SerializeField] private Slider _sfxSlider;
        [SerializeField] private Slider _musicSlider;

        [Header("Vibration")]
        [SerializeField] private Toggle _vibrationToggle;

        [Header("Navigation")]
        [SerializeField] private Button _backBtn;

        [Header("Delete Save")]
        [SerializeField] private Button     _deleteSaveBtn;
        [SerializeField] private GameObject _confirmDeletePanel;
        [SerializeField] private Button     _confirmYesBtn;
        [SerializeField] private Button     _confirmNoBtn;

        #endregion
        // -------------------------------------------------------------------------
        #region Unity Lifecycle

        private void Start()
        {
            LoadAndApplyStoredValues();
            WireListeners();
        }

        #endregion
        // -------------------------------------------------------------------------
        #region Initialization

        /// <summary>
        /// Reads persisted settings from PlayerPrefs and pushes them to each control.
        /// Listener registration happens afterwards so setting initial values does not
        /// trigger the value-changed callbacks during Start.
        /// </summary>
        private void LoadAndApplyStoredValues()
        {
            float sfxVolume   = PlayerPrefs.GetFloat("sfx_volume",   1f);
            float musicVolume = PlayerPrefs.GetFloat("music_volume", 0.7f);
            bool  vibration   = SaveManager.Instance?.Data?.vibrationEnabled ?? true;

            if (_sfxSlider   != null) _sfxSlider.value   = sfxVolume;
            if (_musicSlider != null) _musicSlider.value  = musicVolume;
            if (_vibrationToggle != null) _vibrationToggle.isOn = vibration;

            // Hide confirm panel on load.
            if (_confirmDeletePanel != null) _confirmDeletePanel.SetActive(false);
        }

        /// <summary>
        /// Wires all button and slider listeners. Called after initial values are set
        /// so that programmatic assignments in <see cref="LoadAndApplyStoredValues"/>
        /// do not fire side-effect callbacks.
        /// </summary>
        private void WireListeners()
        {
            _sfxSlider?.onValueChanged.AddListener(OnSFXSliderChanged);
            _musicSlider?.onValueChanged.AddListener(OnMusicSliderChanged);
            _vibrationToggle?.onValueChanged.AddListener(OnVibrationToggleChanged);

            _backBtn?.onClick.AddListener(OnBackBtn);
            _deleteSaveBtn?.onClick.AddListener(OnDeleteSave);
            _confirmYesBtn?.onClick.AddListener(OnConfirmYes);
            _confirmNoBtn?.onClick.AddListener(OnConfirmNo);
        }

        #endregion
        // -------------------------------------------------------------------------
        #region Slider / Toggle Handlers

        /// <summary>
        /// Applies the SFX volume to <see cref="AudioManager"/> and persists it.
        /// AudioManager.SetSFXVolume() handles the PlayerPrefs write.
        /// </summary>
        /// <param name="value">New volume in the range [0, 1].</param>
        private void OnSFXSliderChanged(float value)
        {
            AudioManager.Instance?.SetSFXVolume(value);
        }

        /// <summary>
        /// Applies the music volume to <see cref="AudioManager"/> and persists it.
        /// AudioManager.SetMusicVolume() handles the PlayerPrefs write.
        /// </summary>
        /// <param name="value">New volume in the range [0, 1].</param>
        private void OnMusicSliderChanged(float value)
        {
            AudioManager.Instance?.SetMusicVolume(value);
        }

        /// <summary>
        /// Persists the vibration preference and fires a brief test pulse on Android.
        /// </summary>
        /// <param name="isOn">True to enable vibration, false to disable.</param>
        private void OnVibrationToggleChanged(bool isOn)
        {
            if (SaveManager.Instance?.Data != null)
            {
                SaveManager.Instance.Data.vibrationEnabled = isOn;
                SaveManager.Instance.Save();
            }

#if UNITY_ANDROID
            if (isOn)
                Handheld.Vibrate();
#endif
        }

        #endregion
        // -------------------------------------------------------------------------
        #region Delete Save Handlers

        /// <summary>Shows the confirmation panel before allowing a save delete.</summary>
        private void OnDeleteSave()
        {
            if (_confirmDeletePanel != null)
                _confirmDeletePanel.SetActive(true);
        }

        /// <summary>
        /// Confirmed: wipes the save file, resets PlayerData, and returns to the Main Menu.
        /// </summary>
        private void OnConfirmYes()
        {
            if (_confirmDeletePanel != null)
                _confirmDeletePanel.SetActive(false);

            SaveManager.Instance?.DeleteSave();

            AdManager.Instance?.OnReturnToMenu();
            SceneLoader.Instance?.LoadScene(Constants.SCENE_MAIN_MENU,
                () => GameManager.Instance?.GoToMainMenu());
        }

        /// <summary>Dismissed: hides the confirmation panel without taking any action.</summary>
        private void OnConfirmNo()
        {
            if (_confirmDeletePanel != null)
                _confirmDeletePanel.SetActive(false);
        }

        #endregion
        // -------------------------------------------------------------------------
        #region Navigation

        /// <summary>Navigates back to the Main Menu scene.</summary>
        private void OnBackBtn()
        {
            AdManager.Instance?.OnReturnToMenu();
            SceneLoader.Instance?.LoadScene(Constants.SCENE_MAIN_MENU);
        }

        #endregion
    }
}
