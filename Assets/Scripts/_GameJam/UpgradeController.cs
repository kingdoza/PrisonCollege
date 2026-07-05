using UnityEngine;
using UnityEngine.Events;

public class UpgradeController : MonoBehaviour
{
    public float interactDistance = 3f;        // 일정 거리
    public float requiredHoldTime = 1f;        // 3초
    public KeyCode interactKey = KeyCode.E;
    public LayerMask interactLayer;            // 상호작용 가능한 레이어

    public HUDController HUDcontroller; // UI 관

    public AudioSource[] holdSound; //상호작용동안 들리는 소리


    private float holdTimer = 0f;
    private bool isLookingAtInteractable = false;
    private IInteractable currentTarget;

    private bool isHoldingSoundPlaying = false;

    private int targetSpecific;

    void Update()
    {
        CheckLookTarget();
        HandleSound();

        if(targetSpecific >= 0 || targetSpecific <= 2)
        {
            HUDcontroller.UpdateINterctingTip(targetSpecific);
        }
        else            
        {
            HUDcontroller.UpdateINterctingTip(-1);
        }

        if (isLookingAtInteractable)
        {
            if (Input.GetKey(interactKey))
            {
                holdTimer += Time.deltaTime;

                HUDcontroller.UpdateInterctingUI(holdTimer/requiredHoldTime);

                if (holdTimer >= requiredHoldTime)
                {
                    currentTarget?.OnInteract();
                    holdTimer = 0f; // 초기화
                    StopHoldSound();
                }
            }
            else
            {
                holdTimer = 0f;
                HUDcontroller.UpdateInterctingUI(0f);
                StopHoldSound();
            }
        }
        else
        {
            holdTimer = 0f;
            HUDcontroller.UpdateInterctingUI(0f);
            StopHoldSound();
        }
    }

    void CheckLookTarget()
    {
        Ray ray = new Ray(Camera.main.transform.position, Camera.main.transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, interactDistance, interactLayer))
        {
            isLookingAtInteractable = true;
            currentTarget = hit.collider.GetComponent<IInteractable>();
            if (currentTarget != null)
                targetSpecific = currentTarget.GetSpecific();
        }
        else
        {
            isLookingAtInteractable = false;
            currentTarget = null;
            targetSpecific = -1;
        }
    }

    void HandleSound()
    {
        if (isLookingAtInteractable && Input.GetKey(interactKey))
        {
            PlayHoldSound();
        }
        else
        {
            StopHoldSound();
        }
    }

    void PlayHoldSound()
    {
        if (!isHoldingSoundPlaying && targetSpecific >= 0)
        {
            holdSound[targetSpecific].Play();
            isHoldingSoundPlaying = true;
        }
    }

    void StopHoldSound()
    {
        if (isHoldingSoundPlaying)
        {
            foreach(AudioSource x in holdSound)
            x.Stop();
            isHoldingSoundPlaying = false;
        }
    }
}

public interface IInteractable
{
    void OnInteract();
    int GetSpecific();
}