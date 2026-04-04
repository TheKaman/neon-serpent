using UnityEngine;

namespace NeonSerpent.Utilities
{
    /// <summary>
    /// Generic singleton base class for MonoBehaviours.
    /// Attach to a GameObject in the Bootstrap scene and mark DontDestroyOnLoad.
    /// </summary>
    public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T _instance;

        public static T Instance
        {
            get
            {
                if (_instance == null)
                    Debug.LogError($"[Singleton] No instance of {typeof(T)} found. Is it in the Bootstrap scene?");
                return _instance;
            }
        }

        protected virtual void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this as T;

            // DontDestroyOnLoad only works on root GameObjects.
            // If this singleton lives under a parent (e.g. [Managers]), we must mark
            // the root so the entire hierarchy survives the scene transition.
            // Calling this multiple times on the same root is harmless.
            DontDestroyOnLoad(transform.root.gameObject);
        }

        protected virtual void OnDestroy()
        {
            // Clear the static reference so a Bootstrap reload does not return
            // a destroyed instance. Only clear if this is the current instance —
            // a duplicate that lost the race and was immediately destroyed should
            // not null out the winner's reference.
            if (_instance == this)
                _instance = null;
        }
    }
}
