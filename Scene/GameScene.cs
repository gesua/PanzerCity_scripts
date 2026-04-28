using UnityEditor.Overlays;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 게임 씬 관리
/// </summary>
public class GameScene : MonoBehaviour
{
    [Header("----- 컴포넌트 -----")]
    [SerializeField] InputSystemHandler _inputSystemHandler;
    [SerializeField] PlayerTank _player;
    [SerializeField] CameraTarget _cameraTarget;
    [SerializeField] SniperModeController _sniperMode;
    // UI
    [SerializeField] RightPanelUI _rightPanelUI;        // 오른쪽 메뉴 UI
    [SerializeField] MiniMapUI _miniMapUI;              // 미니맵 UI
    [SerializeField] GameInfoUI _gameInfoUI;            // 게임 정보 UI
    [SerializeField] TankDirectionUI _tankDirectionUI;  // 탱크 방향 UI
    [SerializeField] EnemySpawnUI _enemySpawnUI;        // 적 스폰 UI
    [SerializeField] GameOverUI _gameOverUI;            // 게임오버 UI
    [Header("----- 런타임 데이터 -----")]
    [SerializeField] int _playerLife = 3;

    Vector3 _playerSpawnPoint; // 플레이어 시작 지점
    StageScene _currentStage; // 현재 스테이지

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

        // 목숨 UI 갱신
        _gameInfoUI.UpdateLife(_playerLife);
    }

    // Stage 씬 로드 완료 후 StageScene 연결
    void OnEnable()
    {
        SceneManager.sceneLoaded += OnStageLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnStageLoaded;
    }

    void OnStageLoaded(Scene scene, LoadSceneMode mode)
    {
        _currentStage = FindAnyObjectByType<StageScene>();
        if (_currentStage == null) return;

        // 스테이지 UI 세팅
        _gameInfoUI.UpdateStage(_currentStage.StageID - 7100); // 스테이지 ID값 빼줌(7100)

        _currentStage.OnHQDestroyed += HandleHQDestroyed;

        // 적 스폰 UI 연동
        _currentStage.EnemySpawner.OnSpawnListReady += _enemySpawnUI.Initialize;
        _currentStage.EnemySpawner.OnEnemySpawned += _enemySpawnUI.SetEnemySpawn;

        // 리스폰
        _currentStage.OnStageLoaded += pos => _player.Respawn(_playerSpawnPoint = pos);
    }

    /// <summary>
    /// 스테이지 이벤트 구독 해제
    /// </summary>
    void UnsubscribeStage()
    {
        if (_currentStage == null) return;

        _currentStage.OnHQDestroyed -= HandleHQDestroyed;
        _currentStage.EnemySpawner.OnSpawnListReady -= _enemySpawnUI.Initialize;
        _currentStage.EnemySpawner.OnEnemySpawned -= _enemySpawnUI.SetEnemySpawn;
        _currentStage = null;
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
        if (_player.IsDead) return;

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
        if (_player.IsDead) return;

        _sniperMode.ToggleSniperMode();
        _player.SetSniperMode(_sniperMode.IsSniper);
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
        // 방향 UI 멈춤
        _tankDirectionUI.SetActive(false);

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

            // 방향 UI 켬
            _tankDirectionUI.SetActive(true);

            // 리스폰
            _player.Respawn(_playerSpawnPoint);
            _cameraTarget.ResetRotation();
        }
        else
        {
            // 게임오버
            _gameOverUI.Show(false);
        }
    }

    /// <summary>
    /// HQ 파괴되서 게임오버
    /// </summary>
    void HandleHQDestroyed()
    {
        _player.DisablePlayerAndUI(); // 플레이어 움직임 막고, UI 없앰
        _gameOverUI.Show(true);
    }
}
