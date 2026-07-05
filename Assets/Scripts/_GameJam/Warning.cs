using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Warning : MonoBehaviour
{
    [Header("Settings")]
    public float duration = 1.0f;     // 페이드 시간
    public float moveDown = 50f;       // 내려갈 거리 (픽셀)

    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;

    private float timer = 0f;
    private Vector2 startPos;
    private bool playing = false;
    public TextMeshProUGUI textBox;

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        rectTransform = GetComponent<RectTransform>();
        startPos = rectTransform.anchoredPosition;
    }

    void Update()
    {
        if (!playing) return;

        timer += Time.deltaTime;
        float t = Mathf.Clamp01(timer / duration);

        // 페이드 아웃
        canvasGroup.alpha = Mathf.Lerp(1f, 0f, t);

        // 아래로 이동
        rectTransform.anchoredPosition =
            startPos + Vector2.down * moveDown * t;

        if (t >= 1f)
        {
            playing = false;
            gameObject.SetActive(false); // 필요 없으면 제거 가능
        }
    }

    /// <summary>
    /// 효과 재생
    /// </summary>
    public void Play(string text)
    {
        textBox.text = text;
        timer = 0f;
        playing = true;

        canvasGroup.alpha = 1f;
        rectTransform.anchoredPosition = startPos;
        gameObject.SetActive(true);
    }
}
