using System;
using UnityEngine;

public class PauseUI : MonoBehaviour
{
    [SerializeField] GameObject _option;
    [SerializeField] GameObject _retryBtn;

    public GameObject RetryBtn => _retryBtn;
    public bool IsOptionOpen => _option.activeSelf;

    public event Action OnResumeClicked;    // 전투재개
    public event Action OnRestartClicked;   // 재도전
    public event Action OnMainMenuClicked;  // 메인메뉴

    public void OnClickResume()
    {
        OnResumeClicked?.Invoke();
    }

    public void OnClickRestart()
    {
        OnRestartClicked?.Invoke();
    }

    public void OnClickOptions()
    {
        _option.SetActive(true);
    }

    public void OnClickMainMenu()
    {
        OnMainMenuClicked?.Invoke();
    }

    public void SetActive(bool active)
    {
        gameObject.SetActive(active);
    }

    public void CloseOption()
    {
        GameManager.Instance.OptionManager.OptionData.Load();
        GameManager.Instance.OptionManager.Apply();
        _option.SetActive(false);
    }
}
