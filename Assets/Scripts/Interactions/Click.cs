using UnityEngine;
using UnityEngine.Events;

public class Click : MonoBehaviour, IPlayerInteractable
{
    public bool InteractState { get; set; } = true;
    public string ActionName { get; set; } = "상호작용";
    public float FillAmount { get; set; } = 1;
    public string InteractionPrompt => $"[F] {ActionName}";
    public bool CanInteract => InteractState;
    public float UIFillRatio => FillAmount;

    public UnityEvent ClickEvent = new();

    public void OnInteractCancel()
    {
        
    }

    public void OnInteractStart()
    {
        ClickEvent?.Invoke();
    }
}
