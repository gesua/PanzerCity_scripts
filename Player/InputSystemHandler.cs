using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputSystemHandler : MonoBehaviour
{
    public event Action<Vector2> OnMoveInput;
    public event Action<Vector2> OnCameraRotInput;

    Vector2 _moveInput;
    Vector2 _cameraRotInput;

    private void Update()
    {
        OnMoveInput?.Invoke(_moveInput);
        OnCameraRotInput?.Invoke(_cameraRotInput);
    }

    public void HandleMoveInput(InputAction.CallbackContext context)
    {
        _moveInput = context.ReadValue<Vector2>();
    }

    public void HandleCameraRotInput(InputAction.CallbackContext context)
    {
        _cameraRotInput = context.ReadValue<Vector2>();
    }
}
