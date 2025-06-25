using UnityEngine;

public class GenericSingletonClass<T> : MonoBehaviour where T : Component
{
    [SerializeField] private static T instance = null;
    public static T Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindAnyObjectByType<T>();
                if (!instance)
                {
                    GameObject obj = new GameObject();
                    obj.name = typeof(T).Name;
                    instance = obj.AddComponent<T>();
                    DontDestroyOnLoad(obj);
                }
            }

            return instance;
        }
    }
}
