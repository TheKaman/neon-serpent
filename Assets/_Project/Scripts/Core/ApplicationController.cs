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
        private void Start()
        {
            // Bootstrap initializes all singletons via their own Awake().
            // After they're ready, transition to the main menu.
            SceneLoader.Instance.LoadScene(Constants.SCENE_MAIN_MENU);
        }
    }
}
