using System.Collections;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 게임 씬 관리
/// </summary>
public class GameScene : MonoBehaviour
{
    [Header("----- 치트 -----")]
    [SerializeField] int _cheatItemNum;
    [SerializeField] bool _cheatItemActive;
    [SerializeField] bool StageClear;
    [SerializeField] int _cheatGoldAmount;
    [SerializeField] bool _cheatGoldActive;

    [Header("----- 컴포넌트 -----")]
    [SerializeField] Camera _mainCamera;
    [SerializeField] InputSystemHandler _inputSystemHandler;
    [SerializeField] PlayerTank _player;
    [SerializeField] CameraTarget _cameraTarget;
    [SerializeField] SniperModeController _sniperMode;
    [SerializeField] CinemachineBrain _cinemachineBrain; // 블렌드 방식 변경용
    [SerializeField] SceneEffect _sceneEffect;
    [SerializeField] HitDirectionIndicator _hitDirectionIndicator; // 피격 방향 표시기
    [SerializeField] ItemEffectHandler _itemEffectHandler; // 아이템 사용
    // UI
    [SerializeField] RightPanelUI _rightPanelUI; // 오른쪽 메뉴 UI
    [SerializeField] MiniMapUI _miniMapUI;       // 미니맵 UI
    [SerializeField] GameInfoUI _gameInfoUI;     // 게임 정보 UI
    [SerializeField] EnemySpawnUI _enemySpawnUI; // 적 스폰 UI
    [SerializeField] GameOverUI _gameOverUI;     // 게임오버 UI
    [SerializeField] StageClearUI _stageClearUI; // 스테이지 클리어 UI
    [SerializeField] GameClearUI _gameClearUI;   // 게임 클리어 UI
    [SerializeField] PauseUI _pauseUI;           // 일시정지 UI
    [SerializeField] InventoryUI _inventoryUI;   // 인벤토리 UI
    [SerializeField] WarningUI _warningUI;       // 경고 UI
    [SerializeField] ShopUI _shopUI;             // 상점 UI
    [SerializeField] EquipmentUI _equipmentUI;   // 장비 UI
    [SerializeField] QuickSlotUI _quickSlotUI;   // 퀵슬롯 UI

    Vector3 _playerSpawnPoint; // 플레이어 시작 지점
    StageScene _currentStage; // 현재 스테이지

    bool _isGameOver;
    bool _isPaused;
    bool _isShopOpen;

    bool _OnCursor; // 마우스 커서 활성화 여부

    AsyncOperation _currentStageUnload; // 현재 씬 언로드 할 거
    AsyncOperation _nextStageLoad; // 다음 스테이지 미리 로드 할 거

    Coroutine _hyperShieldRoutine;

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
        _inputSystemHandler.OnInventoryInput += HandleInventoryInput;
        _inputSystemHandler.OnInteractInput += HandleInteractInput;
        _inputSystemHandler.OnCursorInput += HandleCursorInput;
        _inputSystemHandler.OnQuickSlotInput += HandleQuickSlotInput;

        GameManager.Instance.PlayerData.OnGoldChanged += _gameInfoUI.UpdateGold;
        GameManager.Instance.PlayerData.OnLifeChanged += _gameInfoUI.UpdateLife;

        _rightPanelUI.OnPauseClicked += HandlePauseInput;
        _pauseUI.OnResumeClicked += HandlePauseInput;
        _pauseUI.OnRestartClicked += HandleRestartStage;
        _pauseUI.OnMainMenuClicked += HandleTitleRequested;
        //_pauseUI.OnTutorialClicked += HandleTutorial;

        _gameOverUI.RestartRequested += HandleRestartStage;
        _gameOverUI.TitleRequested += HandleTitleRequested;
        _gameClearUI.TitleRequested += HandleTitleRequested;
        _inventoryUI.Presenter.OnItemUsed += _itemEffectHandler.Use;
        _shopUI.OnExitClicked += HandleShopExit;

        _player.Model.OnDead += HandlePlayerDead;
        _player.OnPlayerRespawn += HandlePlayerRespawn;

        GameManager.Instance.EquipmentManager.Initialize(_player.Model);
        GameManager.Instance.OptionManager.OnMouseSensitivityChanged += _cameraTarget.SetSensitivity;

