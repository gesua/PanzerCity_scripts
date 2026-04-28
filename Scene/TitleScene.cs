using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 첫 시작화면 관리
/// </summary>
public class TitleScene : MonoBehaviour
{
    [SerializeField] AudioListener _audioListener;
    [SerializeField] UnityEngine.EventSystems.EventSystem _eventSystem;

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
        // 씬 전환될 때 그대로 두면 2개라고 에러 뜸
        _audioListener.enabled = false;
        _eventSystem.gameObject.SetActive(false);

        _gameSceneLoad.allowSceneActivation = true; // 시작 버튼 누를 때 활성화
        // Game씬 로드 완료까지 대기
        yield return _gameSceneLoad;

        SceneManager.UnloadSceneAsync("Title");
        SceneManager.LoadSceneAsync("Stage01", LoadSceneMode.Additive);
    }
}
