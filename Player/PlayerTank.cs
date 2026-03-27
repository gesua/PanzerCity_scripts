using UnityEngine;

public class PlayerTank : TankBase
{
    [Header("----- 컴포넌트 -----")]
    [SerializeField] Mover _mover;
    [SerializeField] Turret _turret;

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
}
