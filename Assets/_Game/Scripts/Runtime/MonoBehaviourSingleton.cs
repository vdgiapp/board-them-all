using UnityEngine;

namespace Game
{
    public abstract class MonoBehaviourSingleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        public static T Instance { get; protected set; }
        
        protected virtual void Awake()
        {
            if (!Instance)
            {
                Instance = this as T;
                return;
            }
            Debug.Log($"Instance: {typeof(T).Name} already exists, destroying duplicate: {gameObject.name}");
            Destroy(gameObject);
        }
        
        protected virtual void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}