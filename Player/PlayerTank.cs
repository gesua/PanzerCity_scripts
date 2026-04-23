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
    [SerializeField] GameObject _normalVisual; // 플레이 모델
    [SerializeField] GameObject _destroyedVisual; // 파괴된 모델
    [SerializeField] GameObject _destroyedTurret; // 파괴된 포탑
    [Header("----- 런타임 데이터 -----")]
    [SerializeField] float _deadDuration = 5f;  // 사망 상태 지속 시간

    bool _isAttack; // 좌클릭 누르는 중인지
    bool _isSniperMode; // 저격 모드인지(Shift)
    float _reloadTimer; // 재장전 시간 잴거

    public Transform TurretTr => _turret.TurretTr;
    public TankModel Model => _model;
    public Vector3 BarrelForward => _turret.BarrelForward; // HACK:조준점 맞추는거 해결중

    protected override bool ShowMuzzleEffect => !_isSniperMode;

    protected override void Awake()
    {
        base.Awake();

        _model.OnDead += HandleDead; // 사망 이벤트 구독

        Initialize();
    }

    public void Initialize()
    {
        _mover.Initialize(_model);
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
    /// 좌클릭 누른 여부
    /// </summary>
    public void SetIsAttack(bool isActive)
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

    /// <summary>
    /// 저격 모드 여부
    /// </summary>
    public void SetSniperMode(bool isSniper)
    {
        _isSniperMode = isSniper;
        _turret.SetSniperMode(isSniper);
    }

    /// <summary>
    /// 사망 처리
    /// </summary>
    void HandleDead()
    {
        // 모델 교체
        _normalVisual.SetActive(false);
        _destroyedVisual.SetActive(true);

        // 포탑 위치 맞춰줌
        _destroyedTurret.transform.localRotation = TurretTr.localRotation;

        // 폭발 이펙트 재생
        GameManager.Instance.EffectSpawner.SpawnEffect(EffectType.SmallExplosion, TurretTr.position);
    }
}
