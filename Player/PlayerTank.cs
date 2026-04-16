using UnityEngine;

/// <summary>
/// 플레이어 탱크
/// </summary>
public class PlayerTank : TankBase
{
    [Header("----- 컴포넌트 -----")]
    [SerializeField] Mover _mover;
    [SerializeField] Turret _turret;

    bool _isSniperMode;

    protected override bool ShowMuzzleEffect => !_isSniperMode;

    private void Start()
    {
        Initialize();
    }

    public void Initialize()
    {
        _model.Initialize();
        _mover.Initialize(_model.ForwardSpeed, _model.BackwardSpeed, _model.RotSpeed, _model.Acceleration, _model.Deceleration);
        _turret.SetRotSpeed(_model.TurretRotSpeed);
    }

    public void Move(Vector3 dir)
    {
        _mover.Move(dir);
    }

    public void SetSniperMode(bool active)
    {
        _isSniperMode = active;
    }
}
