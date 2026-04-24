using UnityEngine;

/// <summary>
/// 씬 관리
/// </summary>
public class StageScene : MonoBehaviour
{
    [Header("----- 컴포넌트 -----")]
    [SerializeField] InputSystemHandler _inputSystemHandler;
    [SerializeField] PlayerTank _player;
    [SerializeField] CameraTarget _cameraTarget;
    [SerializeField] SniperModeController _sniperMode;
    [SerializeField] RightPanelUI _rightPanelUI; // 오른쪽 메뉴 UI
    [SerializeField] MiniMapUI _miniMapUI; // 미니맵 UI
    [SerializeField] GameInfoUI _gameInfoUI; // 게임 정보 UI
    [SerializeField] GameOverUI _gameOverUI; // 게임오버 UI
    [SerializeField] EnemySpawner _enemySpawner;
    [SerializeField] Transform _playerSpawnPoint; // 플레이어 시작 지점
    [Header("----- 런타임 데이터 -----")]
    [SerializeField] int _stageID; // 현재 스테이지 ID
    [SerializeField] int _playerLife = 3;

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;

        _inputSystemHandler.OnMoveInput += HandleMoveInput;
        _inputSystemHandler.OnCameraRotInput += HandleCameraRotateInput;
        _inputSystemHandler.OnMouseScrollInput += HandleCameraZoomInput;
        _inputSystemHandler.OnAttackInput += HandleAttackInput;
        _inputSystemHandler.OnSniperInput += HandleSniperInput;
        _inputSystemHandler.OnToggleRightUIInput += HandleToggleRightUIInput;
        _inputSystemHandler.OnMapInput += HandleMapInput;
        _player.OnPlayerDead += HandlePlayerDead;
        _player.OnPlayerRespawn += HandlePlayerRespawn;

        // 게임 정보 UI 갱신(스테이지, 목숨)
        _gameInfoUI.UpdateStage(_stageID - 7100); // 고유 ID값 빼줌(7100)
        _gameInfoUI.UpdateLife(_playerLife);

        // 적 스포너 초기화
        _enemySpawner.Initialize(_stageID);
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
        _player.SetIsAttack(isAttack);
    }

    void HandleCameraZoomInput(Vector2 inputVector)
    {
        // 저격 모드가 아닐 때만 줌 조절
        if (_sniperMode.IsSniper == false)
        {
            _cameraTarget.Zoom(inputVector);
        }
    }

    /// <summary>
    /// 저격 모드 토글
    /// </summary>
    void HandleSniperInput()
    {
        _sniperMode.ToggleSniperMode();
        _player.SetSniperMode(_sniperMode.IsSniper);

        // 조준점(+) 방향으로 카메라 유지
        if (_sniperMode.IsSniper)
        {
            //_cameraTarget.AlignToDirection(_player.BarrelForward); // HACK:조준점 맞추는거 해결중
            _cameraTarget.AlignToScreenPoint(0.75f);
        }
        else
        {
            //_cameraTarget.AlignToDirection(_player.BarrelForward);
            _cameraTarget.AlignToScreenPoint(0.25f);
        }
    }

    /// <summary>
    /// 오른쪽 메뉴 UI 토글
    /// </summary>
    void HandleToggleRightUIInput()
    {
        _rightPanelUI.Toggle();
    }

    /// <summary>
    /// 맵 토글
    /// </summary>
    void HandleMapInput()
    {
        _miniMapUI.Toggle();
    }

    /// <summary>
    /// 플레이어 사망
    /// </summary>
    void HandlePlayerDead()
    {
        // 저격 모드 중이면 해제
        if (_sniperMode.IsSniper)
        {
            _sniperMode.SetSniperMode(false);
            _player.SetSniperMode(false);
        }
    }

    /// <summary>
    /// 플레이어 리스폰
    /// </summary>
    void HandlePlayerRespawn()
    {
        if (_playerLife > 0)
        {
            // 목숨 UI 갱신
            _playerLife--;
            _gameInfoUI.UpdateLife(_playerLife);

            // 리스폰
            _player.Respawn(_playerSpawnPoint.position);
            _cameraTarget.ResetRotation();
        }
        else
        {
            // 게임오버
            _gameOverUI.Show();
        }
    }
}
