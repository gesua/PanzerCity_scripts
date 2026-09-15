using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.SceneManagement;

/// <summary>
/// 첫 시작화면 관리
/// </summary>
public class TitleScene : MonoBehaviour
{
    [SerializeField] AudioListener _audioListener;
    [SerializeField] UnityEngine.EventSystems.EventSystem _eventSystem;
    [SerializeField] GameObject _option;
    [SerializeField] GameObject _credits;
    [SerializeField] SaveSlotUI _saveSlotUI;
    [SerializeField] TitleButtonSparkleManager _sparkleManager; // 세이브 슬롯 UI 표시 중 타이틀 버튼 반짝임을 제어하기 위한 참조

    [Header("----- 로고 애니메이션 -----")]
    [SerializeField] RectTransform _titleLogo;
    [SerializeField] float _logoAnimationDuration = 0.4f; // 로고가 내려오는 시간
    [SerializeField] float _logoStartYOffset = 340f;      // 시작 시 위로 올라가 있을 위치
    [SerializeField] float _logoMoveAmount = 8f;          // 대기 중 좌우 이동량
    [SerializeField] float _logoMoveSpeed = 0.8f;         // 대기 중 좌우 이동 속도

    [Header("----- 버튼 애니메이션 -----")]
    [SerializeField] CanvasGroup _titleButtonGroup;
    [SerializeField] float _animationDuration = 0.8f; // 올라오는데 걸리는 시간
    [SerializeField] float _startYOffset = -50f;      // 시작 시 아래로 내려가 있을 위치 값

    [Header("----- 방장 이탈 알림 팝업 -----")]
    [SerializeField] GameObject _hostLeftPopup;          // 패널 전체 켜고 끄기용(평소엔 비활성)
    [SerializeField] TextMeshProUGUI _hostLeftPopupText; // 메시지 표시

    Vector2 _logoOriginalPosition;

    AsyncOperation _gameSceneLoad; // Game씬 동기화용

    IEnumerator Start()
    {
        _saveSlotUI.OnSlotSelected += HandleSlotSelected;

        // 로고 초기 상태 세팅
        InitializeLogoState();

        // 로컬라이징 대기 전에 버튼을 투명하게 하고 아래로 내림
        InitializeButtonState();

        // 로컬라이제이션 초기화 대기
        yield return LocalizationSettings.InitializationOperation;

        GameManager manager = GameManager.Instance;
        manager.GameStatistics.ResetAll(); // 통계 초기화
        manager.AudioManager.PlayBgm(BgmType.Title);

        // 멀티플레이 중 방장이 나가서 여기로 왔다면 팝업으로 안내(로컬라이징 초기화 이후라 GetLocalizedString 사용 가능)
        if (LobbyManager.Instance != null && LobbyManager.Instance.ConsumeHostLeftNotification())
        {
            ShowHostLeftPopup();
        }

        // 초기화가 끝나면 로고 등장 애니메이션 실행
        StartCoroutine(ShowLogoRoutine());

        // 초기화가 끝나면 버튼이 위로 올라오면서 나타나는 애니메이션 실행
        StartCoroutine(ShowButtonRoutine());
    }

    /// <summary>
    /// 방장 이탈 알림 팝업 표시
    /// </summary>
    void ShowHostLeftPopup()
    {
        _hostLeftPopupText.text = LocalizationSettings.StringDatabase.GetLocalizedString("Localization", "UI_MP_MSG_HOST_LEFT");
        _hostLeftPopup.SetActive(true);
    }

    /// <summary>
    /// 방장 이탈 알림 팝업 닫기 버튼
    /// </summary>
    public void OnClickCloseHostLeftPopup()
    {
        _hostLeftPopup.SetActive(false);
    }

