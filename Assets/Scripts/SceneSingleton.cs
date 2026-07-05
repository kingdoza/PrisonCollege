using UnityEngine;

public class SceneSingleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T _instance;
    private static readonly object _lock = new object();

    public static T Instance
    {
        get
        {
            lock (_lock)
            {
                if (_instance == null)
                {
                    // 씬 내에 이미 배치된 인스턴스가 있는지 확인
                    _instance = (T)Object.FindAnyObjectByType(typeof(T));

                    if (_instance == null)
                    {
                        // 없다면 새로 생성 (필요한 경우에만)
                        GameObject singletonObject = new GameObject(typeof(T).Name);
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
        }
        else if (_instance != this)
        {
            // 씬에 이미 다른 인스턴스가 있다면 중복 방지를 위해 파괴
            Debug.LogWarning($"[SceneSingleton] {typeof(T).Name}의 중복 인스턴스가 감지되어 파괴되었습니다.");
            Destroy(gameObject);
        }

        // DontDestroyOnLoad를 하지 않으므로 씬 전환 시 자동으로 파괴됨
    }

    protected virtual void OnDestroy()
    {
        // 인스턴스가 파괴될 때 참조를 비워줌 (메모리 누수 방지)
        if (_instance == this)
        {
            _instance = null;
        }
    }
}