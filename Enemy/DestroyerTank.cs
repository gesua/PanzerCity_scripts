using UnityEngine;

/// <summary>
/// 구축전차
/// Rigidbody Mass:0.9
/// </summary>
public class DestroyerTank : EnemyTank
{
    [Header("----- 구축전차 AI -----")]
    [SerializeField] float _fireAngle = 8f;        // 이 각도 안에 들어와야 발사
    [SerializeField] float _directMoveAngle = 12f; // 직접 조준 중 전진 가능한 각도
    [SerializeField] float _pathMoveAngle = 10f;   // 벽 뒤 타겟을 따라갈 때 경로 전진 가능한 각도

    public override bool CanAimWhileMoving => false;
    public override bool UsesBodyAim => true;

    // _turret 오브젝트는 위치 기준점으로만 쓰고, 방향은 차체 정면을 사용한다.
    protected override Vector3 AimForward => transform.forward;

    public override void Initialize()
    {
        base.Initialize();

        // 성격은 고정형 또는 공격형만 나오게 함
        _personality = (UnityEngine.Random.value < 0.5f)
            ? EnemyPersonality.Stationary
            : EnemyPersonality.Aggressive;
    }

    /// <summary>
    /// 구축전차는 포탑이 없으므로 차체 전체를 돌려서 조준한다.
    /// </summary>
    public override void AimAtTarget()
    {
        if (Target == null) return;
        RotateBodyToward(Target.position, Time.deltaTime);
    }

    /// <summary>
    /// 고정형 상태에서도 차체 조준을 그대로 사용한다.
    /// </summary>
    public override void RotateBodyToTarget()
    {
        AimAtTarget();
    }

    /// <summary>
    /// 구축전차 이동은 두 단계로 나뉜다.
    /// 플레이어가 직접 보이면 차체 조준을 우선하고, 벽 뒤에 있으면 NavMesh 경로를 따라간다.
    /// </summary>
    public override void AgentMove()
    {
        if (Target == null) return;

        if (HasClearVisionTo(Target.position))
        {
            MoveWhileAimingAtTarget();
            return;
        }

        FollowPathToHiddenTarget();
    }

    /// <summary>
    /// 구축전차는 감지 각도보다 더 좁은 발사 각도를 사용
    /// </summary>
    public override bool CanAttackTarget()
    {
        if (CanSeePlayer() == false) return false;
        return GetAimAngleTo(Target.position) <= _fireAngle;
    }

    /// <summary>
    /// 발사 하면 무조건 플레이어 방향으로 포탄이 날아가게 함
    /// </summary>
    public override void Attack()
    {
        if (Target != null)
        {
            Vector3 dir = Target.position - _firePoint.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > Mathf.Epsilon)
                _firePoint.rotation = Quaternion.LookRotation(dir);
        }

        base.Attack();
    }

    /// <summary>
    /// 플레이어가 직접 보일 때는 경로 방향으로 차체를 꺾지 않고, 조준 방향으로만 전진한다.
    /// 공격형 상태에서 이미 AimAtTarget을 호출했으므로 여기서는 회전을 반복하지 않는다.
    /// </summary>
    void MoveWhileAimingAtTarget()
    {
        float angle = GetAimAngleTo(Target.position);
        if (angle > _directMoveAngle || IsBlocked())
        {
            SetEngineEffect(false);
            return;
        }

        SetEngineEffect(true);
        MoveForward();
    }

    /// <summary>
    /// 플레이어가 벽 뒤에 있으면 조준보다 길찾기를 우선한다.
    /// 이때만 일반 탱크처럼 NavMesh의 다음 경로점 방향으로 차체를 돌린다.
    /// </summary>
    void FollowPathToHiddenTarget()
    {
        if (TryGetAgentSteeringDirection(out Vector3 dir) == false)
        {
            SetEngineEffect(false);
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, _model.RotSpeed * Time.fixedDeltaTime);

        float angle = Vector3.Angle(transform.forward, dir);
        if (angle > _pathMoveAngle || IsBlocked())
        {
            SetEngineEffect(false);
            return;
        }

        SetEngineEffect(true);
        MoveForward();
    }
}
