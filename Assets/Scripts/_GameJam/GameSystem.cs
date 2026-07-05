using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class GameSystem : MonoBehaviour
{
    [SerializeField] private GameObject gameOverText;
    [SerializeField] private GameObject gameWinText;

    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private TextMeshProUGUI escapedGraduateStudentCounterText;

    public float startTime;
    private float currentTime;
    private bool isRunning = true;

    private int escapedGraduateStudentCounter;

    void Start()
    {
        gameOverText.SetActive(false);
        gameWinText.SetActive(false);
        currentTime = startTime;
        escapedGraduateStudentCounter = 0;
        escapedGraduateStudentCounterText.text = "대학원생 수 : " + (5 - escapedGraduateStudentCounter) + "/5";
        //escapedGraduateStudentCounterText.text = "탈출한 대학원생 : " + escapedGraduateStudentCounter + "/3";
    }

    void Update()
    {
        if (isRunning)
        {
            currentTime -= Time.deltaTime;

            if (currentTime <= 0)
            {
                currentTime = 0;
                isRunning = false;
                GameWin();
            }

            UpdateTimerUI();
        }
    }

    void UpdateTimerUI()
    {
        int minutes = Mathf.FloorToInt(currentTime / 60);
        int seconds = Mathf.FloorToInt(currentTime % 60);

        timerText.text = "남은 시간 - " + string.Format("{0:0}:{1:00}", minutes, seconds);
    }

    public void UpdateEscapeCounter()
    {
        escapedGraduateStudentCounter++;
        // escapedGraduateStudentCounterText.text = string.Format("탈출한 대학원생 수 : {0}/3", escapedGraduateStudentCounter);
        //escapedGraduateStudentCounterText.text = "탈출한 대학원생 : " + escapedGraduateStudentCounter + "/3";
        escapedGraduateStudentCounterText.text = "대학원생 수 : " + (5 - escapedGraduateStudentCounter) + "/5";
        if(escapedGraduateStudentCounter>=3) GameOver(); 
    }


    public void GameOver()
    {
        gameOverText.SetActive(true);
        goBackToTitle();
    }

    public void GameWin()
    {
        gameWinText.SetActive(true);
        goBackToTitle();
    }

    void goBackToTitle()
    {
        IEnumerator delayRoutine()
        {
            yield return new WaitForSeconds(3F);
            gameObject.GetComponent<SceneLoad>().LoadScene("StartScene");
        }
        StartCoroutine(delayRoutine());
    }
}