        // 아이템 효과들
        // 목숨 증가
        _itemEffectHandler.OnLifeUp += () =>
        {
            GameManager.Instance.PlayerData.AddLife(1);
        };
        // 기지 무적
        _itemEffectHandler.OnBaseShield += duration =>
        {
            _currentStage.BaseWall.ActivateShield(duration);
        };
        // 나 무적
        _itemEffectHandler.OnHyperShield += duration =>
        {
            if (_hyperShieldRoutine != null) StopCoroutine(_hyperShieldRoutine);
            _hyperShieldRoutine = StartCoroutine(HyperShieldRoutine(duration));
        };
        // 적 멈춤
        _itemEffectHandler.OnEMPField += duration =>
        {
            _currentStage.EnemySpawner.StartEMPField(duration);
        };
        // 폭탄
        _itemEffectHandler.OnAirSupport += () =>
        {
            _currentStage.EnemySpawner.DestroyAllEnemies();
        };

        // 이어줌
        _player.OnDamaged += _sceneEffect.ShowDamageEffect;
        _player.OnHit += _hitDirectionIndicator.Show;

        // 목숨 UI 갱신
        _gameInfoUI.UpdateLife(GameManager.Instance.PlayerData.Life);

        // 인벤토리 세팅
        _player.ItemPickup.Initialize(_inventoryUI.Presenter);
        _quickSlotUI.Initialize(_inventoryUI.Presenter);

        // 상점 세팅
        _shopUI.Initialize(_inventoryUI, _equipmentUI);

