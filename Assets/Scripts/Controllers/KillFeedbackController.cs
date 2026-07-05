using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class KillFeedbackController : SceneSingleton<KillFeedbackController>
{
    [Header("Hit Stop Settings")]
    [Range(0f, 1f)] public float hitStopScale = 0.05f; // 얼마나 느려질지 (0에 가까울수록 멈춤)
    public float duration = 0.1f; // 유지 시간 (현실 시간 기준)

    [Header("Screen Tint Settings")]
    public Image overlayImage; // 화면 전체를 덮는 투명한 UI Image (빨간색 등)
    public Color killColor = new Color(1, 0, 0, 0.3f); // 죽였을 때 번쩍일 색상
    public SoundData killSD;

    public void PlayKillFeedback()
    {
        // 1. 기존에 돌던 연출이 있다면 초기화
        //StopAllCoroutines();
        DOTween.Kill("KillEffect");

        // 2. 슬로우 모션 (Hit Stop) 실행
        //StartCoroutine(HitStopRoutine());
        SoundUtils.PlayScene2DSFX(killSD, 0.8f);
        // 3. 화면 색상 연출 (GTA 스타일)
        if (overlayImage != null)
        {
            overlayImage.DOKill();
            overlayImage.color = killColor; // 즉시 색상 변경
            overlayImage.DOFade(0, 0.5f)    // 0.5초 동안 서서히 투명해짐
                        .SetUpdate(true)    // 중요: 타임스케일 영향을 받지 않도록 설정
                        .SetId("KillEffect");
        }
    }

    private IEnumerator HitStopRoutine()
    {
        float originalScale = 1f;
        Time.timeScale = hitStopScale;

        // 타임스케일이 줄어든 상태이므로 WaitForSeconds 대신 WaitForSecondsRealtime 사용
        yield return new WaitForSecondsRealtime(duration);

        // 서서히 원래 속도로 복구 (DOTween 사용)
        DOTween.To(() => Time.timeScale, x => Time.timeScale = x, originalScale, 0.2f)
               .SetUpdate(true); // 타임스케일 무시 설정
    }
}
