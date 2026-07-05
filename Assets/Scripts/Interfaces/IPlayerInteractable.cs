using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IPlayerInteractable
{
    string InteractionPrompt { get; } // 화면에 띄울 메시지 (예: "F키를 눌러 설치")
    bool CanInteract { get; }         // 지금 상호작용 가능한 상태인가?
    float UIFillRatio {  get; }

    void OnInteractStart();               // 즉시 실행 (클릭)
    void OnInteractCancel();            // 상호작용 종료/취소 시
}
