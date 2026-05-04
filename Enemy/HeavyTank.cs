using UnityEngine;

/// <summary>
/// 중전차
/// Rigidbody Mass:1.0
/// </summary>
public class HeavyTank : EnemyTank
{
    [Header("----- 컴포넌트(HeavyTank) -----")]
    [SerializeField] Renderer[] _renderers; // 색상 바꿀 렌더러들
    
    // 바뀔 색
    Color[] _hpColors =
    {
        new Color(0f, 0.5f, 0f),    // 4/4 초록
        Color.yellow,               // 3/4 노랑
        new Color(1f, 0.5f, 0f),    // 2/4 주황
        Color.red                   // 1/4 빨강
    };

    protected override void Awake()
    {
        base.Awake();
        _model.OnHpChanged += HandleHpChanged;
        _model.OnHit += HandleHit;
    }

    void HandleHpChanged(int current, int max)
    {
        // current 1~4를 인덱스 3~0으로 변환
        int colorIndex = max - current;
        colorIndex = Mathf.Clamp(colorIndex, 0, _hpColors.Length - 1);

        // 색 변경
        foreach (Renderer renderer in _renderers)
        {
            renderer.material.color = _hpColors[colorIndex];
        }
    }

    void HandleHit(HitData hitData)
    {
        // 공격한 탱크를 바로 타겟으로 설정
        SetTarget(hitData.AtkTank.transform);
        ChangeState(EnemyStateType.Combat);
    }
}