        // HACK:카메라 w값 조절(나중에 하기)
        //bool isOpen = _rightPanelUI.IsOpen;
        //UpdateCameraRect(isOpen);
    }

    /// <summary>
    /// Stage 씬 로드 완료 후 StageScene 연결
    /// </summary>
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
        _currentStage.OnHQDestroyed += HandleHQDestroyed; // 아군 기지 파괴
        _currentStage.OnBaseWallDestroyed += HandleBaseWallDestroyed; // 기지 벽 파괴
        _currentStage.EnemySpawner.OnAllEnemiesDefeated += HandleAllEnemiesDefeated; // 모든 적 격파(UI 띄울 용도)
        _currentStage.OnStageClear += HandleStageClear; // 스테이지 클리어

        // 적 스폰 UI 연동
        _currentStage.EnemySpawner.OnSpawnListReady += _enemySpawnUI.Initialize;
        _currentStage.EnemySpawner.OnEnemySpawned += _enemySpawnUI.SetEnemySpawn;

        // 플레이어 능력치 다시 세팅(장착한 거 적용)
        _player.Initialize();

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
        _player.Respawn(_playerSpawnPoint = pos, _cinemachineBrain);
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
    /// 일시정지(esc키)
    /// </summary>
    void HandlePauseInput()
    {
        // 옵션창이 열려있으면 옵션창만 닫기
        if (_pauseUI.IsOptionOpen)
        {
            _pauseUI.CloseOption();
            return;
        }

        ForceDrop();

        _isPaused = !_isPaused;
        _inputSystemHandler.SetInputDisabled(_isPaused);
        _pauseUI.SetActive(_isPaused);
        Time.timeScale = _isPaused ? 0f : 1f;

        if (_isShopOpen) return; // 상점 열렸을 땐 항상 보이기
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
    /// 퀵슬롯
    /// </summary>
    void HandleQuickSlotInput(int slotIndex)
    {
        if (_OnCursor) return;
        if (_player.IsDead) return;
        _quickSlotUI.UseSlot(slotIndex);
    }

    /// <summary>
    /// 커서 보이기
    /// </summary>
    void HandleCursorInput(bool isActive)
    {
        if (_isShopOpen) return; // 상점 열렸을 땐 항상 보이기
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

        if (GameManager.Instance.PlayerData.Life > 0)
        {
            // 목숨 UI 갱신
            GameManager.Instance.PlayerData.SpendLife(1);

            // 리스폰
            _player.Respawn(_playerSpawnPoint, _cinemachineBrain);
            _cameraTarget.ResetRotation();
        }
        else
        {
            // 게임오버
            _gameOverUI.Show(false);
        }
    }

    /// <summary>
    /// 기지 벽 파괴됨
    /// </summary>
    void HandleBaseWallDestroyed()
    {
        _warningUI.Show();
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

        // 다음 스테이지 미리 로드
        _currentStageUnload = SceneManager.UnloadSceneAsync(_currentStage.SceneName);

        // 마지막 스테이지면 상점 없이 게임 클리어 UI 표시
        // HACK:게임 클리어에서 이어하기 하면 상점 나오게 할거임
        if (_currentStage.StageID == GameManager.Instance.DataManager.LastStageID)
        {
            _OnCursor = true;
            _inputSystemHandler.SetInputDisabled(true);
            _player.SetPlayerGravity(false); // 씬 언로드 중 자유낙하 방지
            _gameClearUI.Show();
            return;
        }

        _nextStageLoad = SceneManager.LoadSceneAsync(_currentStage.NextStageName, LoadSceneMode.Additive);
        _nextStageLoad.allowSceneActivation = false;

        // 상점 열기
        _isShopOpen = true;
        _OnCursor = true;
        _shopUI.SetShopActive(true);
        _inventoryUI.EnterStore(_shopUI.RightPanelTr); // UI 위치 옮김
        _inputSystemHandler.SetInputDisabled(true);
        Cursor.lockState = CursorLockMode.None;

        // 상점 열렸을 땐 재도전 막아놓음
        _pauseUI.RetryBtn.SetActive(false);

        _player.EquipViewMod(true); // 장착 모드 활성화
        _player.SetPlayerGravity(false); // 중력 설정(씬 전환시 자유낙하 방지)
    }

    /// <summary>
    /// 상점 나가기(다음 스테이지)
    /// </summary>
    void HandleShopExit()
    {
        _pauseUI.RetryBtn.SetActive(true);
        _player.EquipViewMod(false); // 장착 모드 비활성화
        StartCoroutine(LoadStageRoutine(_currentStage.NextStageName));
    }

    /// <summary>
    /// 스테이지 재시작
    /// </summary>
    void HandleRestartStage()
    {
        if (_isShopOpen)
        {
            Debug.Log("상점 열렸을 때는 재시작 막아놓음"); // 실제로 눌릴 일은 없음
        }
        else
        {
            StartCoroutine(LoadStageRoutine(_currentStage.SceneName));
        }
    }

    IEnumerator LoadStageRoutine(string sceneName)
    {

        // 일시정지 관련 초기화
        _isPaused = false;
        _OnCursor = false;
        _inputSystemHandler.SetInputDisabled(_isPaused);
        _pauseUI.SetActive(_isPaused);
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;

        // 상점 닫기
        if (_isShopOpen)
        {
            _isShopOpen = false;
            _shopUI.SetShopActive(false);
            _inventoryUI.ExitStore(); // UI 위치 복귀
        }

        // 로딩 이미지 띄우기
        LoadingUI loadingUI = GameManager.Instance.LoadingUI;
        loadingUI.Show();

        // 스테이지 구독 해제
        UnsubscribeStage();

        // 현재 Stage 씬 언로드
        if (_currentStageUnload != null)
        {
            yield return _currentStageUnload;
            _currentStageUnload = null;
        }
        else
        {
            AsyncOperation stageUnload = SceneManager.UnloadSceneAsync(_currentStage.SceneName);
            while (stageUnload.isDone == false)
            {
                yield return null;
            }
        }
        _currentStage = null;

        // 다음 Stage 씬 로드
        AsyncOperation stageLoad;
        if (_nextStageLoad != null)
        {
            // 미리 로드된 씬 활성화
            stageLoad = _nextStageLoad;
            stageLoad.allowSceneActivation = true;
            _nextStageLoad = null;
        }
        else
        {
            stageLoad = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        }

        // 로딩바 업데이트
        StartCoroutine(loadingUI.UpdateProgress(stageLoad));
        yield return new WaitUntil(() => stageLoad.progress >= 0.9f); // 실제로 기다리는 거

        _player.SetPlayerGravity(true); // 중력 복구

        // 카메라 초기화
        _cameraTarget.ResetRotation();
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

    /// <summary>
    /// 무적 아이템 사용
    /// </summary>
    IEnumerator HyperShieldRoutine(float duration)
    {
        _player.Model.SetNoDamage(true);
        _player.SetShieldEffect(true);

        // 이펙트 색 변경
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float ratio = 1f - (elapsed / duration); // 1에서 0으로 감소
            _player.UpdateShieldColor(ratio);
            yield return null;
        }

        _player.Model.SetNoDamage(false);
        _player.SetShieldEffect(false);
    }

    void Update()
    {
#if UNITY_EDITOR
        if (_cheatItemActive) // 아이템 치트
        {
            _cheatItemActive = false;
            AddCheatItem(_cheatItemNum);
        }
        if (StageClear) // 클리어 치트
        {
            StageClear = false;
            HandleStageClear();
        }
        if (_cheatGoldActive) // 돈 치트
        {
            _cheatGoldActive = false;
            GameManager.Instance.PlayerData.AddGold(_cheatGoldAmount);
        }
#endif
    }

    /// <summary>
    /// 치트로 아이템 추가
    /// </summary>
    void AddCheatItem(int itemID)
    {
        ItemConfig config = GameManager.Instance.DataManager.GetItemConfig(itemID);
        if (config == null)
        {
            Debug.Log($"{itemID}는 없는 아이템");
            return;
        }

        ItemModel item = new ItemModel(config);
        _inventoryUI.Presenter.AddItem(item);
    }
}
