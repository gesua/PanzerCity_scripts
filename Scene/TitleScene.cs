using System.Collections;
using UnityEngine;
using UnityEngine.Localization.Components;
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

    AsyncOperation _gameSceneLoad; // Game씬 동기화용
    bool _isStart;

    IEnumerator Start()
    {
        // 로컬라이제이션 초기화 대기
        yield return LocalizationSettings.InitializationOperation;

        // 저장된 언어 설정 적용
        string language = PlayerPrefs.GetString("Language", "");

        // 저장된 언어 없으면 시스템 언어 가져옴
        if (string.IsNullOrEmpty(language))
        {
            language = (Application.systemLanguage == SystemLanguage.Korean) ? "ko" : "en";
        }

        foreach (var locale in LocalizationSettings.AvailableLocales.Locales)
        {
            if (locale.Identifier.Code == language)
            {
                LocalizationSettings.SelectedLocale = locale;
                break;
            }
        }

        // 언어 변경 구독
        GameManager.Instance.OptionManager.OnLanguageChanged += OnLanguageChanged;

        // 게임 씬 미리 로드
        _gameSceneLoad = SceneManager.LoadSceneAsync("Game", LoadSceneMode.Additive);
        _gameSceneLoad.allowSceneActivation = false; // 활성화 보류
    }

    /// <summary>
    /// 게임시작 버튼
    /// </summary>
    public void OnClickStart()
    {
        if (_isStart) return;
        _isStart = true;

        StartCoroutine(StartRoutine());
    }

    IEnumerator StartRoutine()
    {
        // Pool 미리 만들기
        GameManager.Instance.PoolManager.GetPool("DroppedItem");
        GameManager.Instance.PoolManager.GetPool("Shell");

        LoadingUI loadingUI = GameManager.Instance.LoadingUI;

        loadingUI.Show();

        // Stage01 로드 대기
        AsyncOperation stageLoad = SceneManager.LoadSceneAsync("Stage01", LoadSceneMode.Additive);
        stageLoad.allowSceneActivation = false;

        // 로드 완료까지 진행도 업데이트
        StartCoroutine(loadingUI.UpdateProgress(_gameSceneLoad, stageLoad)); // 로딩UI만 따로 움직임
        yield return new WaitUntil(() => _gameSceneLoad.progress >= 0.9f && stageLoad.progress >= 0.9f); // 실제로 기다리는 거

        // 씬 전환될 때 그대로 두면 2개라고 에러 뜸
        _audioListener.enabled = false;
        _eventSystem.gameObject.SetActive(false);

        // 동시에 활성화
        _gameSceneLoad.allowSceneActivation = true;
        stageLoad.allowSceneActivation = true;
        yield return stageLoad;

        SceneManager.UnloadSceneAsync("Title");
        yield return new WaitForSeconds(0.1f); // 잠깐 기다리기

        loadingUI.Hide();
    }

    public void OnClickOptions()
    {
        _option.SetActive(true);
    }

    /// <summary>
    /// 언어 변경시 대기
    /// </summary>
    void OnLanguageChanged()
    {
        StartCoroutine(RefreshLocalization());
    }

    /// <summary>
    /// Font Asset이 늦게 바뀌니까 대기
    /// </summary>
    IEnumerator RefreshLocalization()
    {
        // Font Asset 비동기 로드 완료 대기
        yield return LocalizationSettings.InitializationOperation;
        yield return null; // 한 프레임 더 대기

        foreach (var localizeEvent in FindObjectsByType<LocalizeStringEvent>(FindObjectsSortMode.None))
        {
            localizeEvent.RefreshString();
        }
    }

    /// <summary>
    /// 구독 해제
    /// </summary>
    void OnDestroy()
    {
        GameManager.Instance.OptionManager.OnLanguageChanged -= OnLanguageChanged;
    }
}
