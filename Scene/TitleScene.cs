using System.Collections;
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
    [SerializeField] SaveSlotUI _saveSlotUI;

    [Header("----- 버튼 애니메이션 -----")]
    [SerializeField] CanvasGroup[] _titleButtons;
    [SerializeField] float _animationDuration = 0.8f; // 올라오는데 걸리는 시간
    [SerializeField] float _startYOffset = -50f;      // 시작 시 아래로 내려가 있을 위치 값

    AsyncOperation _gameSceneLoad; // Game씬 동기화용

    IEnumerator Start()
    {
        _saveSlotUI.OnSlotSelected += HandleSlotSelected;

        // 로컬라이징 대기 전에 버튼들을 투명하게 하고 아래로 내림
        InitializeButtonsState();

        // 로컬라이제이션 초기화 대기
        yield return LocalizationSettings.InitializationOperation;

        GameManager manager = GameManager.Instance;

        // 초기화가 끝나면 버튼들이 위로 올라오면서 나타나는 애니메이션 실행
        StartCoroutine(ShowButtonsRoutine());
    }

    /// <summary>
    /// 버튼 초기 상태 세팅 (투명, 상호작용 불가, 아래로 이동)
    /// </summary>
    void InitializeButtonsState()
    {
        foreach (var cg in _titleButtons)
        {
            if (cg == null) continue;

            cg.alpha = 0f; // 투명하게
            cg.interactable = false; // 애니메이션 도중 클릭 방지
            cg.blocksRaycasts = false;

            // Y축으로 _startYOffset 만큼 아래로 이동
            RectTransform rt = cg.GetComponent<RectTransform>();
            rt.anchoredPosition += new Vector2(0, _startYOffset);
        }
    }

    /// <summary>
    /// 버튼들이 페이드인 되며 위로 올라오는 애니메이션 코루틴
    /// </summary>
    IEnumerator ShowButtonsRoutine()
    {
        float elapsedTime = 0f;

        // 각 버튼의 RectTransform과 시작/목표 위치 캐싱
        RectTransform[] rects = new RectTransform[_titleButtons.Length];
        Vector2[] startPositions = new Vector2[_titleButtons.Length];
        Vector2[] targetPositions = new Vector2[_titleButtons.Length];

        for (int i = 0; i < _titleButtons.Length; i++)
        {
            if (_titleButtons[i] == null) continue;
            rects[i] = _titleButtons[i].GetComponent<RectTransform>();
            startPositions[i] = rects[i].anchoredPosition;
            // 원래 위치(목표 위치)는 시작 위치에서 아까 내렸던 만큼 다시 올린 위치
            targetPositions[i] = startPositions[i] - new Vector2(0, _startYOffset);
        }

        // 애니메이션 루프
        while (elapsedTime < _animationDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / _animationDuration);

            // 부드러운 감속 (Ease-Out Cubic) 효과 적용 - 점점 느려지면서 도착
            float curve = 1f - Mathf.Pow(1f - t, 3f);

            for (int i = 0; i < _titleButtons.Length; i++)
            {
                if (_titleButtons[i] == null) continue;

                // 1. 알파값 페이드인
                _titleButtons[i].alpha = curve;

                // 2. 위치 이동
                rects[i].anchoredPosition = Vector2.Lerp(startPositions[i], targetPositions[i], curve);
            }
            yield return null;
        }

        // 애니메이션 종료 후 확실하게 최종값 셋팅 및 클릭 활성화
        for (int i = 0; i < _titleButtons.Length; i++)
        {
            if (_titleButtons[i] == null) continue;
            _titleButtons[i].alpha = 1f;
            _titleButtons[i].interactable = true;
            _titleButtons[i].blocksRaycasts = true;
            rects[i].anchoredPosition = targetPositions[i];
        }
    }

    /// <summary>
    /// 게임시작 버튼 — 세이브 슬롯 선택 UI 표시
    /// </summary>
    public void OnClickStart()
    {
        _saveSlotUI.Show();
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
        StartCoroutine(LoadLobbyRoutine());
    }

    IEnumerator StartRoutine(string stageName)
    {
        // Pool 미리 만들기
        GameManager.Instance.PoolManager.GetPool("DroppedItem");
        GameManager.Instance.PoolManager.GetPool("Shell");

        LoadingUI loadingUI = GameManager.Instance.LoadingUI;

        loadingUI.Show();

        // 스테이지 로드
        _audioListener.enabled = false;
        _eventSystem.gameObject.SetActive(false);

        _gameSceneLoad = SceneManager.LoadSceneAsync("Game", LoadSceneMode.Additive);
        yield return _gameSceneLoad;

        AsyncOperation stageLoad = SceneManager.LoadSceneAsync(stageName, LoadSceneMode.Additive);
        stageLoad.allowSceneActivation = false;

        // 로드 완료까지 진행도 업데이트
        StartCoroutine(loadingUI.UpdateProgress(stageLoad)); // 로딩UI만 따로 움직임
        yield return new WaitUntil(() => stageLoad.progress >= 0.9f); // 실제로 기다리는 거

        // 씬 전환될 때 그대로 두면 2개라고 에러 뜸
        _audioListener.enabled = false;
        _eventSystem.gameObject.SetActive(false);

        // 동시에 활성화
        _gameSceneLoad.allowSceneActivation = true;
        stageLoad.allowSceneActivation = true;
        yield return stageLoad;

        yield return new WaitForSeconds(0.1f); // 잠깐 기다리기

        SceneManager.UnloadSceneAsync("Title");
    }

    IEnumerator LoadLobbyRoutine()
    {
        _audioListener.enabled = false;
        _eventSystem.gameObject.SetActive(false);

        yield return SceneManager.LoadSceneAsync("Lobby");
    }

    public void OnClickOptions()
    {
        _option.SetActive(true);
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
        GameManager.Instance.LoadingUI.Hide();
    }
}