using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// InputSystem 사용할거
/// </summary>
public class InputSystemHandler : MonoBehaviour
{
    public event Action<Vector2> OnMoveInput;
    public event Action<Vector2> OnCameraRotInput;
    public event Action<Vector2> OnMouseScrollInput;
    public event Action<bool> OnAttackInput;
    public event Action OnSniperInput;
    public event Action OnToggleRightUIInput;
    public event Action OnMapInput;

    bool _onAttack = false;     // 좌클릭 상태 토글
    Vector2 _moveInput;         // 이동 입력
    Vector2 _cameraRotInput;    // 카메라 회전 입력
    Vector2 _cameraZoomInput;   // 카메라 줌

    private void Update()
    {
        OnMoveInput?.Invoke(_moveInput);
        OnCameraRotInput?.Invoke(_cameraRotInput);
        OnMouseScrollInput?.Invoke(_cameraZoomInput);
    }

    public void HandleMoveInput(InputAction.CallbackContext context)
    {
        _moveInput = context.ReadValue<Vector2>();
    }

    public void HandleCameraRotInput(InputAction.CallbackContext context)
    {
        _cameraRotInput = context.ReadValue<Vector2>();
    }

    // 마우스 휠
    public void HandlePlayerScrollWhellInput(InputAction.CallbackContext context)
    {
        _cameraZoomInput = context.ReadValue<Vector2>();
    }

    // 좌클릭 입력 토글
    public void HandleAttackInput(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            _onAttack = true;
            OnAttackInput?.Invoke(_onAttack);
        }
        else if (context.canceled)
        {
            _onAttack = false;
            OnAttackInput?.Invoke(_onAttack);
        }
    }

    // Shift키(저격 모드)
    public void HandleSniperInput(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            OnSniperInput?.Invoke();
        }
    }

    // Tab키(오른쪽 UI)
    public void HandleToggleRightUIInput(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            OnToggleRightUIInput?.Invoke();
        }
    }

    // M키(미니맵)
    public void HandleMapInput(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            OnMapInput?.Invoke();
        }
    }
}
