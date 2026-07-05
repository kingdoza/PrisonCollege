using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.AI;

public static class Utils
{
    /// <summary>
    /// 대상 위치에서 가장 가까운 NavMesh 좌표를 반환합니다.
    /// </summary>
    /// <param name="targetPos">대상(타겟)의 현재 위치</param>
    /// <param name="range">검색할 반경 (보통 1.0f ~ 2.0f)</param>
    /// <returns>투사된 좌표 (찾지 못하면 원래 좌표 반환)</returns>
    public static Vector3 SampleNavMesh(Vector3 targetPos, float range)
    {
        NavMeshHit hit;

        // NavMesh.SamplePosition(검색 시작점, 결과 저장 변수, 검색 반경, 레이어 마스크)
        if (NavMesh.SamplePosition(targetPos, out hit, range, NavMesh.AllAreas))
        {
            return hit.position; // NavMesh 위로 투사된 좌표
        }

        return targetPos; // 근처에 NavMesh가 없으면 원래 위치 반환
    }


    /// <summary>
    /// SphereCast의 hit 정보를 분석하여 안전한 충돌 지점을 반환합니다.
    /// (시작점이 겹쳐 hit.point가 0일 경우 ClosestPoint로 보정)
    /// </summary>
    public static Vector3 GetContactPoint(this RaycastHit hit, Vector3 origin)
    {
        return hit.point == Vector3.zero ? hit.collider.ClosestPoint(origin) : hit.point;
    }

    /// <summary>
    /// SphereCast의 hit 정보를 분석하여 안전한 법선 벡터를 반환합니다.
    /// (법선이 0일 경우 쏜 방향의 반대 방향으로 보정)
    /// </summary>
    public static Vector3 GetNormal(this RaycastHit hit, Vector3 direction)
    {
        return hit.normal == Vector3.zero ? -direction : hit.normal;
    }

    /// <summary>
    /// 해당 게임 오브젝트가 특정 레이어 마스크에 포함되는지 확인합니다.
    /// </summary>
    public static bool IsInLayerMask(this GameObject obj, LayerMask mask)
    {
        return ((1 << obj.layer) & mask) != 0;
    }


    public static bool IsInLayerMask(this GameObject obj, string layerName)
    {
        int layerIndex = LayerMask.NameToLayer(layerName);

        // 2. 존재하지 않는 레이어 이름일 경우 처리 (-1 반환됨)
        if (layerIndex == -1)
        {
            Debug.LogWarning($"Layer '{layerName}' does not exist!");
            return false;
        }

        // 3. 비트 연산으로 오브젝트의 레이어와 비교
        // (1 << obj.layer)는 오브젝트의 레이어를 비트로 변환한 값
        // (1 << layerIndex)는 비교 대상 레이어를 비트로 변환한 값
        return (1 << obj.layer) == (1 << layerIndex);
    }



    // List<T>에 대한 확장 메서드 정의
    //public static T GetRandom<T>(this List<T> items) where T : IWeightedEntry
    //{
    //    // 기존 로직과 동일
    //    if (items == null || items.Count == 0) return default;

    //    float totalWeight = 0;
    //    foreach (var item in items)
    //    {
    //        totalWeight += item.Chance;
    //    }

    //    float pivot = Random.Range(0f, totalWeight);

    //    float cumulative = 0f;
    //    foreach (var item in items)
    //    {
    //        cumulative += item.Chance;
    //        if (pivot <= cumulative)
    //        {
    //            return item;
    //        }
    //    }

    //    return items[items.Count - 1];
    //}

    /// <summary>
    /// 배열에서 무작위 원소를 반환합니다.
    /// </summary>
    public static T GetRandom<T>(this T[] array)
    {
        if (array == null || array.Length == 0)
        {
            Debug.LogWarning("GetRandom: 배열이 비어있습니다.");
            return default;
        }

        return array[UnityEngine.Random.Range(0, array.Length)];
    }

    /// <summary>
    /// 리스트에서 무작위 원소를 반환합니다.
    /// </summary>
    public static T GetRandom<T>(this IList<T> list)
    {
        if (list == null || list.Count == 0)
        {
            Debug.LogWarning("GetRandom: 리스트가 비어있습니다.");
            return default;
        }

        return list[UnityEngine.Random.Range(0, list.Count)];
    }



    /// <summary>
    /// 두 Transform 사이의 직선 거리를 반환합니다. (3D)
    /// </summary>
    public static float DistanceTo(this Transform start, Transform target)
    {
        if (start == null || target == null) return 0f;
        return Vector3.Distance(start.position, target.position);
    }



