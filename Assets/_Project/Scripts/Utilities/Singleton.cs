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
            DontDestroyOnLoad(gameObject);
        }
    }
}
