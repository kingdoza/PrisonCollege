using UnityEngine;

public class SettingPanel : MonoBehaviour, IEscapeControllable
{
    private CanvasGroup _canvasGroup;



    private void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
    }



    private void Start()
    {
        //gameObject.SetActive(true);
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

    public void Activate()
    {
        Show();
    }

    public void Deactivate()
    {
        Hide();
    }
}
