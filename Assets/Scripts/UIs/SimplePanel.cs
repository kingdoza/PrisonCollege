using NUnit.Framework.Constraints;
using UnityEngine;
using UnityEngine.Events;

public class SimplePanel : MonoBehaviour, IEscapeControllable
{
    private CanvasGroup _canvasGroup;
    public UnityEvent DeactivateEvent = new();



    private void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
    }



    private void Start()
    {
        //Hide(); 
    }



    public void Show()
    {
        _canvasGroup.alpha = 1;
        _canvasGroup.interactable = true;
        _canvasGroup.blocksRaycasts = true;
    }



    public void Hide()
    {
        _canvasGroup.alpha = 0;
        _canvasGroup.interactable = false;
        _canvasGroup.blocksRaycasts = false;
    }



    public void Back_Btn()
    {
        Hide();
        EscapeInputSystem.Instance.DisablePanel(this);
    }

    public void Exit_Btn()
    {
        GameManager.Instance.ExitGame();
    }

    public void Activate()
    {
        Show();
    }

    public void Deactivate()
    {
        Hide();
        DeactivateEvent?.Invoke();
    }
}
