using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoad : MonoBehaviour
{
    float time = 0;
    public void LoadScene(string name){
        ChaosSystem.chaos = 0;
        StartCoroutine(LoadingAsync(name));
    }

    IEnumerator LoadingAsync(string name){
        AsyncOperation asyncOperation = SceneManager.LoadSceneAsync(name);
        asyncOperation.allowSceneActivation = true; //로딩이 완료되는대로 씬을 활성화할것인지
        
        while(!asyncOperation.isDone){ //isDone는 로딩이 완료되었는지 확인하는 변수
            time += Time.deltaTime; //시간을 더해줌
            print(asyncOperation.progress); //로딩이 얼마나 완료되었는지 0~1의 값으로 보여줌
            
            yield return null;
        }
    }
}
