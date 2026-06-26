using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;

/// <summary>
/// 튜토리얼 정보 메시지 UI
/// 진행도와 무관하게 메시지를 순환 표시
/// </summary>
public class TutorialInfoUI : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI _messageText;
    [SerializeField] string[] _messageKeys; // 순환할 메시지 키 목록
    [SerializeField] float _interval = 5f;  // 메시지 전환 간격

    Coroutine _routine;
    int _currentIndex = 0;

    /// <summary>
    /// 메시지 순환 시작
    /// </summary>
    public void Play()
    {
        if (_messageKeys.Length == 0) return;

        gameObject.SetActive(true);
        _currentIndex = 0;
        _routine = StartCoroutine(CycleRoutine());
    }

    /// <summary>
    /// 메시지 순환 중단
    /// </summary>
    public void Stop()
    {
        if (_routine != null)
        {
            StopCoroutine(_routine);
            _routine = null;
        }

        gameObject.SetActive(false);
    }

    /// <summary>
    /// 메시지 순환 코루틴
    /// </summary>
    IEnumerator CycleRoutine()
    {
        while (true)
        {
            ShowMessage(_messageKeys[_currentIndex]);
            yield return new WaitForSeconds(_interval);
            _currentIndex = (_currentIndex + 1) % _messageKeys.Length;
        }
    }

    /// <summary>
    /// 메시지 표시
    /// </summary>
    void ShowMessage(string key)
    {
        LocalizedString localizedString = new LocalizedString("Localization", key);
        _messageText.text = localizedString.GetLocalizedString();
    }
}
