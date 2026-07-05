using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Extension
{
    /// <summary>
    /// p% 확률로 true 반환
    /// </summary>
    /// <param name="percent">0~100 사이 값</param>
    public static bool Check(float probability)
    {
        if (probability <= 0f) return false;
        if (probability >= 1f) return true;

        return Random.value < probability;
    }
}
