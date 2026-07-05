using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SmokeSpot_d : Spot
{
    [Header("VFX Prefab")]
    [SerializeField] private ParticleSystem smokePrefab;

    [Header("Effect Settings")]
    [SerializeField] private Vector3 offset = Vector3.up * 1.8f;
    private ParticleSystem currentVFX;
    private bool vfxPlaying = false;
    public float arrivalTime = 0;

    public override void Update()
    {
        base.Update();
        if (isArrived)
        {
            arrivalTime += Time.deltaTime;
        }
        else
        {
            arrivalTime = 0;
        }

        if (student == null && vfxPlaying)
        {
            vfxPlaying=false;
            StopVFX();
        }

        if(!vfxPlaying&&arrivalTime>=5)
        {
            PlayVFX();
            vfxPlaying=true;
        }

        if (arrivalTime >= 8)
        {
            GameObject EnvCtrl = GameObject.FindWithTag("EnvCtrl");
            EnvCtrl.GetComponent<EnvironmentEventController>().startFlood();
            vfxPlaying=false;
            StopVFX();
        }
    }

    /// <summary>
    /// 프리팹에서 생성 후 재생
    /// </summary>
    private void PlayVFX()
    {
        if (smokePrefab == null) return;

        // 이미 생성되어 있으면 재생
        if (currentVFX != null)
        {
            currentVFX.Play();
            return;
        }

        // 프리팹 생성
        currentVFX = Instantiate(smokePrefab, transform.position + offset, Quaternion.identity);
        currentVFX.Play();
    }

    /// <summary>
    /// 재생 중지 + 초기화
    /// </summary>
    private void StopVFX()
    {
        if (currentVFX == null) return;

        currentVFX.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        Destroy(currentVFX.gameObject); // 완전히 제거
        currentVFX = null;
    }

    // public override void Update()
    // {
    //     if (isArrived)
    //     {
    //         if (!vfxPlaying)
    //         {
    //             PlayVFX();
    //             vfxPlaying = true;
    //         }
    //     }
    //     else
    //     {
    //         if (vfxPlaying)
    //         {
    //             ResetVFX();
    //             vfxPlaying = false;
    //         }
    //     }
    // }


    // private IEnumerator PlaySmoke(Vector3 position)
    // {
    //     smokePlaying = true;

    //     // Smoke 생성
    //     GameObject smoke = Instantiate(smokeObject, position + Vector3.up, Quaternion.identity);
    //     smoke.transform.localScale = startScale;

    //     Material mat = smoke.GetComponent<Renderer>().material;
    //     Color originalColor = mat.color;

    //     float timer = 0f;
    //     Vector3 startPos = smoke.transform.position;
    //     Vector3 endPos = startPos + Vector3.up * riseDistance;

    //     while (timer < duration)
    //     {
    //         float t = timer / duration;

    //         // 위치 이동
    //         smoke.transform.position = Vector3.Lerp(startPos, endPos, t);

    //         // 크기 증가
    //         smoke.transform.localScale = Vector3.Lerp(startScale, endScale, t);

    //         // 투명도 감소
    //         Color c = originalColor;
    //         c.a = Mathf.Lerp(1f, 0f, t);
    //         mat.color = c;

    //         timer += Time.deltaTime;
    //         yield return null;
    //     }

    //     Destroy(smoke);
    //     smokePlaying = false;
    // }



    public override string GetAnimName()
    {
        return "Smoke";
    }


    public override bool GetIsRootTrans()
    {
        return false;
    }


    public override bool GetIsEscape()
    {
        return false;
    }


    public override bool HasToTurn()
    {
        return hasTurn;
    }
}
