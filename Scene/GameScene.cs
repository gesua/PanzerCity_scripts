using System.Collections;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 게임 씬 관리
/// </summary>
public class GameScene : MonoBehaviour
{
    [Header("----- 컴포넌트 -----")]
    [SerializeField] Camera _mainCamera;
    [SerializeField] InputSystemHandler _inputSystemHandler;
    [SerializeField] PlayerTank _player;
    [SerializeField] CameraTarget _cameraTarget;
    [SerializeField] SniperModeController _sniperMode;
    [SerializeField] CinemachineBrain _cinemachineBrain; // 블렌드 방식 변경용
    [SerializeField] SceneEffect _sceneEffect;
    [SerializeField] HitDirectionIndicator _hitDirectionIndicator; // 피격 방향 표시기
    // UI
    [SerializeField] RightPanelUI _rightPanelUI; // 오른쪽 메뉴 UI
    [SerializeField] MiniMapUI _miniMapUI;       // 미니맵 UI
    [SerializeField] GameInfoUI _gameInfoUI;     // 게임 정보 UI
    [SerializeField] EnemySpawnUI _enemySpawnUI; // 적 스폰 UI
    [SerializeField] GameOverUI _gameOverUI;     // 게임오버 UI
    [SerializeField] StageClearUI _stageClearUI; // 스테이지 클리어 UI
    [SerializeField] PauseUI _pauseUI;           // 일시정지 UI
    [SerializeField] InventoryUI _inventoryUI;   // 인벤토리 UI
    [Header("----- 런타임 데이터 -----")]
    [SerializeField] int _playerLife = 3;       // 목숨

    Vector3 _playerSpawnPoint; // 플레이어 시작 지점
    StageScene _currentStage; // 현재 스테이지

    bool _isGameOver;
    bool _isPaused;

    bool _OnCursor; // 마우스 커서 활성화 여부

    // 카메라 w크기 관련
    float _rightPanelPixelWidth = 350f; // 오른쪽 패널 픽셀 너비

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
        _inputSystemHandler.OnFreeLookInput += HandleFreeLookInput;
        _inputSystemHandler.OnPauseInput += HandlePauseInput;
        _rightPanelUI.OnPauseClicked += HandlePauseInput;
        _pauseUI.OnResumeClicked += HandlePauseInput;
        _pauseUI.OnRestartClicked += HandleRestartStage;
        _pauseUI.OnMainMenuClicked += HandleTitleRequested;
        //_pauseUI.OnTutorialClicked += HandleTutorial;
        _inputSystemHandler.OnInventoryInput += HandleInventoryInput;
        _inputSystemHandler.OnInteractInput += HandleInteractInput;
        _inputSystemHandler.OnCursorInput += HandleCursorInput;
        _player.Model.OnDead += HandlePlayerDead;
        _player.OnPlayerRespawn += HandlePlayerRespawn;
        _gameOverUI.RestartRequested += HandleRestartStage;
        _gameOverUI.TitleRequested += HandleTitleRequested;

        // 이어줌
        _player.OnDamaged += _sceneEffect.ShowDamageEffect;
        _player.OnHit += _hitDirectionIndicator.Show;

        // 목숨 UI 갱신
        _gameInfoUI.UpdateLife(_playerLife);

        // 인벤토리 초기화
        _player.ItemPickup.Initialize(_inventoryUI.Presenter);

        // HACK:카메라 w값 조절(나중에 하기)
        //bool isOpen = _rightPanelUI.IsOpen;
        //UpdateCameraRect(isOpen);
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

        // 이벤트 구독
        _currentStage.OnHQDestroyed += HandleHQDestroyed; // 아군 기지 격파
        _currentStage.EnemySpawner.OnAllEnemiesDefeated += HandleAllEnemiesDefeated; // 모든 적 격파(UI 띄울 용도)
        _currentStage.OnStageClear += HandleStageClear; // 스테이지 클리어

