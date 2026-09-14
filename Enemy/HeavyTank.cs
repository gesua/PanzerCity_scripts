using UnityEngine;

/// <summary>
/// 중전차
/// Rigidbody Mass:1.0
/// </summary>
public class HeavyTank : EnemyTank
{
    [Header("----- 컴포넌트(HeavyTank) -----")]
    [SerializeField] Renderer[] _renderers; // 색상 바꿀 렌더러들

    public override bool FleesBackward => true;

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

    public override void TakeHit(ref HitData hitData)
    {
        switch (hitData.ZoneType)
        {
            case HitZoneType.None:
            case HitZoneType.Front:
                break;
            case HitZoneType.Side:
                hitData.AddDamage(1);
                break;
            case HitZoneType.Rear:
                hitData.AddDamage(2);
                break;
        }

        base.TakeHit(ref hitData);
    }

    public override void AgentMove()
    {
        if (Personality == EnemyPersonality.Coward)
        {
            FleeBackward();
            return;
        }

        base.AgentMove(); // 그 외 성격은 기존 방식 그대로
    }

    /// <summary>
    /// 후진 도주: 경로 반대 방향을 정면으로 삼아 회전한 뒤, 정렬이 맞으면 후진한다.
    /// 도주 경로는 타겟에서 멀어지는 방향이라 대체로 정면이 타겟 쪽을 향하지만,
    /// 장애물을 크게 돌아갈 때는 일시적으로 어긋날 수 있다.
    /// </summary>
    private void FleeBackward()
    {
        if (Target == null) return;

        if (TryGetAgentSteeringDirection(out Vector3 dir) == false) return; // 경로가 아직 준비 안 됐으면 대기

        RotateBodyToward(transform.position - dir, Time.fixedDeltaTime); // 경로 반대 방향을 정면으로

        float angle = Vector3.Angle(-transform.forward, dir);
        if (angle < 10f && IsBlocked(checkBackward: true) == false)
        {
            MoveBackward();
        }
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
        if (hitData.AtkTank == null) return;

        // 공격한 탱크를 바로 타겟으로 설정
        SetTarget(hitData.AtkTank.transform);
        ChangeState(EnemyStateType.Combat);
    }
}