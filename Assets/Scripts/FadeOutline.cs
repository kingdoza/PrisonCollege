using UnityEngine;
using System.Collections;

public class FadeOutline : Outline
{
    private Coroutine currentCoroutine;

    [Header("Settings")]
    public float maxOutlineWidth = 2.0f; // 목표 두께
    public float fadeDuration = 0.3f;    // 페이드 속도
    public float blinkSpeed = 2.0f;      // 깜빡임 속도

    // 1. [즉시 변경] 그냥 껐다 켰다 하기 (가장 확실함)
    public void ToggleOutline(bool isOn)
    {
        StopAction();
        this.OutlineWidth = maxOutlineWidth; // 두께 고정
        this.enabled = isOn;
    }

    // 2. [두께 페이드] 알파 대신 두께를 0~Max로 조절해서 페이드 효과내기
    public void FadeIn()
    {
        StopAction();
        this.enabled = true;
        currentCoroutine = StartCoroutine(Co_LerpWidth(this.OutlineWidth, maxOutlineWidth));
    }

    public void FadeOut()
    {
        StopAction();
        currentCoroutine = StartCoroutine(Co_LerpWidth(this.OutlineWidth, 0f, true));
    }

    // 3. [깜빡임] 두께가 0~Max를 반복
    public void StartBlink()
    {
        StopAction();
        this.enabled = true;
        currentCoroutine = StartCoroutine(Co_BlinkWidth());
    }

    public void StopAction()
    {
        if (currentCoroutine != null) StopCoroutine(currentCoroutine);
        currentCoroutine = null;
    }

    private IEnumerator Co_LerpWidth(float start, float end, bool disableAtEnd = false)
    {
        float elapsed = 0;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            this.OutlineWidth = Mathf.Lerp(start, end, elapsed / fadeDuration);
            yield return null;
        }
        this.OutlineWidth = end;
        if (disableAtEnd && end <= 0) this.enabled = false;
    }

    private IEnumerator Co_BlinkWidth()
    {
        while (true)
        {
            // 두께를 왕복시킴 (알파가 안 먹히니 두께로 깜빡임 구현)
            this.OutlineWidth = Mathf.PingPong(Time.time * blinkSpeed * maxOutlineWidth, maxOutlineWidth);
            yield return null;
        }
    }
}