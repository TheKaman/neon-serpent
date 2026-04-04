using UnityEngine;
using NeonSerpent.Utilities;

namespace NeonSerpent.Core
{
    /// <summary>
    /// Entry point for the Bootstrap scene.
    /// Ensures all DontDestroyOnLoad singletons are initialized in the correct order,
    /// then loads the MainMenu scene.
    /// </summary>
    public class ApplicationController : MonoBehaviour
    {
        private void Awake()
        {
            // Lock frame rate on device. 60fps on mid/high-end; vsync off so the lock
            // takes effect. VSync is disabled because enabling it on Android can cause
            // the frame rate to be tied to the display refresh rate rather than our cap.
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount  = 0;
        }

        private void Start()
        {
            // Bootstrap initializes all singletons via their own Awake().
            // After they're ready, transition to the main menu.
            SceneLoader.Instance.LoadScene(Constants.SCENE_MAIN_MENU);
        }
    }
}
