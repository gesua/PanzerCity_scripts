using System;
using Unity.VisualScripting;
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
    public event Action<bool> OnFreeLookInput;
    public event Action OnPauseInput;

    bool _onAttack; // 좌클릭 상태 토글
    bool _isPaused;  // 일시정지 시 키입력 막음
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
        if (_isPaused) return;

        _cameraRotInput = context.ReadValue<Vector2>();
    }

    // 마우스 휠
    public void HandlePlayerScrollWhellInput(InputAction.CallbackContext context)
    {
        if (_isPaused) return;

        _cameraZoomInput = context.ReadValue<Vector2>();
    }

    // 좌클릭 입력 토글
    public void HandleAttackInput(InputAction.CallbackContext context)
    {
        if (_isPaused) return;

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
        if (_isPaused) return;

        if (context.performed)
        {
            OnSniperInput?.Invoke();
        }
    }

    // Tab키(오른쪽 UI)
    public void HandleToggleRightUIInput(InputAction.CallbackContext context)
    {
        if (_isPaused) return;

        if (context.performed)
        {
            OnToggleRightUIInput?.Invoke();
        }
    }

    // M키(미니맵)
    public void HandleMapInput(InputAction.CallbackContext context)
    {
        if (_isPaused) return;

        if (context.performed)
        {
            OnMapInput?.Invoke();
        }
    }

    // Alt키(조준점 고정, 카메라)
    public void HandleFreeLookInput(InputAction.CallbackContext context)
    {
        if (_isPaused) return;

        if (context.started) OnFreeLookInput?.Invoke(true);
        if (context.canceled) OnFreeLookInput?.Invoke(false);
    }

    // ESC키(일시정지 메뉴)
    public void HandlePauseInput(InputAction.CallbackContext context)
    {
        if (context.performed) OnPauseInput?.Invoke();
    }

    /// <summary>
    /// 일시정지 시 입력 막아놓고 초기화
    /// </summary>
    public void SetPause(bool enabled)
    {
        _isPaused = enabled;

        // 키 입력 초기화
        if (_isPaused)
        {
            _cameraRotInput = Vector2.zero; // 카메라
            if (enabled) OnAttackInput?.Invoke(false); // 공격 상태
        }
    }
}
