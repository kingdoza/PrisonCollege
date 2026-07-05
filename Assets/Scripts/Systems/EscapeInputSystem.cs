using System.Collections.Generic;
using UnityEngine;

public class EscapeInputSystem : SceneSingleton<EscapeInputSystem>
{
    [SerializeField] private GameObject _defaultEscapePanelObj;
    private Stack<IEscapeControllable> _panelStack = new();
    private IEscapeControllable _defaultEscapeControllerable;



    protected override void Awake()
    {
        base.Awake();
        if (_defaultEscapePanelObj != null)
        {
            _defaultEscapePanelObj.SetActive(true);
            _defaultEscapeControllerable = _defaultEscapePanelObj.GetComponent<IEscapeControllable>();
        }
    }



    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (_panelStack.Count <= 0 && _defaultEscapeControllerable != null)
            {
                EnablePanel(_defaultEscapeControllerable);
            }
            else if (_panelStack.Count > 0)
            {
                DisableTopPanel();
            }
        }
    }



    public void EnablePanel(IEscapeControllable newPanel)
    {
        _panelStack.Push(newPanel);
        newPanel.Activate();
    }



    public void DisablePanel(IEscapeControllable targetPanel)
    {
        if (_panelStack.Count <= 0) return;
        if (_panelStack.Peek() != targetPanel) return;
        IEscapeControllable top =_panelStack.Pop();
        top.Deactivate();
    }



    private void DisableTopPanel()
    {
        if (_panelStack.Count <= 0) return;
        DisablePanel(_panelStack.Peek());
    }








}
