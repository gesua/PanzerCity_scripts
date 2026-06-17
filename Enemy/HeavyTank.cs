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

    public override void TakeHit(HitData hitData)
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

        base.TakeHit(hitData);
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

    /// <summary>
    /// HACK:도주형 해본거(레이캐스트랑 엔진 연기 등 이렇게 하면 안될듯)
    /// </summary>
    public override void AgentMove()
    {
        // 도주형만 따로 계산
        if (_personality != EnemyPersonality.Coward)
        {
            base.AgentMove();
            return;
        }

        // 도주형일 때 후진으로 도주
        if (TryGetAgentSteeringDirection(out Vector3 dir) == false) return;

        // 후진하려면 차체 뒤쪽이 경로 방향을 향해야 함
        Quaternion targetRotation = Quaternion.LookRotation(-dir);

        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRotation,
            _model.RotSpeed * Time.fixedDeltaTime);

        float angle = Vector3.Angle(-transform.forward, dir);

        if (angle < 10f)
        {
            MoveBackward();
        }
    }
}
