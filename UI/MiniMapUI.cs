using System.Collections;
using UnityEngine;

/// <summary>
/// 미니맵 UI
/// </summary>
public class MiniMapUI : MonoBehaviour
{
    [SerializeField] RectTransform _miniMap;
    [SerializeField] float _expandDuration = 0.3f;

    [Header("----- 미니맵 크기/위치 -----")]
    [SerializeField] Vector2 _smallSize;    // 기본 크기
    [SerializeField] Vector2 _smallPos;     // 기본 위치
    [SerializeField] Vector2 _largeSize;    // 확대 크기
    [SerializeField] Vector2 _largePos;     // 확대 위치 (화면 중앙 = 0, 0)

    bool _isExpanded = false;
    Coroutine _expandRoutine;

    void Awake()
    {
        _smallSize = _miniMap.sizeDelta;
        _smallPos = _miniMap.anchoredPosition;
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
    /* 정해진 크기로 확대
    IEnumerator ExpandRoutine(bool isExpand)
    {
        Vector2 startSize = _miniMap.sizeDelta;
        Vector2 startPos = _miniMap.anchoredPosition;
        Vector2 targetSize = isExpand ? _largeSize : _smallSize;
        Vector2 targetPos = isExpand ? _largePos : _smallPos;

        float elapsed = 0f;
        while (elapsed < _expandDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / _expandDuration;
            t = t * t * (3f - 2f * t); // SmoothStep

            _miniMap.sizeDelta = Vector2.Lerp(startSize, targetSize, t);
            _miniMap.anchoredPosition = Vector2.Lerp(startPos, targetPos, t);
            yield return null;
        }

        _miniMap.sizeDelta = targetSize;
        _miniMap.anchoredPosition = targetPos;
    }
    //*/
    //* 해상도 최대 크기로 확대
    IEnumerator ExpandRoutine(bool isExpand)
    {
        Vector2 startSize = _miniMap.sizeDelta;
        Vector2 startPos = _miniMap.anchoredPosition;
        Vector2 startAnchorMin = _miniMap.anchorMin;
        Vector2 startAnchorMax = _miniMap.anchorMax;
        Vector2 startPivot = _miniMap.pivot;

        Vector2 targetSize, targetPos, targetAnchorMin, targetAnchorMax, targetPivot;

        if (isExpand)
        {
            // 화면 크기 기준으로 최대 크기 계산
            float screenSize = Mathf.Min(Screen.width, Screen.height);
            targetSize = new Vector2(screenSize, screenSize);
            targetPos = Vector2.zero;
            targetAnchorMin = new Vector2(0.5f, 0.5f);
            targetAnchorMax = new Vector2(0.5f, 0.5f);
            targetPivot = new Vector2(0.5f, 0.5f);
        }
        else
        {
            targetSize = _smallSize;
            targetPos = _smallPos;
            targetAnchorMin = new Vector2(1f, 0f);
            targetAnchorMax = new Vector2(1f, 0f);
            targetPivot = new Vector2(1f, 0f);
        }

        // Anchor, Pivot 즉시 전환 후 위치 보정
        _miniMap.anchorMin = isExpand ? targetAnchorMin : startAnchorMin;
        _miniMap.anchorMax = isExpand ? targetAnchorMax : startAnchorMax;
        _miniMap.pivot = isExpand ? targetPivot : startPivot;

        float elapsed = 0f;
        while (elapsed < _expandDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / _expandDuration;
            t = t * t * (3f - 2f * t);

            _miniMap.sizeDelta = Vector2.Lerp(startSize, targetSize, t);
            _miniMap.anchoredPosition = Vector2.Lerp(startPos, targetPos, t);
            yield return null;
        }

        _miniMap.sizeDelta = targetSize;
        _miniMap.anchoredPosition = targetPos;

        // 축소 완료 후 Anchor, Pivot 복구
        if (!isExpand)
        {
            _miniMap.anchorMin = targetAnchorMin;
            _miniMap.anchorMax = targetAnchorMax;
            _miniMap.pivot = targetPivot;
        }
    }
    //*/
}
