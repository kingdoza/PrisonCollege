using UnityEngine;

public class LightFlicker : MonoBehaviour
{
    public Light targetLight;         // 깜빡일 Light (Point Light)
    public float minIntensity = 0f;   // 최소 밝기
    public float maxIntensity = 1f;   // 최대 밝기
    public float fadeDuration = 0.5f; // 어두워지거나 밝아지는 데 걸리는 시간
    public float flickerInterval = 2f; // 깜빡이는 간격(초)

    private bool isBright = true;     // 현재 밝기 상태

    void Start()
    {
        if (targetLight == null)
            targetLight = GetComponent<Light>();

        StartCoroutine(FlickerRoutine());
    }

    private System.Collections.IEnumerator FlickerRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(flickerInterval);
            float startIntensity = targetLight.intensity;
            float endIntensity = isBright ? minIntensity : maxIntensity;

            float timer = 0f;

            while (timer < fadeDuration)
            {
                timer += Time.deltaTime;
                float t = timer / fadeDuration;
                targetLight.intensity = Mathf.Lerp(startIntensity, endIntensity, t);
                yield return null;
            }

            targetLight.intensity = endIntensity;
            isBright = !isBright; // 밝기 상태 반전
        }
    }
}