    /// <summary>
    /// 로고 초기 상태 세팅
    /// </summary>
    void InitializeLogoState()
    {
        if (_titleLogo == null) return;

        _logoOriginalPosition = _titleLogo.anchoredPosition;

        // 로고를 원래 위치보다 위에 배치
        _titleLogo.anchoredPosition = _logoOriginalPosition + new Vector2(0, _logoStartYOffset);
    }

    /// <summary>
    /// 로고가 화면 위에서 내려오며 바닥에 강하게 충돌한 후 튕기는 연출
    /// </summary>
    IEnumerator ShowLogoRoutine()
    {
        if (_titleLogo == null) yield break;

        float elapsedTime = 0f;
        Vector2 startPosition = _titleLogo.anchoredPosition;
        Vector2 targetPosition = _logoOriginalPosition;

        while (elapsedTime < _logoAnimationDuration)
        {
            elapsedTime += Time.deltaTime;

            float t = Mathf.Clamp01(elapsedTime / _logoAnimationDuration);

            // 감속 없이 일정한 속도로 내려오도록 Linear 적용
            _titleLogo.anchoredPosition = Vector2.Lerp(
                startPosition,
                targetPosition,
                t
            );

            yield return null;
        }

        // 정확히 목표 위치에 도착
        _titleLogo.anchoredPosition = targetPosition;

        // 바닥에 부딪힌 후 강하게 튕기는 연출
        yield return StartCoroutine(LogoBounceRoutine());

        // 착지 후 잠시 정지
        yield return new WaitForSeconds(0.4f);

        // 등장 애니메이션이 끝나면 대기 연출 시작
        StartCoroutine(LogoIdleRoutine());
    }

    /// <summary>
    /// 로고가 착지한 후 위아래로 튕기는 연출
    /// </summary>
    IEnumerator LogoBounceRoutine()
    {
        if (_titleLogo == null) yield break;

        Vector2 targetPosition = _logoOriginalPosition;

        // 첫 번째 강한 튕김
        yield return MoveLogoBounce(targetPosition, 25f, 0.12f);

        // 두 번째 작은 튕김
        yield return MoveLogoBounce(targetPosition, 7f, 0.09f);

        // 최종 위치 보정
        _titleLogo.anchoredPosition = targetPosition;
    }

    /// <summary>
    /// 로고를 위로 튕겼다가 원래 위치로 돌아오게 함
    /// </summary>
    IEnumerator MoveLogoBounce(Vector2 targetPosition, float amount, float duration)
    {
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;

            float t = Mathf.Clamp01(elapsedTime / duration);

            // 빠르게 올라갔다가 부드럽게 내려옴
            float curve = Mathf.Sin(t * Mathf.PI);

            _titleLogo.anchoredPosition =
                targetPosition + Vector2.up * amount * curve;

            yield return null;
        }

