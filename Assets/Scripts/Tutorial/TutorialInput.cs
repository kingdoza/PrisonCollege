using System;
using UnityEngine;

public class TutorialInput : MonoBehaviour
{
    public event Action AdvancePressed;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
            AdvancePressed?.Invoke();
    }
}