        // 적 스폰 UI 연동
        _currentStage.EnemySpawner.OnSpawnListReady += _enemySpawnUI.Initialize;
        _currentStage.EnemySpawner.OnEnemySpawned += _enemySpawnUI.SetEnemySpawn;


        // 리스폰
        _currentStage.OnStageLoaded += HandleStageLoaded;
    }

    /// <summary>
    /// 스테이지 이벤트 구독 해제
    /// </summary>
    void UnsubscribeStage()
    {
        if (_currentStage == null) return;

        _currentStage.OnHQDestroyed -= HandleHQDestroyed;
        _currentStage.OnStageClear -= HandleStageClear;
        _currentStage.EnemySpawner.OnAllEnemiesDefeated -= HandleAllEnemiesDefeated;
        _currentStage.EnemySpawner.OnSpawnListReady -= _enemySpawnUI.Initialize;
        _currentStage.EnemySpawner.OnEnemySpawned -= _enemySpawnUI.SetEnemySpawn;
        _currentStage.OnStageLoaded -= HandleStageLoaded;
    }

    /// <summary>
    /// 스테이지 불러와짐
    /// </summary>
    void HandleStageLoaded(Vector3 pos)
    {
        _player.Respawn(_playerSpawnPoint = pos);
    }

    /// <summary>
    /// 이동
    /// </summary>
    void HandleMoveInput(Vector2 inputVector)
    {
        // x,y 축을 x,z축으로 변경
        Vector3 moveVector = Vector3.forward * inputVector.y + Vector3.right * inputVector.x;
        _player.Move(moveVector);
    }

    /// <summary>
    /// 카메라
    /// </summary>
    void HandleCameraRotateInput(Vector2 inputVector)
    {
        if (_OnCursor) return; // 커서 보일 땐 잠금

        _cameraTarget.Rotate(inputVector);
    }

    /// <summary>
    /// 좌클릭 누르고 있는 동안에 자동 발사
    /// </summary>
    void HandleAttackInput(bool isAttack)
    {
        if (_OnCursor) return; // 커서 보일 땐 잠금
        if (_player.IsDead) return;

        _player.SetIsAttack(isAttack);
    }

    void HandleCameraZoomInput(Vector2 inputVector)
    {
        _cameraTarget.Zoom(inputVector);
    }

    /// <summary>
    /// 저격 모드 토글
    /// </summary>
    void HandleSniperInput()
    {
        if (_player.IsDead) return;

        _sniperMode.ToggleSniperMode();
        _player.SetSniperMode(_sniperMode.IsSniper);
        _cameraTarget.SetSniperMode(_sniperMode.IsSniper);
    }

    /// <summary>
    /// 오른쪽 메뉴 UI 토글
    /// </summary>
    void HandleToggleRightUIInput()
    {
        ForceDrop();
        _rightPanelUI.Toggle();
    }

    /// <summary>
    /// HACK:카메라 w값 조절(나중에 하기)
    /// </summary>
    void UpdateCameraRect(bool isPanelOpen)
    {
        float ratio = isPanelOpen ? 1f - (_rightPanelPixelWidth / Screen.width) : 1f;
        _mainCamera.rect = new Rect(0f, 0f, ratio, 1f);
    }

    /// <summary>
    /// 맵 토글
    /// </summary>
    void HandleMapInput()
    {
        ForceDrop();
        _miniMapUI.Toggle();
    }

    /// <summary>
    /// 자유 시점
    /// </summary>
    void HandleFreeLookInput(bool isFreeLook)
    {
        _player.SetAimLocked(isFreeLook);
    }

    /// <summary>
    /// 일시정지
    /// </summary>
    void HandlePauseInput()
    {
        ForceDrop();

        _isPaused = !_isPaused;
        _inputSystemHandler.SetPause(_isPaused);
        _pauseUI.SetActive(_isPaused);
        Time.timeScale = _isPaused ? 0f : 1f;

        _OnCursor = _isPaused;
        Cursor.lockState = _isPaused ? CursorLockMode.None : CursorLockMode.Locked;
    }

    /// <summary>
    /// 인벤토리
    /// </summary>
    void HandleInventoryInput()
    {
        ForceDrop();
        _inventoryUI.Toggle();
    }

    /// <summary>
    /// 상호작용
    /// </summary>
    void HandleInteractInput()
    {
        _player.ItemPickup.TryPickup();
    }

    /// <summary>
    /// 커서 보이기
    /// </summary>
    void HandleCursorInput(bool isActive)
    {
        if (_isPaused) return; // 일시정지 중엔 항상 보이기

        _OnCursor = isActive;
        Cursor.lockState = isActive ? CursorLockMode.None : CursorLockMode.Locked;
        _player.SetIsAttack(false); // 자동 공격중인거 취소

        if (isActive == false) ForceDrop();
    }

    /// <summary>
    /// 드래그 중이면 강제 드롭
    /// </summary>
    void ForceDrop()
    {
        if (_inventoryUI.Presenter.IsDragging)
        {
            _inventoryUI.Presenter.ForceDrop();
        }
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
        if (_isGameOver) return; // HQ 파괴되면 리스폰 막기

        if (_playerLife > 0)
        {
            // 목숨 UI 갱신
            _playerLife--;
            _gameInfoUI.UpdateLife(_playerLife);

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
        _isGameOver = true;

        _player.DisablePlayerAndUI(); // 플레이어 움직임 막고, UI 없앰

        // 시네머신 블렌드 방식 변경(저격은 cut)
        _cinemachineBrain.DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.EaseInOut, 1f);

        StartCoroutine(GameOverRoutine());
    }

    IEnumerator GameOverRoutine()
    {
        yield return new WaitForSecondsRealtime(2f); // 카메라 전환 시간보다 1초 더 기다리기
        Time.timeScale = 1f; // 시간 재생

        _gameOverUI.Show(true);
    }

    /// <summary>
    /// 모든 적 격파
    /// </summary>
    void HandleAllEnemiesDefeated()
    {
        StartCoroutine(_stageClearUI.Show());
    }

    /// <summary>
    /// 스테이지 클리어
    /// </summary>
    void HandleStageClear()
    {
        _stageClearUI.Hide();
        string nextScene = "Stage" + (_currentStage.StageID - 7100 + 1).ToString("D2");
        StartCoroutine(LoadStageRoutine(nextScene));
    }

    /// <summary>
    /// 스테이지 재시작
    /// </summary>
    void HandleRestartStage()
    {
        StartCoroutine(LoadStageRoutine(_currentStage.SceneName));
    }

    IEnumerator LoadStageRoutine(string sceneName)
    {
        // 일시정지 관련 초기화
        _isPaused = false;
        _inputSystemHandler.SetPause(_isPaused);
        _pauseUI.SetActive(_isPaused);
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;

        // 로딩 이미지 띄우기
        LoadingUI loadingUI = GameManager.Instance.LoadingUI;
        loadingUI.Show();

        // 스테이지 구독 해제
        UnsubscribeStage();

        // 현재 Stage 씬 언로드
        yield return SceneManager.UnloadSceneAsync(_currentStage.SceneName);

        _currentStage = null;

        // 다음 Stage 씬 로드
        AsyncOperation stageLoad = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        yield return StartCoroutine(loadingUI.UpdateProgress(stageLoad));

        // 로딩바 업데이트
        yield return StartCoroutine(loadingUI.UpdateProgress(stageLoad));

        // 플레이어, 카메라, UI 초기화
        _cameraTarget.ResetRotation();
        _gameInfoUI.UpdateLife(_playerLife);
        _isGameOver = false;

        yield return new WaitForSeconds(0.1f); // 잠깐 기다리기
        loadingUI.Hide();
    }

    /// <summary>
    /// 메인화면으로 가기
    /// </summary>
    void HandleTitleRequested()
    {
        Time.timeScale = 1f;
        UnsubscribeStage();
        SceneManager.LoadScene("Title");
    }
}
