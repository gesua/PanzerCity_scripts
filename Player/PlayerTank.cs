using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

/// <summary>
/// 플레이어 탱크
/// </summary>
public class PlayerTank : TankBase
{
    [Header("----- 컴포넌트 -----")]
    [SerializeField] Mover _mover;
    [SerializeField] Turret _turret;
    [SerializeField] ReloadIndicator _reloadIndicator; // 재장전 표시 UI

    bool _isAttack; // 좌클릭 누르는 중인지
    bool _isSniperMode; // 저격 모드인지(Shift)
    float _reloadTimer; // 재장전 시간 잴거

    public Transform TurretTr => _turret.TurretTr;

    protected override bool ShowMuzzleEffect => !_isSniperMode;

    private void Start()
    {
        Initialize();
    }

    public void Initialize()
    {
        _mover.Initialize(_model.ForwardSpeed, _model.BackwardSpeed, _model.RotSpeed, _model.Acceleration, _model.Deceleration);
        _turret.SetRotSpeed(_model.TurretRotSpeed);
    }

    public void Move(Vector3 dir)
    {
        _mover.Move(dir);
    }

    /// <summary>
    /// 공격
    /// </summary>
    public override void Attack()
    {
        if (_isAttack == false) return; // 좌클릭 안 눌림

        base.Attack();
        _reloadTimer = _model.MinAttackTime; // 플레이어는 min,max 아무거나 가져오기
    }

    /// <summary>
    /// 좌클릭 누르고 있는지 확인
    /// </summary>
    public void IsAttack(bool isActive)
    {
        _isAttack = isActive;
    }

    private void Update()
    {
        // 재장전 체크
        if (_reloadTimer > 0f)
        {
            _reloadTimer -= Time.deltaTime;
            _reloadIndicator.UpdateReload(_model.MinAttackTime - _reloadTimer, _model.MinAttackTime);
        }
        else
        {
            Attack();
        }
    }

    public void SetSniperMode(bool active)
    {
        _isSniperMode = active;
    }
}
