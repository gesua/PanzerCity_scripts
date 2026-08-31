using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// 게임 HUD, 상점 UI 등 화면 요소 전반이 화면 바깥에서 안으로 슬라이드인하는 연출 전담 범용 컴포넌트
/// 각 UI 스크립트(PlayerHPUI, TankDirectionUI, QuickSlotUI, RightPanelUI, MiniMapUI, ShopUI 등)의 내부 로직에는 관여하지 않고,
/// RectTransform의 anchoredPosition만 이동시킴
/// 자동으로 화면 밖 위치를 잡지 않으므로(Start()에서 PrepareOffscreen을 호출하지 않음), 사용하는 쪽에서
/// PrepareOffscreen() → (필요한 초기화) → PlayIntro() 순서로 명시적으로 호출해야 함
/// </summary>
public class SlideInDirector : MonoBehaviour
{
    /// <summary>
    /// 슬라이드가 시작되는 화면 밖 방향(도착 지점 기준 상대 방향)
    /// </summary>
    public enum SlideDirection
    {
        Left,
        Right,
        Bottom,
        BottomLeft,
        BottomRight
    }

    [Serializable]
    public class SlideEntry
    {
        public RectTransform Target;
        public SlideDirection Direction;
    }

    [SerializeField] float _slideDuration = 0.4f;
    [SerializeField] SlideEntry[] _entries;

    Vector2[] _shownPositions; // 각 엔트리의 원래(보여지는) 위치 캐시. 최초 1회만 캐싱됨(아래 PrepareOffscreen 참고)
    Vector2[] _startPositions; // 각 엔트리의 화면 밖 시작 위치. PrepareOffscreen에서 채워지고 PlayIntro에서 사용됨
    Coroutine _introRoutine;

    /// <summary>
    /// 슬라이드인 1회 소요 시간. 외부에서 "슬라이드인이 끝나는 시점"을 기준으로
    /// 후속 연출(예: 상점 주인 인사)을 지연시켜야 할 때 참조용으로 사용
    /// </summary>
    public float SlideDuration => _slideDuration;

    /// <summary>
    /// HUD를 화면 밖 대기 위치로 순간 이동시킴. 슬라이드인은 하지 않음
    /// 로딩 UI가 화면을 가리고 있는 동안(Game씬 최초 로드 시, 또는 스테이지 전환 로딩 시작 시) 호출해야
    /// "이미 보이던 UI가 갑자기 사라지는" 현상 없이 처리됨
    /// </summary>
    public void PrepareOffscreen()
    {
        if (_entries == null || _entries.Length == 0) return;

        // 원래 위치는 최초 호출 시에만 캐싱함
        // (재호출 시 현재 위치를 다시 캐싱하면, 예를 들어 RightPanelUI가 플레이어 조작으로 닫혀있는 상태에서
        // 스테이지 전환이 일어났을 때 그 닫힌 위치를 "보여지는 위치"로 잘못 캐싱하게 됨)
        if (_shownPositions == null)
        {
            _shownPositions = new Vector2[_entries.Length];
            for (int i = 0; i < _entries.Length; i++)
            {
                RectTransform target = _entries[i].Target;
                if (target == null) continue;

                _shownPositions[i] = target.anchoredPosition;
            }
        }

        // 화면 밖 시작 위치로 순간 이동
        _startPositions = new Vector2[_entries.Length];
        for (int i = 0; i < _entries.Length; i++)
        {
            RectTransform target = _entries[i].Target;
            if (target == null) continue;

            Vector2 offset = GetOffscreenOffset(target, _entries[i].Direction);
            _startPositions[i] = _shownPositions[i] + offset;
            target.anchoredPosition = _startPositions[i];
        }
    }

    /// <summary>
    /// 화면 밖에 대기 중인 HUD를 슬라이드인시킴. PrepareOffscreen이 먼저 호출되어 있어야 함
    /// 게임 최초 시작, 스테이지 전환, 재도전 시 리스폰 완료 시점에 호출됨
    /// 이미 재생 중이면 중단하고 새로 시작(재호출 대비, RightPanelUI.Toggle과 동일한 가드 패턴)
    /// </summary>
    public void PlayIntro()
    {
        if (_entries == null || _entries.Length == 0) return;
        if (_startPositions == null) return; // PrepareOffscreen 미호출 상태에선 재생 안 함

        if (_introRoutine != null) StopCoroutine(_introRoutine);
        _introRoutine = StartCoroutine(IntroRoutine());
    }

    IEnumerator IntroRoutine()
    {
        // 전체 엔트리 동시 슬라이드인(공유 타이머)
        float elapsed = 0f;
        while (elapsed < _slideDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / _slideDuration;
            progress = Mathf.SmoothStep(0f, 1f, progress);

            for (int i = 0; i < _entries.Length; i++)
            {
                RectTransform target = _entries[i].Target;
                if (target == null) continue;

                target.anchoredPosition = Vector2.Lerp(_startPositions[i], _shownPositions[i], progress);
            }

            yield return null;
        }

        // 최종 위치 보정(프레임 오차 방지)
        for (int i = 0; i < _entries.Length; i++)
        {
            RectTransform target = _entries[i].Target;
            if (target == null) continue;

            target.anchoredPosition = _shownPositions[i];
        }
    }

    /// <summary>
    /// 방향에 따라 화면 밖으로 벗어나는 오프셋 계산
    /// 자기 자신의 너비/높이만큼 해당 방향으로 이동(RightPanelUI._hiddenX 계산 방식과 동일한 패턴)
    /// </summary>
    Vector2 GetOffscreenOffset(RectTransform target, SlideDirection direction)
    {
        float width = target.rect.width;
        float height = target.rect.height;

        switch (direction)
        {
            case SlideDirection.Left:
                return new Vector2(-width, 0f);
            case SlideDirection.Right:
                return new Vector2(width, 0f);
            case SlideDirection.Bottom:
                return new Vector2(0f, -height);
            case SlideDirection.BottomLeft:
                return new Vector2(-width, -height);
            case SlideDirection.BottomRight:
                return new Vector2(width, -height);
            default:
                return Vector2.zero;
        }
    }
}