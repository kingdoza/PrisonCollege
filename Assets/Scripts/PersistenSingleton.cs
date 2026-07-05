using UnityEngine;

public class PersistentSingleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T _instance;
    private static readonly object _lock = new object();
    private static bool _quitting = false;

    public static T Instance
    {
        get
        {
            // 앱 종료 시 싱글톤 호출 방지 (Ghost 인스턴스 생성 막기)
            if (_quitting) return null;

            lock (_lock)
            {
                if (_instance == null)
                {
                    _instance = (T)Object.FindAnyObjectByType(typeof(T));

                    if (_instance == null)
                    {
                        GameObject singletonObject = new GameObject($"[Singleton] {typeof(T).Name}");
                        _instance = singletonObject.AddComponent<T>();
                    }
                }
                return _instance;
            }
        }
    }

    protected virtual void Awake()
    {
        if (_instance == null)
        {
            _instance = this as T;

            // [핵심] 부모가 있다면 부모까지 최상위로 올려서 파괴 방지
            if (transform.parent != null)
            {
                transform.SetParent(null);
            }

            // [핵심] 씬이 바뀌어도 파괴되지 않게 설정
            DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            Debug.LogWarning($"[PersistentSingleton] {typeof(T).Name} 중복 인스턴스 파괴.");
            Destroy(gameObject);
        }
    }

    protected virtual void OnApplicationQuit()
    {
        _quitting = true;
    }

    protected virtual void OnDestroy()
    {
        if (_instance == this)
        {
            // _quitting 상태가 아닐 때만 null 체크 (필요 시)
            _instance = null;
        }
    }
}