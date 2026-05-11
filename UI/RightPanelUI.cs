using System.Collections;
using UnityEngine;

/// <summary>
/// 오른쪽 패널 UI
/// </summary>
public class RightPanelUI : MonoBehaviour
{
    [SerializeField] float _slideDuration = 0.3f;
    [SerializeField] RectTransform _panel;

    float _hiddenX;  // 숨겨진 위치 (오른쪽 밖)
    float _shownX;   // 보여지는 위치
    bool _isHidden = false;
    Coroutine _slideRoutine;

    public bool IsOpen => !_isHidden;

    void Awake()
    {
        _shownX = _panel.anchoredPosition.x;
        _hiddenX = _shownX + _panel.rect.width; // 패널 너비만큼 오른쪽으로
    }

    /// <summary>
    /// UI 표시 토글
    /// </summary>
    public void Toggle()
    {
        _isHidden = !_isHidden;
        if (_slideRoutine != null) StopCoroutine(_slideRoutine);
        _slideRoutine = StartCoroutine(SlideRoutine(_isHidden ? _hiddenX : _shownX));
    }

    /// <summary>
    /// UI 슬라이드
    /// </summary>
    IEnumerator SlideRoutine(float targetX)
    {
        float startX = _panel.anchoredPosition.x;
        float elapsed = 0f;

        while (elapsed < _slideDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / _slideDuration;
            progress = Mathf.SmoothStep(0f, 1f, progress);
            _panel.anchoredPosition = new Vector2(Mathf.Lerp(startX, targetX, progress), _panel.anchoredPosition.y);
            yield return null;
        }

        _panel.anchoredPosition = new Vector2(targetX, _panel.anchoredPosition.y);
    }
}