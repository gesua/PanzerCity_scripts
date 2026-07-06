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

    AsyncOperation _gameSceneLoad; // Game씬 동기화용

    IEnumerator Start()
    {
        _saveSlotUI.OnSlotSelected += HandleSlotSelected;

        // 로컬라이제이션 초기화 대기
        yield return LocalizationSettings.InitializationOperation;

        GameManager manager = GameManager.Instance;
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