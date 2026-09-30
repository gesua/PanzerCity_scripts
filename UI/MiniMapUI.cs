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

    // 앵커와 피벗은 항상 같은 값으로 씀 — anchoredPosition이 앵커 지점 기준 오프셋이 되므로
    // Expand로 캔버스 크기가 달라져도 구석/중앙 위치를 Unity 레이아웃이 알아서 맞춰줌
    static readonly Vector2 SmallAnchor = new Vector2(1f, 0f);     // 기본: 오른쪽 아래 구석
    static readonly Vector2 LargeAnchor = new Vector2(0.5f, 0.5f); // 확대: 화면 중앙

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
        _smallPos = Vector2.zero; // 오른쪽 아래 구석은 SmallAnchor(앵커/피벗)가 잡아주므로 오프셋 불필요

        // 확대맵 세팅
        float screenSize = Mathf.Min(referenceResolution.x, referenceResolution.y);
        _largeSize = new Vector2(screenSize, screenSize); // 해상도 최대 사이즈
        _largePos = Vector2.zero; // 화면 중앙

        // 미니맵 초기화(해상도에 따라 달라질 수 있으니)
        ApplyLayout(SmallAnchor, _smallSize, _smallPos);
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
        Vector2 startAnchor = _miniMap.anchorMin; // 앵커와 피벗이 항상 같은 값(한 점 앵커)이라 anchorMin으로 현재 값을 읽음
        Vector2 startSize = _miniMap.sizeDelta;
        Vector2 startPos = _miniMap.anchoredPosition;
        Vector2 targetAnchor, targetSize, targetPos;

        if (isExpand)
        {
            targetAnchor = LargeAnchor;
            targetSize = _largeSize;
            targetPos = _largePos;
        }
        else
        {
            targetAnchor = SmallAnchor;
            targetSize = _smallSize;
            targetPos = _smallPos;
        }

        float timer = 0f;
        while (timer < _expandDuration)
        {
            timer += Time.deltaTime;
            float progress = timer / _expandDuration;
            progress = Mathf.SmoothStep(0f, 1f, progress);

            ApplyLayout(
                Vector2.Lerp(startAnchor, targetAnchor, progress),
                Vector2.Lerp(startSize, targetSize, progress),
                Vector2.Lerp(startPos, targetPos, progress));
            yield return null;
        }

        ApplyLayout(targetAnchor, targetSize, targetPos);
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
            ApplyLayout(SmallAnchor, _smallSize, _smallPos);
        }
    }

    /// <summary>
    /// 앵커/피벗/크기/위치를 한 번에 적용
    /// 앵커와 피벗을 같은 값으로 세팅하므로 anchoredPosition은 앵커 지점 기준 오프셋이 됨
    /// </summary>
    /// <param name="anchor">앵커 및 피벗 (구석은 (1, 0), 중앙은 (0.5, 0.5))</param>
    /// <param name="size">크기 (앵커가 한 점이라 sizeDelta가 곧 실제 크기)</param>
    /// <param name="pos">앵커 지점 기준 위치</param>
    void ApplyLayout(Vector2 anchor, Vector2 size, Vector2 pos)
    {
        _miniMap.anchorMin = anchor;
        _miniMap.anchorMax = anchor;
        _miniMap.pivot = anchor;
        _miniMap.sizeDelta = size;
        _miniMap.anchoredPosition = pos;
    }
}