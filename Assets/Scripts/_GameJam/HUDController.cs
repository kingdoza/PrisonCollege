using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
// using System.Numerics;

public class HUDController : MonoBehaviour
{
    
    public Image img;

    public TextMeshProUGUI complexityScore;
    public TextMeshProUGUI interactingTip;
    private Color fillColor;

    bool canErase;

    void Start()
    {
        fillColor = img.color;
        img.color = fillColor;
        interactingTip.text = "";
    }

    
    public void UpdateInterctingUI(float t)
    {
        img.color = fillColor;
        img.fillAmount = t;
        
    }

    public void UpdateINterctingTip(int specific)
    {
        switch(specific)
        {
            case 0:
            interactingTip.text = "E키를 눌러 문 강화하기";
            break;
            
            case 1:
            interactingTip.text = "E키를 눌러 창문 강화하기";
            break;

            case 2:
            interactingTip.text = "E키를 눌러 고치기";
            break;

            default:
            interactingTip.text = " ";
            break;
        }
    }

    void Update()
    {
        complexityScore.text = "혼란 : " + ChaosSystem.chaos;
    }

}
