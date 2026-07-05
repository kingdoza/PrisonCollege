using System.Collections;
using System.Collections.Generic;
using UnityEngine;
// using UnityEngine.Random;

public class DecidingBehaviour : MonoBehaviour
{
    //루틴 간의 기다리는 시간
    public float waitingTime;

    //for debugg
    // void Start(){ startBehaviourRoutine();}

    public void startBehaviourRoutine()
    {
        //waitingTime 만큼 기다렸다가 행동 시작
        StartCoroutine(RoutineDelay());
    }

    IEnumerator RoutineDelay()
    {
        yield return new WaitForSeconds(waitingTime);
        decideBehaviour();
    }


    private void decideBehaviour()
    {
        int randValue = Random.Range(1,100 + 1);

        if(randValue <= 20)
        {
            //기본 상태 함수 호출
            Debug.Log("기본");
        }
        else if(randValue <= 45)
        {
            //정문 탈출 함수 호출
            Debug.Log("1");
        }
        else if(randValue <= 65)
        {
            //창문 탈출 함수 호출
            Debug.Log("2");
        }
        else if(randValue <= 80)
        {
            //춤추는 대학원생 호출
            Debug.Log("3");
        }
        else if(randValue <= 90)
        {
            //맞짱 대학원생 호출
            Debug.Log("4");
        }
        else if(randValue <= 95)
        {
            //불꺼지는 기믹 호출
            Debug.Log("5");
        }
        else
        {
            //홍수나는 기믹 호출
            Debug.Log("6");
        }

        startBehaviourRoutine();
    }
}
