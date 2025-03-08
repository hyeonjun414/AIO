using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.Framework.UI
{
    public abstract class Singleton<T> : SerializedMonoBehaviour where T : MonoBehaviour
    {
        [SerializeField] private bool isDontDestroyOnLoad = false;
        public static T Instance;
        protected virtual void Awake()
        {
            if (isDontDestroyOnLoad == false)
            {
                Instance = this as T;
                OnCreated();
            }
            else
            {
                if (Instance == null)
                {
                    Instance = this as T;
                    OnCreated();
                    DontDestroyOnLoad(Instance);
                }
                else
                {
                    Destroy(gameObject);
                }
            }
        }

        public static bool IsValid()
        {
            return Instance != null && Instance.gameObject != null;
        }
        
        protected virtual void OnCreated() { }

        protected virtual void OnDestroy()
        {
            if(Instance == this)
            {
                Destroy();
                Instance = null;
            }
        }

        protected virtual void Destroy() { }
    }
}
