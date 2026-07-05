using System.Collections;
using System.Collections.Generic;
// using System.Diagnostics;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
public class UpgradeDoors : MonoBehaviour, IInteractable
{
    public enum specifics{doors, window, electricShield};
    public specifics Specifics;
    public GameObject doorObject;
    public UnityEvent LightOffEvent = new UnityEvent();

    private int health = 100;
    
    public bool isUpgraded = true;

    public string AnimName { get {
            if (Specifics == specifics.doors)
            {
                return isUpgraded ? "Punch" : "OpenDoor";
            }
            else if (Specifics == specifics.window)
            {
                return isUpgraded ? "Punch" : "JumpWindow"; 
            }
            return "Idle";
        }
        set => throw new System.NotImplementedException(); 
    }

    public bool isLight = true;

    UnityEvent UseChangeEvent { get; set; } = new UnityEvent();

    void Start()
    {
        if(Specifics == specifics.doors || Specifics == specifics.window)
        {
            if (Extension.Check(0.5f))
            {
                gameObject.GetComponent<MeshRenderer>().enabled=false; 
                isUpgraded = false;
                gameObject.layer = 6;
                UseChangeEvent?.Invoke();
            }
            else
            {
                DoUpgrade();
            }
            //DoCrash();
        // gameObject.GetComponent<MeshRenderer>().enabled=false;       
        // isUpgraded = false;
        }
        else if(Specifics == specifics.electricShield)
        {
            hackPopup.SetActive(false);
            // WarnSystemHack = StartCoroutine(SystemHackWarning());
            // StopCoroutine(WarnSystemHack);
            // DoFixLight();
        }
    }

    public int GetSpecific()
    {
        return (int)Specifics;
    }
    public void OnInteract()
    {
        Debug.Log("AAA");
        if(Specifics == specifics.doors || Specifics == specifics.window) DoUpgrade();
        else if(Specifics == specifics.electricShield) DoFixLight();
    }

    public void DoUpgrade()
    {
        gameObject.GetComponent<MeshRenderer>().enabled=true;
        isUpgraded = true;  
        gameObject.layer = 0;
        UseChangeEvent?.Invoke();
    }

    public void DoCrash()
    {
        if (isUpgraded == false)
            return;
        gameObject.GetComponent<MeshRenderer>().enabled=false; 
        isUpgraded = false;
        gameObject.layer = 6;
        UseChangeEvent?.Invoke();
        AudioSource.PlayClipAtPoint(Player.Instance.audioPlayer.woodBreakSound, transform.position, 0.6f);
    }

    void DoFixLight()
    {
        isLight = true;
        GameObject EnvCtrl = GameObject.FindWithTag("EnvCtrl");
        EnvCtrl.GetComponent<EnvironmentEventController>().EndRedLight();
        gameObject.layer = 0;

        hackPopup.SetActive(false);
        if (WarnSystemHack != null)
            StopCoroutine(WarnSystemHack);
    }


    public void OffLight()
    {
        if (isLight == false)
            return;
        isLight = false;
        LightOffEvent?.Invoke();
        GameObject EnvCtrl = GameObject.FindWithTag("EnvCtrl");
        EnvCtrl.GetComponent<EnvironmentEventController>().startRedLight();

        hackPopup.SetActive(true);
        WarnSystemHack = StartCoroutine(SystemHackWarning());
    }

    public void Use()
    {
        health -= 20;
        if (health <= 0)
        {
            DoCrash();
        }
    }


    IEnumerator DelayClose(float delay)
    {
        yield return new WaitForSeconds(delay);
        CloseDoor();
    }


    bool isDoorOpen = false;


    public void OpenDoor()
    {
        if (isDoorOpen)
            return;
        isDoorOpen = true;
        CancelInvoke();
        doorObject.transform.rotation = Quaternion.Euler(0, -90f, 0);
        Invoke("CloseDoor", 2);
        Debug.Log("OpenDoor");
        AudioSource.PlayClipAtPoint(Player.Instance.audioPlayer.doorOpenSound, transform.position, 0.6f);
        //DelayClose(5f);
    }


    public void CloseDoor()
    {
        isDoorOpen = false;
        doorObject.transform.rotation = Quaternion.Euler(0, 0f, 0);
    }

    public Coroutine WarnSystemHack;
    public GameObject hackPopup;


    IEnumerator SystemHackWarning()
    {
        hackPopup.SetActive(true);
        Image popupImg = hackPopup.GetComponent<Image>();
        Color color = popupImg.color;

        while(!isLight) // 전원이 켜져 있는 동안 무한 반복
        {
            // 시간(Time.time)에 따라 0.5 ~ 1.0 사이를 부드럽게 왔다갔다 함
            // 0.75f는 중간값, 0.25f는 진폭, 5f는 속도입니다.
            float alpha = 0.6f + Mathf.Sin(Time.time * 5f) * 0.4f;
            
            color.a = alpha;
            popupImg.color = color;

            yield return null; // 매 프레임마다 갱신 (부드러움의 핵심)
        }

        hackPopup.SetActive(false); // 루프가 끝나면 팝업 끄기
    }


    void Update()
    {
        // if (Specifics == specifics.electricShield && is)
        // {
        //     //OffLight();
        // }
    }
}
