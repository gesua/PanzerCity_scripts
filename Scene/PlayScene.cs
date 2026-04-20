using UnityEngine;

/// <summary>
/// 씬 관리
/// </summary>
public class PlayScene : MonoBehaviour
{
    [Header("----- 컴포넌트 -----")]
    [SerializeField] InputSystemHandler _inputSystemHandler;
    [SerializeField] PlayerTank _player;
    [SerializeField] CameraTarget _cameraTarget;
    [SerializeField] SniperModeController _sniperMode;

    bool _isFiring; // 좌클릭 누르고 있는 상태인지

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;

        _inputSystemHandler.OnMoveInput += HandleMoveInput;
        _inputSystemHandler.OnCameraRotInput += HandleCameraRotateInput;
        _inputSystemHandler.OnMouseScrollInput += HandleCameraZoomInput;
        _inputSystemHandler.OnAttackInput += HandleAttackInput;
        _inputSystemHandler.OnSniperInput += HandleSniperInput;
    }

    void HandleMoveInput(Vector2 inputVector)
    {
        // x,y 축을 x,z축으로 변경
        Vector3 moveVector = Vector3.forward * inputVector.y + Vector3.right * inputVector.x;
        _player.Move(moveVector);
    }

    void HandleCameraRotateInput(Vector2 inputVector)
    {
        _cameraTarget.Rotate(inputVector);
    }

    /// <summary>
    /// 좌클릭 누르고 있는 동안에 자동 발사
    /// </summary>
    void HandleAttackInput(bool isAttack)
    {
        _player.IsAttack(isAttack);
    }

    void HandleCameraZoomInput(Vector2 inputVector)
    {
        // 저격 모드가 아닐 때만 줌 조절
        if (_sniperMode.IsSniper == false)
        {
            _cameraTarget.Zoom(inputVector);
        }
    }

    void HandleSniperInput()
    {
        _sniperMode.ToggleSniperMode();
        _player.SetSniperMode(_sniperMode.IsSniper);
    }
}