        _titleLogo.anchoredPosition = targetPosition;
    }

    /// <summary>
    /// 로고 대기 중 미세한 좌우 움직임
    /// </summary>
    IEnumerator LogoIdleRoutine()
    {
        if (_titleLogo == null) yield break;

        float startTime = Time.time;

        while (true)
        {
            // 대기 연출 시작 시점을 기준으로 0부터 시작
            float time = Time.time - startTime;

            // 중앙에서 시작해서 좌우로 천천히 움직임
            float moveX = Mathf.Sin(time * _logoMoveSpeed) * _logoMoveAmount;

            _titleLogo.anchoredPosition =
                _logoOriginalPosition + new Vector2(moveX, 0f);

            yield return null;
        }
    }

    /// <summary>
    /// 버튼 초기 상태 세팅 (투명, 상호작용 불가, 아래로 이동)
    /// </summary>
    void InitializeButtonState()
    {
        if (_titleButtonGroup == null) return;

        _titleButtonGroup.alpha = 0f; // 투명하게
        _titleButtonGroup.interactable = false; // 애니메이션 도중 클릭 방지
        _titleButtonGroup.blocksRaycasts = false;

        // Y축으로 _startYOffset 만큼 아래로 이동
        if (_titleButtonGroup.TryGetComponent<RectTransform>(out RectTransform rt) == false) return;
        rt.anchoredPosition += new Vector2(0, _startYOffset);
    }

    /// <summary>
    /// 버튼이 페이드인 되며 위로 올라오는 애니메이션 코루틴
    /// </summary>
    IEnumerator ShowButtonRoutine()
    {
        if (_titleButtonGroup == null) yield break;
        if (_titleButtonGroup.TryGetComponent<RectTransform>(out RectTransform rt) == false) yield break;

        float elapsedTime = 0f;

        // 버튼의 RectTransform과 시작/목표 위치 캐싱
        Vector2 startPosition = rt.anchoredPosition;
        // 원래 위치(목표 위치)는 시작 위치에서 아까 내렸던 만큼 다시 올린 위치
        Vector2 targetPosition = startPosition - new Vector2(0, _startYOffset);

        // 애니메이션 루프
        while (elapsedTime < _animationDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / _animationDuration);

            // 부드러운 감속 (Ease-Out Cubic) 효과 적용 - 점점 느려지면서 도착
            float curve = 1f - Mathf.Pow(1f - t, 3f);

            // 알파값 페이드인
            _titleButtonGroup.alpha = curve;

            // 위치 이동
            rt.anchoredPosition = Vector2.Lerp(startPosition, targetPosition, curve);

            yield return null;
        }

        // 애니메이션 종료 후 확실하게 최종값 셋팅 및 클릭 활성화
        _titleButtonGroup.alpha = 1f;
        _titleButtonGroup.interactable = true;
        _titleButtonGroup.blocksRaycasts = true;
        rt.anchoredPosition = targetPosition;
    }

    /// <summary>
    /// 게임시작 버튼 — 세이브 슬롯 선택 UI 표시 (표시되는 동안 타이틀 버튼 반짝임 정지)
    /// </summary>
    public void OnClickStart()
    {
        _saveSlotUI.Show();
        _sparkleManager.PauseAll(); // 세이브 슬롯 UI가 열리는 동안 타이틀 버튼 반짝임 정지
    }

    /// <summary>
    /// 세이브 슬롯 선택됨 — 빈 슬롯이면 새 게임, 저장된 슬롯이면 이어하기
    /// </summary>
    void HandleSlotSelected(int slotIndex)
    {
        _saveSlotUI.Hide();

        SaveManager saveManager = GameManager.Instance.SaveManager;
        SaveData saveData = saveManager.GetSlotData(slotIndex);

        saveManager.ContinueGame(slotIndex);
        string stageName = GameManager.Instance.DataManager.StageIDToSceneName(saveData.CurrentStageID);

        StartCoroutine(StartRoutine(stageName));
    }

    /// <summary>
    /// 튜토리얼 버튼
    /// </summary>
    public void OnClickTutorial()
    {
        // 튜토리얼 모드
        GameManager.Instance.SaveManager.SetTutorialMode();

        StartCoroutine(StartRoutine("Stage00"));
    }

    /// <summary>
    /// 멀티플레이 버튼
    /// </summary>
    public void OnClickMultiplayer()
    {
        // Lobby 씬의 오브젝트들이 Awake를 마친 직후(=Lobby 리스너가 켜진 직후) 호출됨
        SceneManager.sceneLoaded += OnLobbySceneLoaded;

        StartCoroutine(LoadLobbyRoutine());
    }

    IEnumerator StartRoutine(string stageName)
    {
        // Pool 미리 만들기
        GameManager.Instance.PoolManager.GetPool("DroppedItem");
        GameManager.Instance.PoolManager.GetPool("Shell");

        LoadingUI loadingUI = GameManager.Instance.LoadingUI;

        loadingUI.Show();

        // Game 씬의 오브젝트들이 Awake를 마친 직후(=메인카메라 리스너가 켜진 직후) 호출됨
        // 프레임 대기 없이 그 시점에 곧바로 Title 리스너를 꺼서 0개/2개 상태가 노출되지 않도록 함
        SceneManager.sceneLoaded += OnGameSceneLoaded;

        // 스테이지 로드
        _eventSystem.gameObject.SetActive(false);

        _gameSceneLoad = SceneManager.LoadSceneAsync("Game", LoadSceneMode.Additive);
        yield return _gameSceneLoad;

        AsyncOperation stageLoad = SceneManager.LoadSceneAsync(stageName, LoadSceneMode.Additive);
        stageLoad.allowSceneActivation = false;

        // 로드 완료까지 진행도 업데이트
        StartCoroutine(loadingUI.UpdateProgress(stageLoad)); // 로딩UI만 따로 움직임
        yield return new WaitUntil(() => stageLoad.progress >= 0.9f); // 실제로 기다리는 거

        // 씬 전환될 때 그대로 두면 2개라고 에러 뜸
        _eventSystem.gameObject.SetActive(false);

        // 동시에 활성화
        _gameSceneLoad.allowSceneActivation = true;
        stageLoad.allowSceneActivation = true;
        yield return stageLoad;

        yield return new WaitForSeconds(0.1f); // 잠깐 기다리기

        SceneManager.UnloadSceneAsync("Title");
    }

    /// <summary>
    /// Game 씬 로드 완료 콜백 — 메인카메라 리스너가 켜진 직후 Title 리스너를 꺼서
    /// 리스너 0개/2개 상태가 어떤 프레임에도 노출되지 않도록 함. 1회성이라 즉시 구독 해제
    /// </summary>
    void OnGameSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != "Game") return;

        _audioListener.enabled = false;
        SceneManager.sceneLoaded -= OnGameSceneLoaded;
    }

    /// <summary>
    /// Lobby 씬 로드 완료 콜백 — Lobby 리스너가 켜진 직후 Title 리스너를 꺼서
    /// 리스너 0개/2개 상태가 어떤 프레임에도 노출되지 않도록 함. 1회성이라 즉시 구독 해제
    /// </summary>
    void OnLobbySceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != "Lobby") return;

        _audioListener.enabled = false;
        SceneManager.sceneLoaded -= OnLobbySceneLoaded;
    }

    IEnumerator LoadLobbyRoutine()
    {
        _eventSystem.gameObject.SetActive(false);

        yield return SceneManager.LoadSceneAsync("Lobby");
    }

    public void OnClickOptions()
    {
        _option.SetActive(true);
    }

    public void OpenCredits()
    {
        _credits.SetActive(true);
        _sparkleManager.PauseAll(); // 타이틀 버튼 반짝임 정지
    }

    public void CloseCredits()
    {
        _credits.SetActive(false);
        _sparkleManager.ResumeAll(); // 반짝임 재개
    }

    /// <summary>
    /// 세이브 슬롯 선택 화면 닫기 버튼 — 선택 없이 취소 (타이틀 버튼 반짝임 재개)
    /// </summary>
    public void OnClickCloseSaveSlot()
    {
        _saveSlotUI.Hide();
        _sparkleManager.ResumeAll(); // 세이브 슬롯 UI가 닫히면 반짝임 재개
    }

    /// <summary>
    /// 게임 종료 버튼
    /// </summary>
    public void OnClickQuit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    /// <summary>
    /// 구독 해제
    /// </summary>
    void OnDestroy()
    {
        _saveSlotUI.OnSlotSelected -= HandleSlotSelected;
        SceneManager.sceneLoaded -= OnGameSceneLoaded;  // 콜백 실행 전 파괴되는 경우를 대비한 안전장치
        SceneManager.sceneLoaded -= OnLobbySceneLoaded; // 콜백 실행 전 파괴되는 경우를 대비한 안전장치
        GameManager.Instance.LoadingUI.Hide();
    }
}