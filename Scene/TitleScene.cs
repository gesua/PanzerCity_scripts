using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 첫 시작화면 관리
/// </summary>
public class TitleScene : MonoBehaviour
{
    [SerializeField] AudioListener _audioListener;
    [SerializeField] UnityEngine.EventSystems.EventSystem _eventSystem;
    [SerializeField] GameObject _loading; // 로딩
    [SerializeField] Image _loadingBar;

    AsyncOperation _gameSceneLoad; // Game씬 동기화용
    bool _isStart;

    void Start()
    {
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
        _loading.SetActive(true); // 로딩 표시

        // 씬 전환될 때 그대로 두면 2개라고 에러 뜸
        _audioListener.enabled = false;
        _eventSystem.gameObject.SetActive(false);

        // Game씬 로드 대기
        _gameSceneLoad.allowSceneActivation = true; // 시작 버튼 누를 때 활성화
        yield return _gameSceneLoad;

        // Stage01 로드 대기
        AsyncOperation stageLoad = SceneManager.LoadSceneAsync("Stage01", LoadSceneMode.Additive);

        // 로드 완료까지 진행도 업데이트
        while (!stageLoad.isDone)
        {
            _loadingBar.fillAmount = stageLoad.progress; // 0.9까지만 올라감(이미지로도 0.9까지가 좋음)
            yield return null;
        }

        SceneManager.UnloadSceneAsync("Title");
    }
}
