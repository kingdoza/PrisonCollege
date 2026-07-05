using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    [SerializeField] private float _interactRange = 3f;
    [SerializeField] private float _interactRadius = 0.3f;
    [SerializeField] private LayerMask _interactableLayer;
    [SerializeField] private InteractionUI _ui;
    private IPlayerInteractable _currentInteractable;
    private IPlayerInteractable _activeInteractable;
    public IPlayerInteractable CurrentInteractable => _currentInteractable;



    private void Update()
    {
        CheckForInteractable();
        HandleInput();
        CheckFocusLost();

        UpdateInteractionUI();
    }



    private void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.F))
        {
            StartInteraction();
        }
    }


    private void UpdateInteractionUI()
    {
        // 1. 현재 실제로 진행 중인 상호작용이 있다면 그 수치를 우선 표시
        if (_activeInteractable != null)
        {
            _ui.Fill(_activeInteractable.UIFillRatio);
        }
        // 2. 진행 중인 건 없지만, 바라보는 대상이 있다면 (보통 0%이거나 대기 상태)
        else if (_currentInteractable != null)
        {
            _ui.Fill(_currentInteractable.UIFillRatio);
        }
    }



    public void CancelActiveInteraction()
    {
        if (_activeInteractable != null)
        {
            _activeInteractable.OnInteractCancel(); // 인터페이스에 정의된 취소 로직 실행
            _activeInteractable = null;
        }
    }

    // 상호작용 시작 시 호출되는 함수 (예시)
    private void StartInteraction()
    {
        if (_currentInteractable != null && _currentInteractable.CanInteract)
        {
            _activeInteractable = _currentInteractable;
            _activeInteractable.OnInteractStart();
        }
    }



    private void CheckFocusLost()
    {
        // 상호작용 중인데, 바라보는 대상(_currentInteractable)이 바뀌었거나 null이 된 경우
        if (_activeInteractable != null && _activeInteractable != _currentInteractable)
        {
            CancelActiveInteraction();
        }
    }



    private void CheckForInteractable()
    {
        // 카메라 위치와 방향 설정
        Vector3 origin = Camera.main.transform.position;
        Vector3 direction = Camera.main.transform.forward;

        // SphereCast: 점이 아닌 구체 형태로 레이를 쏨
        // 인자: 시작점, 반지름, 방향, 결과, 거리, 레이어마스크
        //if (Physics.SphereCast(origin, _interactRadius, direction, out RaycastHit hit, _interactRange, _interactableLayer))
        if (Physics.Raycast(origin, direction, out RaycastHit hit, _interactRange, _interactableLayer))
        {
            IPlayerInteractable interactable = hit.collider.GetComponentInParent<IPlayerInteractable>();

            if (interactable != null && interactable.CanInteract)
            {
                if (_currentInteractable != interactable)
                {
                    _currentInteractable = interactable;
                    // 문자열 보간($)을 사용하면 더 깔끔합니다.
                    _ui.Show(_currentInteractable.InteractionPrompt);
                }
                return;
            }
        }

        if (_currentInteractable != null)
        {
            _currentInteractable = null;
            _ui.Hide();
        }
    }
}
