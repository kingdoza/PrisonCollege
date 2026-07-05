using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class SpotGroup : MonoBehaviour
{
    public List<SpotEntry> Spots;

    /// <summary>
    /// 가중치 확률에 따라 랜덤하게 BehaveSpot을 반환합니다.
    /// </summary>
    public BehaveSpot GetRandomSpotByWeight()
    {
        if (Spots == null || Spots.Count == 0) return null;

        // 1. 모든 가중치의 총합을 구함
        float totalWeight = 0;
        foreach (var entry in Spots)
        {
            totalWeight += entry.weight;
        }

        // 2. 0부터 총합 사이의 랜덤 값 생성
        float pivot = Random.Range(0, totalWeight);
        float currentWeight = 0;

        // 3. 어떤 구간에 랜덤 값이 속하는지 확인
        foreach (var entry in Spots)
        {
            currentWeight += entry.weight;
            if (pivot <= currentWeight)
            {
                return entry.behaveSpot;
            }
        }

        // 만약 소수점 계산 오차 등으로 못 찾으면 마지막 항목 반환
        return Spots[Spots.Count - 1].behaveSpot;
    }
}