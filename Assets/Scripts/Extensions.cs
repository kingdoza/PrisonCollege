using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public static class Extensions
{
    /// <summary>
    /// 리스트에서 무작위 원소 하나를 반환합니다.
    /// </summary>
    public static T GetRandomElem<T>(this List<T> list)
    {
        if (list == null || list.Count == 0)
        {
            Debug.LogWarning("리스트가 비어있어 무작위 원소를 뽑을 수 없습니다.");
            return default;
        }

        int randomIndex = Random.Range(0, list.Count);
        return list[randomIndex];
    }

    /// <summary>
    /// 리스트를 무작위로 섞습니다 (Fisher-Yates Shuffle).
    /// </summary>
    public static void Shuffle<T>(this List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            T temp = list[i];
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }


    public static void SetSampleDestination(this NavMeshAgent agent, Vector3 targetPos, float range)
    {
        Vector3 samplePos = Utils.SampleNavMesh(targetPos, range);
        agent.SetDestination(samplePos);
    }
}
