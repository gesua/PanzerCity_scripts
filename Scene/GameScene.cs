using System.Collections;
using Unity.Cinemachine;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

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
    [SerializeField] SpectatorController _spectatorController; // 멀티플레이:관전 모드 카메라 전환
    [SerializeField] SniperModeController _sniperMode;
    [SerializeField] CinemachineBrain _cinemachineBrain; // 블렌드 방식 변경용
    [SerializeField] SceneEffect _sceneEffect;
    [SerializeField] HitDirectionIndicator _hitDirectionIndicator; // 피격 방향 표시기
    [SerializeField] ItemEffectHandler _itemEffectHandler; // 아이템 사용
    // UI
    [SerializeField] RightPanelUI _rightPanelUI; // 오른쪽 메뉴 UI
    [SerializeField] MiniMapUI _miniMapUI;       // 미니맵 UI
    [SerializeField] GameInfoUI _gameInfoUI;     // 게임 정보 UI
    [SerializeField] PlayerHPUI _playerHPUI;     // 플레이어 HP UI
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
    [SerializeField] DropZoneUI _dropZoneUI;     // 드롭존 UI
    [SerializeField] HUDIntroDirector _hudIntroDirector; // HUD 등장 연출
    // 멀티에서 로컬에게 전달
    [SerializeField] Image _centerCrosshairImage;      // 조준점 색상용
    [SerializeField] RectTransform _centerCrosshair;   // 포탑 조준점(+) UI
    [SerializeField] RectTransform _turretCrosshair;   // 포탑 조준점(O) UI
    [SerializeField] ReloadIndicator _reloadIndicator; // 재장전 표시 UI
    [SerializeField] TankDirectionUI _tankDirectionUI;         // 차체/포탑 방향 UI

    Vector3 _playerSpawnPoint; // 플레이어 시작 지점(싱글 전용)
    int _localSpawnIndex; // 멀티플레이:로컬 플레이어의 스폰 인덱스
    bool _hasCompletedFirstStageLoad; // 멀티플레이:스테이지 전환(2번째 이후) 판별용

    StageScene _currentStage; // 현재 스테이지

    bool _isGameOver;
    bool _isSpectating; // 멀티플레이:본인은 목숨이 다 떨어졌지만 다른 아군이 살아있어 관전 중인 상태
    bool _isPaused;
    bool _isShopOpen;
    bool _isRestarting;
    bool _isTutorial; // 튜토리얼 씬 여부
    bool _hasPlayedHudIntro = false; // 스테이지당 1회 체크 플래그

    bool _OnCursor; // 마우스 커서 활성화 여부

    AsyncOperation _currentStageUnload; // 현재 씬 언로드 할 거
    AsyncOperation _nextStageLoad; // 다음 스테이지 미리 로드 할 거

    Coroutine _hyperShieldRoutine;
    Coroutine _respawnShieldRoutine;

    // 카메라 w크기 관련
    float _rightPanelPixelWidth = 350f; // 오른쪽 패널 픽셀 너비

    private void Start()
    {
        // 멀티플레이:로컬 플레이어 스폰 신호를 기다림
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            // 목숨 UI:다른 플레이어의 목숨 변경/이탈 수신은 로컬 플레이어 스폰(Initialize)을 기다리지 않고 여기서 바로 구독
            // — 다른 플레이어가 나보다 먼저 스폰되면서 늦게 도착한 값을 수동 동기화하는 신호(PlayerNetworkOwner.OnNetworkSpawn 참고)가
            // 내가 구독하기 전에 지나가버려서 목숨 UI가 기본값으로 남는 문제 방지
            NetworkGameManager.Instance.OnPlayerLifeChanged += HandleOtherPlayerLifeChanged;
            NetworkGameManager.Instance.OnPlayerLeft += HandleOtherPlayerLeft;

            NetworkGameManager.Instance.OnLocalPlayerSpawned += HandleLocalPlayerSpawned;
        }
        else // 싱글플레이:즉시 초기화
        {
            Initialize(_player);
        }
    }

    /// <summary>
    /// 로컬 플레이어 스폰 완료(멀티플레이 전용)
    /// </summary>
    void HandleLocalPlayerSpawned(PlayerTank player)
    {
        NetworkGameManager.Instance.OnLocalPlayerSpawned -= HandleLocalPlayerSpawned;

        Initialize(player);
    }

    /// <summary>
    /// 멀티플레이:다른 플레이어의 목숨 변경 수신(NetworkGameManager가 호출)
    /// 내 것은 이미 로컬 PlayerData.OnLifeChanged 구독으로 처리되므로 제외
    /// </summary>
    void HandleOtherPlayerLifeChanged(int playerIndex, int life)
    {
        if (playerIndex == _localSpawnIndex) return;

        int displayLife = Mathf.Max(life, 0); // 완전 패배(EliminatedLife=-1)는 UI에 음수로 노출되면 안 되니 0으로 표시
        _gameInfoUI.UpdateLife(playerIndex, displayLife);
    }

    /// <summary>
    /// 멀티플레이:다른 플레이어의 연결 종료 수신(NetworkGameManager가 호출) — 해당 목숨 슬롯 비활성화
    /// 나간 클라이언트 본인은 이미 연결이 끊겨 이 신호를 받을 수 없으므로 별도 필터링 불필요
    /// </summary>
    void HandleOtherPlayerLeft(int playerIndex)
    {
        _gameInfoUI.HideLifeSlot(playerIndex);
    }

    /// <summary>
    /// 플레이어 바인딩 및 게임 씬 초기화
    /// 싱글: Start()에서 즉시 호출 / 멀티: 로컬 플레이어 스폰 후 호출
    /// </summary>
    void Initialize(PlayerTank player)
    {
        _player = player; // 로컬 플레이어 바인딩
        if (_reloadIndicator != null)
        {
            _reloadIndicator.SetCenterCrosshair(_centerCrosshair); // 싱글/멀티 공통: 재장전 완료 시 중앙 조준점 연출 연결
        }

        // 멀티플레이 전용
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            _player.SetSceneReferences(_cameraTarget, _centerCrosshair, _turretCrosshair, _centerCrosshairImage, _reloadIndicator);

            // 프리팹에 연결할 참조 주입
            _cameraTarget.SetTarget(_player.transform);
            _tankDirectionUI.SetPlayer(_player);
            _playerHPUI.SetPlayer(_player);
            _hitDirectionIndicator.SetPlayer(_player.transform);
            _sniperMode.SetPlayerVisualReferences(_player);

            _player.Initialize();

            // 멀티플레이:룸에서 배정받은 자리(PlayerIndex)를 스폰 인덱스로 사용
            // (clientId는 재접속마다 계속 증가하고 재사용도 안 돼서 스폰 인덱스로 쓸 수 없음)
            if (_player.TryGetComponent(out PlayerNetworkOwner networkOwner))
            {
                _localSpawnIndex = networkOwner.PlayerIndex;
            }

            RespawnPlayer(_currentStage.GetSpawnPoint(_localSpawnIndex));

            // 목숨 UI:접속 인원 수만큼만 슬롯 활성화
            _gameInfoUI.SetActivePlayerCount(NetworkManager.Singleton.ConnectedClientsIds.Count);

            // 목숨 UI:이미 스폰된 플레이어들(나 포함)의 현재 목숨을 초기 반영
            foreach (NetworkClient client in NetworkManager.Singleton.ConnectedClientsList)
            {
                if (client.PlayerObject == null) continue;
                if (client.PlayerObject.TryGetComponent(out PlayerNetworkOwner clientNetworkOwner) == false) continue;

                _gameInfoUI.UpdateLife(clientNetworkOwner.PlayerIndex, clientNetworkOwner.CurrentLife);
            }

            // 퀵슬롯 UI:기지 무적/EMP는 공유 상태라 누가 발동했든 전원 링을 공유해야 함
            NetworkGameManager.Instance.OnBaseShieldActivated += duration => _quickSlotUI.PlayEffectFeedback(1002, duration);
            NetworkGameManager.Instance.OnEMPFieldActivated += duration => _quickSlotUI.PlayEffectFeedback(1004, duration);

            // 멀티플레이: 모든 클라이언트 로딩 완료(게임 실제 시작 시점) 수신
            NetworkGameManager.Instance.OnAllClientsReady += HandleAllClientsReady;

            // 멀티플레이: 호스트의 재도전 신호 수신(비호스트만 실제로 반응함, NetworkGameManager에서 필터링됨)
            NetworkGameManager.Instance.OnRestartRequested += _gameOverUI.OnClickRestart;

            // 멀티플레이: 전원 사망 신호 수신(관전 중이던 클라이언트도 여기서 게임오버로 전환됨)
            NetworkGameManager.Instance.OnAllPlayersDead += HandleAllPlayersDead;

            // 멀티플레이: 방장 연결 종료 감지(LobbyManager가 UGS 로비/Netcode 연결 종료 기준으로 판별) — 메인메뉴로 강제 이동
            LobbyManager.Instance.OnHostLeft += HandleHostLeft;

            // 관전 모드:카메라 대상 순환 전환(Q/E)
            _inputSystemHandler.OnSpectatePrevInput += HandleSpectatePrevInput;
            _inputSystemHandler.OnSpectateNextInput += HandleSpectateNextInput;
        }
        else
        {
            // 목숨 UI:싱글플레이는 슬롯 1개만 쓰므로 여백 대비 글자가 작아 보이지 않도록 확대
            _gameInfoUI.SetSingleplayerScale();
        }

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
        GameManager.Instance.PlayerData.OnLifeChanged += life => _gameInfoUI.UpdateLife(_localSpawnIndex, life);

        _rightPanelUI.OnPauseClicked += HandlePauseInput;
        _pauseUI.OnResumeClicked += HandlePauseInput;
        _pauseUI.OnRestartClicked += HandleRestartStage;
        _pauseUI.OnMainMenuClicked += HandleTitleRequested;

        _gameOverUI.RestartRequested += HandleRestartStage;
        _gameOverUI.TitleRequested += HandleTitleRequested;
        _gameClearUI.TitleRequested += HandleTitleRequested;
        _inventoryUI.Presenter.OnItemUsed += _itemEffectHandler.Use;
        _shopUI.OnExitClicked += HandleShopExit;

        _player.Model.OnDead += HandlePlayerDead;
        _player.OnPlayerRespawn += HandlePlayerRespawn;
        _player.OnRespawnComplete += HandleRespawnComplete;
        _player.ItemPickup.OnAutoUsed += _itemEffectHandler.Use;
        _inventoryUI.Presenter.OnItemDropped += HandleItemDropped;

        GameManager.Instance.EquipmentManager.Initialize(_player.Model);
        // 장비 장착/해제 시 서버 쪽 TankModel 사본에도 반영(멀티에서 비호스트 클라이언트의 장비 스탯이 데미지 판정에 실제로 적용되도록)
        GameManager.Instance.EquipmentManager.OnEquipped += (slot, item) => _player.SyncEquipmentToServer(item.Config.Id, true);
        GameManager.Instance.EquipmentManager.OnUnequipped += (slot, item) => _player.SyncEquipmentToServer(item.Config.Id, false);
        GameManager.Instance.OptionManager.OnMouseSensitivityChanged += _cameraTarget.SetSensitivity;

        // 세이브 데이터
        SaveManager saveManager = GameManager.Instance.SaveManager;
        if (saveManager.Mode == SaveManager.SaveLoadMode.Continue)
        {
            saveManager.ApplyLoadedData(_inventoryUI.Presenter);
        }

        // 아이템 효과들
        // 공유 스테이지 오브젝트(BaseWall, EnemySpawner)를 건드리는 효과는 멀티에서 서버 권위로 처리해야 함
        bool isMultiplayer = NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;

        // 목숨 증가 — 로컬 개인 자원이라 싱글/멀티 구분 없이 그대로 로컬 처리
        _itemEffectHandler.OnLifeUp += () =>
        {
            GameManager.Instance.PlayerData.AddLife(1);
            GameManager.Instance.AudioManager.PlaySfx(SfxType.LifeUp);
        };
        // 기지 무적
        _itemEffectHandler.OnBaseShield += duration =>
        {
            if (isMultiplayer)
            {
                NetworkGameManager.Instance.RequestBaseShieldServerRpc(duration);
            }
            else
            {
                _currentStage.BaseWall.ActivateShield(duration);
            }
        };
        // 나 무적 — 로컬 개인 상태라 싱글/멀티 구분 없이 그대로 로컬 처리
        _itemEffectHandler.OnHyperShield += duration =>
        {
            if (_hyperShieldRoutine != null) StopCoroutine(_hyperShieldRoutine);
            _hyperShieldRoutine = StartCoroutine(HyperShieldRoutine(duration));
        };
        // 적 멈춤
        _itemEffectHandler.OnEMPField += duration =>
        {
            if (isMultiplayer)
            {
                NetworkGameManager.Instance.RequestEMPFieldServerRpc(duration);
            }
            else
            {
                _currentStage.EnemySpawner.StartEMPField(duration);
            }
        };
        // 폭탄
        _itemEffectHandler.OnAirSupport += () =>
        {
            if (isMultiplayer)
            {
                NetworkGameManager.Instance.RequestAirSupportServerRpc();
            }
            else
            {
                _currentStage.EnemySpawner.DestroyAllEnemies();
            }
        };

        // 이어줌
        _player.OnDamaged += _sceneEffect.ShowDamageEffect;
        _player.OnHit += _hitDirectionIndicator.Show;

        // 목숨 UI 갱신
        _gameInfoUI.UpdateLife(_localSpawnIndex, GameManager.Instance.PlayerData.Life);

        // 인벤토리 세팅
        _player.ItemPickup.Initialize(_inventoryUI.Presenter, () => _player.IsDead);
        _quickSlotUI.Initialize(_inventoryUI.Presenter);

        // 드롭존 세팅
        _dropZoneUI.Initialize(_inventoryUI.Presenter);

        // 상점 세팅
        _shopUI.Initialize(_inventoryUI, _equipmentUI);

        // 마우스 감도 적용
        _cameraTarget.SetSensitivity(GameManager.Instance.OptionManager.OptionData.MouseSensitivity);

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

        // 멀티플레이 로컬 플레이어 스폰 대기 중이었다면 구독 해제
        if (NetworkGameManager.Instance != null)
        {
            NetworkGameManager.Instance.OnLocalPlayerSpawned -= HandleLocalPlayerSpawned;
            NetworkGameManager.Instance.OnAllClientsReady -= HandleAllClientsReady;
            NetworkGameManager.Instance.OnRestartRequested -= _gameOverUI.OnClickRestart;
            NetworkGameManager.Instance.OnAllPlayersDead -= HandleAllPlayersDead;
        }

        if (LobbyManager.Instance != null)
        {
            LobbyManager.Instance.OnHostLeft -= HandleHostLeft;
        }

        _inputSystemHandler.OnSpectatePrevInput -= HandleSpectatePrevInput;
        _inputSystemHandler.OnSpectateNextInput -= HandleSpectateNextInput;
    }

    void OnStageLoaded(Scene scene, LoadSceneMode mode)
    {
        _currentStage = FindAnyObjectByType<StageScene>();
        if (_currentStage == null) return;

        // 스테이지 UI 세팅
        _gameInfoUI.UpdateStage(_currentStage.StageID - 7100); // 스테이지 ID값 빼줌(7100)

        // 튜토리얼 모드 여부 전달(씬 이름 기준)
        if (_player != null)
        {
            bool isTutorialScene = _currentStage.gameObject.scene.name == "Stage00";
            _player.ItemPickup.SetTutorialMode(isTutorialScene);

            if (isTutorialScene)
            {
                _pauseUI.RetryBtn.SetActive(false); // 재도전 버튼 막음

                // 튜토리얼 시작시 소모품 아이템 지급 (퀵슬롯 1~4키 입력을 암시적으로 전달)
                AddCheatItem(1002);
                AddCheatItem(1003); AddCheatItem(1003);
                AddCheatItem(1004); AddCheatItem(1004); AddCheatItem(1004);
                AddCheatItem(1005); AddCheatItem(1005); AddCheatItem(1005); AddCheatItem(1005);
            }
        }

        // 이벤트 구독
        _currentStage.OnHQDestroyed += HandleHQDestroyed; // 아군 기지 파괴
        _currentStage.OnBaseWallDestroyed += HandleBaseWallDestroyed; // 기지 벽 파괴
        _currentStage.OnAllEnemiesDefeatedNotify += HandleAllEnemiesDefeated; // 모든 적 격파(UI 띄울 용도)
        _currentStage.OnStageClear += HandleStageClear; // 스테이지 클리어

        // 적 스폰 UI 연동
        _currentStage.EnemySpawner.OnSpawnListReady += _enemySpawnUI.Initialize;
        _currentStage.EnemySpawner.OnEnemySpawned += _enemySpawnUI.SetEnemySpawn;

        // 플레이어 능력치 다시 세팅(장착한 거 적용)
        if (_player != null) _player.Initialize();

        _currentStage.OnStageLoaded += HandleStageLoaded;

        // 재시작 시 목숨, 인벤토리 복구
        if (_isRestarting)
        {
            GameManager.Instance.PlayerData.RestoreSnapshot();
            _inventoryUI.Presenter.RestoreSnapshot();
            _isRestarting = false;
        }
        else // 현재 목숨, 인벤토리 저장
        {
            GameManager.Instance.PlayerData.SaveSnapshot();
            _inventoryUI.Presenter.SaveSnapshot();

            GameManager.Instance.GameStatistics.MarkGameStart(); // 최초 1회만 동작

            // 스테이지 시작 시점 자동저장
            SaveManager saveManager = GameManager.Instance.SaveManager;
            if (saveManager.Mode != SaveManager.SaveLoadMode.None)
            {
                saveManager.SaveCurrentProgress(_currentStage.StageID, _inventoryUI.Presenter);
            }
        }

        // 멀티플레이 — 스테이지 준비 완료를 NetworkGameManager에 알림(서버만 실제로 스폰 처리)
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            NetworkGameManager.Instance?.OnStageReady(_currentStage);

            // 스테이지 전환(2번째 이후 로드)은 NGO 동기화 씬로드를 안 타서 OnLoadEventCompleted가 안 옴 — 별도 신호로 적 스폰 트리거
            if (_hasCompletedFirstStageLoad) NetworkGameManager.Instance.RequestStageLoadedServerRpc();
            _hasCompletedFirstStageLoad = true;
        }
    }

    /// <summary>
    /// 스테이지 이벤트 구독 해제
    /// </summary>
    void UnsubscribeStage()
    {
        if (_currentStage == null) return;

        _currentStage.OnHQDestroyed -= HandleHQDestroyed;
        _currentStage.OnStageClear -= HandleStageClear;
        _currentStage.OnAllEnemiesDefeatedNotify -= HandleAllEnemiesDefeated;
        _currentStage.EnemySpawner.OnSpawnListReady -= _enemySpawnUI.Initialize;
        _currentStage.EnemySpawner.OnEnemySpawned -= _enemySpawnUI.SetEnemySpawn;
        _currentStage.OnStageLoaded -= HandleStageLoaded;
    }

    /// <summary>
    /// 스테이지 불러와짐
    /// </summary>
    void HandleStageLoaded(Vector3 pos)
    {
        _playerSpawnPoint = pos;

        // pos는 항상 1P 스폰 지점(인덱스 0)이라 멀티에 그대로 쓰면 안 됨
        // 최초 스폰 배치는 Initialize(PlayerTank)가 처리하지만 그건 최초 1회뿐이라, 전환 시엔 여기서 다시 해줘야 함
        bool isMultiplayer = NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;

        if (_player != null)
        {
            // 멀티플레이:재도전으로 스테이지가 다시 시작되는 경우, 관전 모드였다면 해제하고 카메라를 본인 탱크로 복구
            if (isMultiplayer)
            {
                ExitSpectateMode();
                _cameraTarget.SetTarget(_player.transform);
            }

            Vector3 spawnPos = (isMultiplayer) ? _currentStage.GetSpawnPoint(_localSpawnIndex) : _playerSpawnPoint;
            RespawnPlayer(spawnPos);
        }

        // 스테이지 시작 소리 (싱글플레이에서만 여기서 재생, 멀티는 HandleAllClientsReady에서 재생)
        if (isMultiplayer == false)
        {
            GameManager.Instance.AudioManager.StopBgm();
            GameManager.Instance.AudioManager.PlaySfx(SfxType.StageStart);
        }
    }

    /// <summary>
    /// 멀티플레이:모든 클라이언트 준비 완료 (게임 시작)
    /// </summary>
    void HandleAllClientsReady()
    {
        GameManager.Instance.AudioManager.StopBgm();
        // 스테이지 시작 소리 (멀티플레이 전용)
        GameManager.Instance.AudioManager.PlaySfx(SfxType.StageStart);
    }

    /// <summary>
    /// 이동
    /// </summary>
    void HandleMoveInput(Vector2 inputVector)
    {
        if (_isShopOpen || _isPaused) return; // 상점이나 ESC메뉴 중엔 무시
        if (_isSpectating) return; // 관전 중엔 본인 탱크가 화면 밖에서 움직이면 안 됨

        // x,y 축을 x,z축으로 변경
        Vector3 moveVector = Vector3.forward * inputVector.y + Vector3.right * inputVector.x;
        _player.Move(moveVector);
    }

    /// <summary>
    /// 카메라
    /// </summary>
    void HandleCameraRotateInput(Vector2 inputVector)
    {
        if (_isShopOpen || _isPaused) return; // 상점이나 ESC메뉴 중엔 무시
        if (_OnCursor) return; // 커서 보일 땐 잠금

        _cameraTarget.Rotate(inputVector);
    }

    /// <summary>
    /// 좌클릭 누르고 있는 동안에 자동 발사
    /// </summary>
    void HandleAttackInput(bool isAttack)
    {
        if (_isShopOpen || _isPaused) return; // 상점이나 ESC메뉴 중엔 무시
        if (_OnCursor) return; // 커서 보일 땐 잠금
        if (_player.IsDead) return;

        _player.SetIsAttack(isAttack);
    }

    void HandleCameraZoomInput(Vector2 inputVector)
    {
        if (_isShopOpen || _isPaused) return; // 상점이나 ESC메뉴 중엔 무시
        _cameraTarget.Zoom(inputVector);
    }

    /// <summary>
    /// 저격 모드 토글
    /// </summary>
    void HandleSniperInput()
    {
        if (_isShopOpen || _isPaused) return; // 상점이나 ESC메뉴 중엔 무시
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

        // 드래그 중인거 강제 드롭
        ForceDrop();

        _isPaused = !_isPaused;
        _inputSystemHandler.SetInputDisabled(_isPaused);
        _pauseUI.SetActive(_isPaused);

        // 멀티플레이 중인지 확인 (싱글플레이일 때만 시간을 멈춤)
        bool isMultiplayer = NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;
        if (isMultiplayer == false)
        {
            Time.timeScale = _isPaused ? 0f : 1f;
        }
        /* 현재 _Multi씬에서 재도전 버튼 지움
        else
        {
            // 멀티플레이 중이라면 재도전 버튼을 숨김
            // TODO:호스트만 보이게 하고 누를 수 있게 하기
            _pauseUI.RetryBtn.SetActive(false);
        }
        */

        // 일시정지 소리
        if (_isPaused) GameManager.Instance.AudioManager.PlaySfx(SfxType.Pause);

        // 커서 설정
        if (_isShopOpen) return; // 상점 열렸을 땐 커서 항상 보이기
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
        if (_isSpectating) return; // 관전 중엔 본인 탱크가 화면 밖에서 아이템을 주우면 안 됨

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
        if (_isShopOpen || _isPaused) return; // 상점이나 일시정지 중엔 항상 보이기

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
    void HandlePlayerDead(HitData hitData)
    {
        // 저격 모드 중이면 해제
        if (_sniperMode.IsSniper)
        {
            _sniperMode.SetSniperMode(false);
            _player.SetSniperMode(false);
        }
    }

    /// <summary>
    /// 플레이어 리스폰 실행
    /// 리스폰 연출 중엔 SniperModeController가 전차장 표시에 개입하지 않도록 잠금(HandleRespawnComplete에서 해제)
    /// </summary>
    void RespawnPlayer(Vector3 spawnPos)
    {
        _sniperMode.SetCommanderVisualLocked(true);
        _player.Respawn(spawnPos, _cinemachineBrain);
    }

    /// <summary>
    /// 플레이어 리스폰
    /// </summary>
    void HandlePlayerRespawn()
    {
        if (_isGameOver) return; // HQ 파괴되면 리스폰 막기
        if (_isSpectating) return; // 관전 중엔 이미 처리된 사망이므로 재진입 방지

        if (GameManager.Instance.PlayerData.Life > 0)
        {
            // 목숨 UI 갱신
            GameManager.Instance.PlayerData.SpendLife(1);

            // 리스폰
            _cameraTarget.ResetRotation();

            // 멀티플레이:Initialize에서 캐싱해둔 로컬 인덱스로 조회
            bool isMultiplayer = NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;
            Vector3 spawnPos = (isMultiplayer) ? _currentStage.GetSpawnPoint(_localSpawnIndex) : _playerSpawnPoint;

            RespawnPlayer(spawnPos);
        }
        else
        {
            bool isMultiplayer = NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;

            if (isMultiplayer)
            {
                // 이 else 분기에 들어왔다는 것 자체가 "목숨 0 상태에서 한 번 더 사망"했다는 뜻 — 완전 패배를 네트워크에 별도로 표시
                // (목숨 0 자체는 "마지막 목숨으로 생존 중"이라 다른 클라이언트/호스트가 이 상태와 구분해야 함)
                _player.NetworkOwner.MarkEliminated();

                // 전원 사망 판정과 GameOverUI 표시는 HandleAllPlayersDead가 전담(SetRestartAvailable 포함) — 여기서 직접 띄우지 않음
                // 호스트는 MarkEliminated 호출 시점에 동기적으로 HandleAllPlayersDead가 이미 실행되지만, 클라이언트는 네트워크 왕복 후 도착하므로
                // 여기서 직접 게임오버를 띄우면 그 신호보다 먼저 _isGameOver가 true가 되어 재도전 제한(SetRestartAvailable)이 씹히는 문제가 있었음
                if (HasAliveTeammate())
                {
                    EnterSpectateMode();
                }
            }
            else
            {
                // 싱글플레이:게임오버
                _gameOverUI.Show(false);
                _isGameOver = true;
            }
        }
    }

    /// <summary>
    /// 멀티플레이:본인을 제외한 접속 클라이언트 중 완전히 패배하지 않은(목숨 0으로 생존 중 포함) 아군이 있는지 확인
    /// </summary>
    bool HasAliveTeammate()
    {
        foreach (NetworkClient client in NetworkManager.Singleton.ConnectedClientsList)
        {
            if (client.PlayerObject == null) continue;
            if (client.PlayerObject.TryGetComponent(out PlayerNetworkOwner networkOwner) == false) continue;
            if (networkOwner.IsOwner) continue; // 본인 제외

            if (networkOwner.CurrentLife != PlayerNetworkOwner.EliminatedLife) return true;
        }

        return false;
    }

    /// <summary>
    /// 관전 모드 진입 — 조작을 막고 탱크 잔해를 숨긴 뒤 카메라를 아군에게 넘김
    /// 댐핑 비활성화:관전 대상은 NetworkTransform 보간으로 들어오는 위치라, Cinemachine 댐핑이 미세한 흔들림을 오히려 증폭시켜서 관전 중엔 꺼둠
    /// </summary>
    void EnterSpectateMode()
    {
        _isSpectating = true;

        // _isDead를 세팅해서 Attack() 등 사망 가드가 걸린 로직이 정상적으로 막히게 함
        _player.DisablePlayerAndUI();

        _player.SetSpectatingVisualHidden(true);
        _cameraTarget.DisableDamping();
        _spectatorController.EnterSpectate(_cameraTarget);
    }

    /// <summary>
    /// 관전 모드 해제 — 재도전 등으로 스테이지가 다시 시작될 때 호출
    /// </summary>
    void ExitSpectateMode()
    {
        if (_isSpectating == false) return;

        _isSpectating = false;

        _player.SetSpectatingVisualHidden(false);
        _cameraTarget.ResetDamping(); // 로컬 플레이어 복귀 후엔 원래 댐핑 값으로 복원(로컬 조작 시의 카메라 연출감 유지)
        _spectatorController.ExitSpectate();
    }

    /// <summary>
    /// 관전 대상 전환(이전, Q키)
    /// </summary>
    void HandleSpectatePrevInput()
    {
        if (_isSpectating == false) return;
        if (_isPaused) return;

        _spectatorController.SpectatePrev();
    }

    /// <summary>
    /// 관전 대상 전환(다음, E키)
    /// </summary>
    void HandleSpectateNextInput()
    {
        if (_isSpectating == false) return;
        if (_isPaused) return;

        _spectatorController.SpectateNext();
    }

    /// <summary>
    /// 멀티플레이:전원 사망 — 관전 중이던 클라이언트도 여기서 게임오버로 전환됨
    /// HQ 파괴 케이스와 동일하게 호스트만 재도전 가능
    /// </summary>
    void HandleAllPlayersDead()
    {
        if (_isGameOver) return; // 이미 게임오버 된 상태에선 또 게임오버 안 됨

        _isGameOver = true;

        ExitSpectateMode(); // 관전 중이었다면 해제(잔해 다시 보이기 등은 재도전 시 리스폰으로 자연히 복구됨)

        // 멀티플레이:호스트만 재도전 버튼을 누를 수 있음(비호스트는 대기)
        bool canRestart = NetworkManager.Singleton.IsServer;
        _gameOverUI.SetRestartAvailable(canRestart);

        _gameOverUI.Show(false);
    }

    /// <summary>
    /// 멀티플레이:방장 연결 종료 수신(LobbyManager가 호출) — 남은 클라이언트를 메인메뉴로 강제 이동
    /// 정리 로직은 평소 메인메뉴 나가기(HandleTitleRequested)와 동일해서 그대로 재사용
    /// </summary>
    void HandleHostLeft()
    {
        HandleTitleRequested();
    }

    /// <summary>
    /// 리스폰 무적
    /// </summary>
    void HandleRespawnComplete(float duration)
    {
        // HUD 연출(스테이지당 첫 리스폰 완료 시에만 재생 — 이후 목숨 소비 리스폰에선 재생 안 함)
        if (_hasPlayedHudIntro == false)
        {
            _hasPlayedHudIntro = true;
            if (_hudIntroDirector != null) _hudIntroDirector.PlayIntro();
        }

        _sniperMode.SetCommanderVisualLocked(false); // 전차장 표시 제어권 반환
        if (_respawnShieldRoutine != null) StopCoroutine(_respawnShieldRoutine);
        _respawnShieldRoutine = StartCoroutine(HyperShieldRoutine(duration));
    }

    /// <summary>
    /// 인벤토리에서 바닥으로 아이템 드롭
    /// </summary>
    void HandleItemDropped(ItemConfig config)
    {
        _player.DropItem(config);
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
        if (_isGameOver) return; // 이미 게임오버 된 상태에선 또 게임오버 안 됨

        _isGameOver = true;

        _player.DisablePlayerAndUI(); // 플레이어 움직임 막고, UI 없앰

        // 시네머신 블렌드 방식 변경(저격은 cut)
        _cinemachineBrain.DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.EaseInOut, 1f);

        // 멀티플레이:호스트만 재도전 버튼을 누를 수 있음(비호스트는 대기)
        bool isMultiplayer = (NetworkManager.Singleton != null) && NetworkManager.Singleton.IsListening;
        bool canRestart = (isMultiplayer == false) || NetworkManager.Singleton.IsServer;
        _gameOverUI.SetRestartAvailable(canRestart);

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
        GameManager.Instance.AudioManager.PlaySfx(SfxType.StageClear); // 클리어 소리
        StartCoroutine(_stageClearUI.Show());
    }

    /// <summary>
    /// 스테이지 클리어
    /// </summary>
    void HandleStageClear()
    {
        _stageClearUI.Hide();

        // 마지막 스테이지면 상점 없이 게임 클리어 UI 표시
        // HACK:게임 클리어에서 이어하기 하면 상점 나오게 할거임
        if (_currentStage.StageID == GameManager.Instance.DataManager.LastStageID)
        {
            _OnCursor = true;
            _inputSystemHandler.SetInputDisabled(true);
            _player.SetPlayerGravity(false); // 씬 언로드 중 자유낙하 방지

            // 통계 띄우기
            GameStatistics stats = GameManager.Instance.GameStatistics;
            _gameClearUI.Show(
                stats.TotalGoldEarned,
                stats.ItemsUsed,
                stats.ShellKills,
                stats.DeathCount,
                _currentStage.StageID - 7100,
                stats.GetPlayTime()
            );
            return;
        }

        // 다음 스테이지 미리 로드
        if (_isTutorial == false)
        {
            _currentStageUnload = SceneManager.UnloadSceneAsync(_currentStage.SceneName);
            _nextStageLoad = SceneManager.LoadSceneAsync(_currentStage.NextStageName, LoadSceneMode.Additive);
            _nextStageLoad.allowSceneActivation = false;
        }

        // 드롭존 비활성화
        _dropZoneUI.SetActiveState(false);

        // 남아있는 포탄, 아이템 등을 모든 Pool로 강제 반환
        GameManager.Instance.PoolManager.ReturnAllPools();

        // 상점 열기
        _isShopOpen = true;
        _OnCursor = true;
        _shopUI.SetShopActive(true);
        _inventoryUI.EnterStore(_shopUI.RightPanelTr); // UI 위치 옮김
        _inputSystemHandler.SetInputDisabled(true);
        Cursor.lockState = CursorLockMode.None;

        // 멀티플레이:탈락 상태였다면 상점 진입을 계기로 마지막 목숨(0)으로 복귀
        bool isMultiplayer = NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;
        if (isMultiplayer) _player.NetworkOwner.ReviveFromEliminationAtShop();

        // 상점 열렸을 땐 재도전 막아놓음
        _pauseUI.RetryBtn.SetActive(false);

        _player.EquipViewMod(true); // 장착 모드 활성화
        _player.SetPlayerGravity(false); // 중력 설정(씬 전환시 자유낙하 방지)
    }

    /// <summary>
    /// 튜토리얼 클리어 연출
    /// </summary>
    public void ShowTutorialClearEffect()
    {
        HandleAllEnemiesDefeated();
    }

    /// <summary>
    /// 튜토리얼 클리어 처리
    /// </summary>
    public void TutorialClear()
    {
        _isTutorial = true;

        // 인벤토리 비우기 + 골드 지급
        _inventoryUI.Presenter.Clear();
        GameManager.Instance.PlayerData.AddGold(3000);

        HandleStageClear();
    }

    /// <summary>
    /// 상점 나가기(다음 스테이지)
    /// </summary>
    void HandleShopExit()
    {
        _pauseUI.RetryBtn.SetActive(true);
        _player.EquipViewMod(false); // 장착 모드 비활성화

        // 튜토리얼이면 타이틀로
        if (_isTutorial)
        {
            HandleTitleRequested();
            return;
        }

        StartCoroutine(LoadStageRoutine(_currentStage.NextStageName));
    }

    /// <summary>
    /// 스테이지 재시작
    /// </summary>
    void HandleRestartStage()
    {
        _isRestarting = true;

        if (_isShopOpen)
        {
            Debug.Log("상점 열렸을 때는 재시작 막아놓음"); // 실제로 눌릴 일은 없음
        }
        else
        {
            // 멀티플레이:호스트만 전원에게 재도전 신호를 전파(비호스트는 신호를 받아 이 함수로 들어온 것이므로 재전파하면 안 됨)
            bool isMultiplayer = (NetworkManager.Singleton != null) && NetworkManager.Singleton.IsListening;
            if (isMultiplayer && NetworkManager.Singleton.IsServer)
            {
                NetworkGameManager.Instance.NotifyRestart();
            }

            // 남아있는 포탄, 아이템 등을 모든 Pool로 강제 반환
            GameManager.Instance.PoolManager.ReturnAllPools();

            StartCoroutine(LoadStageRoutine(_currentStage.SceneName));
        }
    }

    IEnumerator LoadStageRoutine(string sceneName)
    {
        // 맵 관련 초기화
        _player.ResetMud();
        _player.ResetBush();

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
            _dropZoneUI.SetActiveState(true); // 드롭존 활성화

            _isShopOpen = false;
            _shopUI.SetShopActive(false);
            _inventoryUI.ExitStore(); // UI 위치 복귀
        }

        // HUD 연출 초기화(로딩 화면이 뜨기 전에 미리 화면 밖으로 이동시켜, 유저에게 순간이동이 노출되지 않게 함)
        _hasPlayedHudIntro = false;
        if (_hudIntroDirector != null) _hudIntroDirector.PrepareOffscreen();

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

        // 시네머신 블렌드 방식을 다시 Cut으로 복구
        _cinemachineBrain.DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.Cut, 0f);

        yield return new WaitForSeconds(0.1f); // 잠깐 기다리기
        loadingUI.Hide();
    }

    /// <summary>
    /// 메인화면으로 가기
    /// </summary>
    void HandleTitleRequested()
    {
        // 남아있는 포탄, 아이템 등을 모든 Pool로 강제 반환
        GameManager.Instance.PoolManager.ReturnAllPools();

        Time.timeScale = 1f;
        UnsubscribeStage();

        // 멀티플레이 로비 정리(호스트는 로비 삭제, 클라이언트는 본인만 나감) — 아래 Shutdown보다 반드시 먼저 호출해야 함
        // LeaveLobbyAsync가 내부에서 가장 먼저 LobbyManager 자체의 OnClientDisconnectCallback 구독을 해제하는데,
        // 이걸 먼저 안 하면 바로 아래 Shutdown()으로 인한 자기 자신의 연결 종료가 "방장이 나감"으로 오인식되어
        // HandleHostLeft가 불필요하게 다시 호출됨(싱글플레이는 LobbyManager.Instance가 없거나 로비가 없어 안전하게 무시됨)
        if (LobbyManager.Instance != null)
        {
            _ = LobbyManager.Instance.LeaveLobbyAsync();
        }

        // 멀티플레이 매치 중(또는 종료 후) 메인메뉴로 돌아갈 때 네트워크 세션이 안 끊긴 채 남으면
        // 다음 멀티플레이 시도 때 좀비 연결 상태 위에서 새 세션이 시작돼버림(LobbyScene.ShutdownNetwork()와 동일한 패턴)
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            NetworkManager.Singleton.Shutdown();
        }

        SceneManager.LoadScene("Title");
    }

    /// <summary>
    /// 무적 아이템 사용
    /// </summary>
    IEnumerator HyperShieldRoutine(float duration)
    {
        _player.Model.SetNoDamage(true);
        _player.SetNetworkInvincible(true); // 멀티:서버의 데미지 판정에도 반영
        _player.ActivateShieldVisual(duration); // 본인 화면 로컬 재생 + 멀티면 다른 클라이언트에도 전파

        yield return new WaitForSeconds(duration);

        _player.Model.SetNoDamage(false);
        _player.SetNetworkInvincible(false);
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