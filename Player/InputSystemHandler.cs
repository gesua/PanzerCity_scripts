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
    public event Action OnAttackStarted;
    public event Action OnAttackCanceled;
    public event Action OnSniperInput;

    Vector2 _moveInput;
    Vector2 _cameraRotInput;
    Vector2 _cameraZoomInput;

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

    // 좌클릭
    public void HandleAttackInput(InputAction.CallbackContext context)
    {
        if (context.started) OnAttackStarted?.Invoke();
        if (context.canceled) OnAttackCanceled?.Invoke();
    }

    // Shift키
    public void HandleSniperInput(InputAction.CallbackContext context)
    {
        if (context.performed == true)
        {
            OnSniperInput?.Invoke();
        }
    }
}
