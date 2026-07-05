using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class FloodController : MonoBehaviour
{

    //파도 높이
    public Transform floodHeight;

    //파도 fade 시간
    public float floodFadeTime;

    //파도 지속 시간
    public float floodPersistentTime;
    
    //진행 변수
    private float progress;

    //물 소리  
    public AudioSource floodSound;

    //경보 소리
    public AudioSource fireAlarmSound;

    public GameObject[] alarmLights;


    public UnityEvent<bool> FullEvent = new UnityEvent<bool>();
    public bool isFull = false;


    //For Debugg
    // void Start(){ startFlood();   }



    // void Start()
    // {
    //     foreach (GameObject alarmLight in alarmLights)
    //     {
    //         alarmLight.SetActive(false);
    //     }
    // }



    public void startFlood()
    {
        StartCoroutine(FloodStartRoutine());
        floodSound.Play();
        fireAlarmSound.Play();
        foreach (GameObject alarmLight in alarmLights)
        {
            alarmLight.SetActive(true);
        }
    }


    IEnumerator FloodStartRoutine()
    {
        float timer = 0f;
        while (timer < floodFadeTime)
        {
            timer += Time.deltaTime;
            float t = timer / floodFadeTime;
            progress = Mathf.Lerp(-1f, 0.54f, t);   // 자연스러운 증가
            floodHeight.position = new Vector3(floodHeight.position.x,progress,floodHeight.position.z);
            yield return null;
        }
        FullEvent?.Invoke(true);
        isFull = true;
        yield return new WaitForSeconds(floodPersistentTime);
        isFull = false;
        FullEvent?.Invoke(false);
        
        floodSound.Stop();
        fireAlarmSound.Stop();
        foreach (GameObject alarmLight in alarmLights)
        {
            alarmLight.SetActive(false);
        }
        // 1 → 0 감소
        timer = 0f;
        while (timer < floodFadeTime)
        {
            timer += Time.deltaTime;
            float t = timer / floodFadeTime;
            progress = Mathf.Lerp(0.54f, -1f, t);   // 자연스러운 감소
            floodHeight.position = new Vector3(floodHeight.position.x,progress,floodHeight.position.z);
            yield return null;
        }

        // 마지막 값 정리
        progress = 0f;
    }
    

}
