using System.Collections;
using Unity.Burst.CompilerServices;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 미니맵 UI
/// </summary>
public class MiniMapUI : MonoBehaviour
{
    [SerializeField] RectTransform _miniMap;
    [SerializeField] float _expandDuration = 0.3f;

    Vector2 _smallSize;    // 기본 크기
    Vector2 _smallPos;     // 기본 위치
    Vector2 _largeSize;    // 확대 크기
    Vector2 _largePos;     // 확대 위치

    bool _isExpanded = false;
    Coroutine _expandRoutine;

    void Awake()
    {
        // Canvas Scaler 기준 해상도 가져오기
        CanvasScaler canvasScaler = GetComponentInParent<CanvasScaler>();
        Vector2 referenceResolution = canvasScaler.referenceResolution;

        // 미니맵 세팅
        _smallSize = _miniMap.sizeDelta; // 현재 크기
        _smallPos = new Vector2(referenceResolution.x * 0.5f - _smallSize.x * 0.5f, -referenceResolution.y * 0.5f + _smallSize.y * 0.5f);

        // 확대맵 세팅
        float screenSize = Mathf.Min(referenceResolution.x, referenceResolution.y);
        _largeSize = new Vector2(screenSize, screenSize); // 해상도 최대 사이즈
        _largePos = Vector2.zero; // 화면 중앙

        // 미니맵 초기화(해상도에 따라 달라질 수 있으니)
        _miniMap.sizeDelta = _smallSize;
        _miniMap.anchoredPosition = _smallPos;
    }

    /// <summary>
    /// 미니맵 확대 토글
    /// </summary>
    public void Toggle()
    {
        _isExpanded = !_isExpanded;
        if (_expandRoutine != null) StopCoroutine(_expandRoutine);
        _expandRoutine = StartCoroutine(ExpandRoutine(_isExpanded));
    }

    /// <summary>
    /// 미니맵 확대
    /// </summary>
    IEnumerator ExpandRoutine(bool isExpand)
    {
        Vector2 startSize = _miniMap.sizeDelta;
        Vector2 startPos = _miniMap.anchoredPosition;
        Vector2 targetSize, targetPos;

        if (isExpand)
        {
            targetSize = _largeSize;
            targetPos = _largePos;
        }
        else
        {
            targetSize = _smallSize;
            targetPos = _smallPos;
        }

        float timer = 0f;
        while (timer < _expandDuration)
        {
            timer += Time.deltaTime;
            float progress = timer / _expandDuration;
            progress = Mathf.SmoothStep(0f, 1f, progress);

            _miniMap.sizeDelta = Vector2.Lerp(startSize, targetSize, progress);
            _miniMap.anchoredPosition = Vector2.Lerp(startPos, targetPos, progress);
            yield return null;
        }

        _miniMap.sizeDelta = targetSize;
        _miniMap.anchoredPosition = targetPos;
    }

    /// <summary>
    /// 미니맵 원래 크기로 복구
    /// </summary>
    public void ResetSize()
    {
        if (_isExpanded)
        {
            _isExpanded = false;
            if (_expandRoutine != null) StopCoroutine(_expandRoutine);

            // 코루틴 없이 즉시 원래 크기와 위치로 되돌림
            _miniMap.sizeDelta = _smallSize;
            _miniMap.anchoredPosition = _smallPos;
        }
    }
}
