using UnityEngine;

public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T m_ins;
    private static bool m_isQuitting = false;

    protected virtual void Awake()
    {
        MakeSingleton(true);
    }

    protected virtual void OnApplicationQuit()
    {
        m_isQuitting = true;
    }

    protected virtual void OnDestroy()
    {
        if (m_ins == this)
        {
            m_ins = null;
        }
    }

    public static bool HasInstance => m_ins != null && !m_isQuitting;

    public static T Ins
    {
        get
        {
            if (m_isQuitting)
            {
                return null;
            }

#if UNITY_EDITOR
            if (!UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode || UnityEditor.EditorApplication.isCompiling)
            {
                return null;
            }
#endif

            if (m_ins == null)
            {
                m_ins = Object.FindFirstObjectByType<T>();

                if (m_ins == null && !m_isQuitting && Application.isPlaying)
                {
                    GameObject singleton = new GameObject(typeof(T).Name);
                    m_ins = singleton.AddComponent<T>();
                }
            }

            return m_ins;
        }
    }

    public void MakeSingleton(bool destroyOnload)
    {
        if (m_ins == null)
        {
            m_ins = this as T;

            if (destroyOnload) return;

            var root = transform.root;

            if (root != transform)
            {
                DontDestroyOnLoad(root);
            }
            else
            {
                DontDestroyOnLoad(gameObject);
            }
        }
        else if (m_ins != this)
        {
            Destroy(gameObject);
        }
    }
}