    private static readonly Dictionary<BehaviorType, BehaviorSafety> _safetyCache = new();

    public static BehaviorSafety GetSafety(this BehaviorType type)
    {
        // 1. 캐시 확인
        if (_safetyCache.TryGetValue(type, out var cachedSafety))
            return cachedSafety;

        // 2. 리플렉션으로 Attribute 찾기
        var field = typeof(BehaviorType).GetField(type.ToString());
        if (field != null)
        {
            var attr = field.GetCustomAttribute<BehaviorInfoAttribute>();
            if (attr != null)
            {
                _safetyCache[type] = attr.Safety;
                return attr.Safety;
            }
        }

        // 3. 값이 없거나 복합 플래그인 경우 기본값 반환
        return BehaviorSafety.Safe;
    }

    // 도우미 메서드: 위험 행동인지 바로 확인
    public static bool IsHazard(this BehaviorType type) => GetSafety(type) == BehaviorSafety.Hazard;



    public static T DeepCopyByJson<T>(T source) where T : ScriptableObject
    {
        string json = JsonUtility.ToJson(source);
        T copy = ScriptableObject.CreateInstance<T>();
        JsonUtility.FromJsonOverwrite(json, copy);
        return copy;
    }



    public static void RemoveComponent<T>(this GameObject go, bool immediate = false) where T : Component
    {
        T component = go.GetComponent<T>();
        if (component != null)
        {
            if (immediate) UnityEngine.Object.DestroyImmediate(component);
            else UnityEngine.Object.Destroy(component);
        }
    }



    public static void RemoveComponents<T>(this GameObject go, bool immediate = false) where T : Component
    {
        // 1. 해당 오브젝트에서만 모든 T 컴포넌트를 가져옴 (자식은 포함 안 함)
        T[] components = go.GetComponents<T>();

        // 2. 역순으로 순회하며 삭제
        for (int i = components.Length - 1; i >= 0; i--)
        {
            if (immediate)
            {
                UnityEngine.Object.DestroyImmediate(components[i]);
            }
            else
            {
                UnityEngine.Object.Destroy(components[i]);
            }
        }
    }



    public static void RemoveComponentsInChildren<T>(this GameObject go, bool includeInactive = true, bool immediate = false) where T : Component
    {
        // 1. 모든 자식(본인 포함)에서 해당 컴포넌트들을 배열로 가져옴
        T[] components = go.GetComponentsInChildren<T>(includeInactive);

        // 2. 루프를 돌며 삭제
        for (int i = components.Length - 1; i >= 0; i--)
        {
            if (immediate)
            {
                UnityEngine.Object.DestroyImmediate(components[i]);
            }
            else
            {
                UnityEngine.Object.Destroy(components[i]);
            }
        }
    }



    public static void RemoveGameObjectsWithComponent<T>(this GameObject root, bool includeInactive = true, bool immediate = false) where T : Component
    {
        // 1. 해당 컴포넌트가 붙은 모든 객체를 찾음
        T[] targets = root.GetComponentsInChildren<T>(includeInactive);

        // 2. 루프를 돌며 오브젝트 자체를 삭제
        for (int i = targets.Length - 1; i >= 0; i--)
        {
            // 본인이 root라면 삭제하지 않도록 안전장치 (필요 시)
            if (targets[i].gameObject == root) continue;

            if (immediate)
            {
                UnityEngine.Object.DestroyImmediate(targets[i].gameObject);
            }
            else
            {
                UnityEngine.Object.Destroy(targets[i].gameObject);
            }
        }
    }



    private static System.Random _rng = new System.Random();
    public static T[] GetRandomElements<T>(this IEnumerable<T> source, int n)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));

        List<T> list = source.ToList();
        int count = list.Count;

        // 뽑으려는 개수가 전체보다 많으면 전체를 반환하거나 예외 처리
        n = Math.Min(n, count);

        // 피셔-예이츠 셔플 응용 (n번만 수행)
        for (int i = 0; i < n; i++)
        {
            int randomIndex = _rng.Next(i, count);
            T temp = list[i];
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }

        // 섞인 앞부분 n개만 잘라서 반환
        return list.GetRange(0, n).ToArray();
    }



    public static T CopyComponentTo<T>(this T original, GameObject destination) where T : Component
    {
        // 대상 오브젝트에 동일한 타입의 컴포넌트 생성
        Type type = original.GetType();
        Component copy = destination.AddComponent(type);

        // 리플렉션을 이용해 모든 필드(변수) 값 복사
        FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Default);
        foreach (FieldInfo field in fields)
        {
            field.SetValue(copy, field.GetValue(original));
        }

        return copy as T;
    }
}
