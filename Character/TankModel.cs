using System;
using UnityEngine;

/// <summary>
/// 탱크 기본 데이터
/// 데이터는 TankData에서 가져오면 됨
/// </summary>
public class TankModel : MonoBehaviour
{
    [Header("----- 치트 -----")]
    [SerializeField] bool _infiniteHP; // HP 무한
    [SerializeField] bool _noDamage;   // 피격 무시

    [Header("----- 이동 -----")]
    [SerializeField] float _forwardSpeed = 5f;  // 전진 속력
    [SerializeField] float _backwardSpeed = 2f; // 후진 속력
    [SerializeField] float _rotSpeed = 100f;    // 회전 속력
    [SerializeField] float _acceleration = 10f; // 가속도(속도 증가율)
    [SerializeField] float _deceleration = 5f;  // 감속도(키를 놓았을 때 천천히 멈추는 속도)
    float _speedMultiplier = 1f; // 차체 이동 속도 배율(진흙 지형 효과)

    [Header("----- 포탑 -----")]
    [SerializeField] float _turretRotSpeed = 100f; // 포탑 회전 속력

    [Header("----- 피격 -----")]
    [SerializeField] int _maxHp = 1; // 최대 체력
    [SerializeField] int _currentHp; // 현재 체력

    [Header("----- 포탄 -----")]
    [SerializeField] int _shellDamage = 1;   // 포탄 공격력
    [SerializeField] float _shellSpeed = 10f; // 포탄 속력
    [SerializeField] float _explosionRadius = 1f; // 포탄 폭발 반경
    [SerializeField] float _minAttackTime = 1.0f; // AI용 최소 재장전 시간(플레이어는 이 값만 사용)
    [SerializeField] float _maxAttackTime = 1.0f; // AI용 최대 재장전 시간
    [SerializeField] LayerMask _hitLayer; // 포탄과 충돌할 레이어

    public float ForwardSpeed => _forwardSpeed * _speedMultiplier;
    public float BackwardSpeed => _backwardSpeed * _speedMultiplier;
    public float RotSpeed => _rotSpeed * _speedMultiplier;
    public float Deceleration => _deceleration;
    public float Acceleration => _acceleration;
    public float TurretRotSpeed => _turretRotSpeed;
    public int MaxHp => _maxHp;
    public int CurrentHp => _currentHp;
    public bool IsAlive => _currentHp > 0; // 살아있는지 여부
    public int ShellDamage => _shellDamage;
    public float ShellSpeed => _shellSpeed;
    public float ExplosionRadius => _explosionRadius;
    public float MinAttackTime => _minAttackTime;
    public float MaxAttackTime => _maxAttackTime;
    public LayerMask HitLayer => _hitLayer;

    /// <summary>
    /// 체력 변경 이벤트(현재 체력, 최대 체력)
    /// </summary>
    public event Action<int, int> OnHpChanged;
    public event Action<HitData> OnHit;
    public event Action<HitData> OnDead;

    /// <summary>
    /// TankData 안 들어가는 기본값들 초기화
    /// </summary>
    public void Initialize()
    {
        _currentHp = _maxHp;
        _speedMultiplier = 1f; // 배율 초기화
        OnHpChanged?.Invoke(_currentHp, _maxHp);
    }

    /// <summary>
    /// TankData 넣고 초기화
    /// </summary>
    /// <param name="data"></param>
    public void Initialize(TankData data)
    {
        _maxHp = data.MaxHP;
        _currentHp = data.MaxHP;
        _forwardSpeed = data.MoveSpeed_Fwd;
        _backwardSpeed = data.MoveSpeed_Bwd;
        _rotSpeed = data.RotateSpeed;
        _turretRotSpeed = data.TurretRotateSpeed;
        _minAttackTime = data.MinAttackTime;
        _maxAttackTime = data.MaxAttackTime;
        _shellDamage = data.ShellDamage;
        _shellSpeed = data.ShellSpeed;
        _explosionRadius = data.ExplosionRadius;
        _speedMultiplier = 1f; // 배율 초기화

        OnHpChanged?.Invoke(_currentHp, _maxHp);
    }


    public void TakeDamage(HitData hitData)
    {
        if (IsAlive == false) return;
        if (_noDamage) return; // 무적 치트

        _currentHp = Mathf.Clamp(_currentHp - hitData.Damage, 0, _maxHp);

        OnHpChanged?.Invoke(_currentHp, _maxHp);

        if (_infiniteHP && _currentHp < 1) _currentHp = _maxHp; // HP무한 치트

        // 사망
        if (IsAlive == false) OnDead?.Invoke(hitData);

        OnHit?.Invoke(hitData);
    }

    /// <summary>
    /// HP 동기화
    /// </summary>
    public void ApplySyncedHp(int syncedHp, HitData hitData)
    {
        if (IsAlive == false) return; // 이미 사망 상태면 무시

        int clampedHp = Mathf.Clamp(syncedHp, 0, _maxHp);
        if (clampedHp == _currentHp) return; // 실제로 값이 바뀐 경우에만 반영(중복 이벤트 방지 — 오너의 값이 아직 도착 전인 호출은 여기서 조용히 스킵됨)

        _currentHp = clampedHp;
        OnHpChanged?.Invoke(_currentHp, _maxHp);
        if (IsAlive == false) OnDead?.Invoke(hitData);
        OnHit?.Invoke(hitData);
    }

    /// <summary>
    /// 무적 세팅
    /// </summary>
    public void SetNoDamage(bool noDamage)
    {
        _noDamage = noDamage;
    }

    /// <summary>
    /// 차체 이동 속도 배율 설정(진흙)
    /// </summary>
    public void SetSpeedMultiplier(float multiplier)
    {
        _speedMultiplier = multiplier;
    }

    /// <summary>
    /// 장비 스탯 적용
    /// </summary>
    public void ApplyEquipment(ItemConfig config, bool equip)
    {
        int sign = equip ? 1 : -1;

        foreach (StatBonus bonus in config.StatBonuses)
        {
            switch (bonus.StatType)
            {
                case StatType.ShellDamage: _shellDamage += (int)(bonus.Value * sign); break;
                case StatType.ShellSpeed: _shellSpeed += bonus.Value * sign; break;
                case StatType.ExplosionRadius: _explosionRadius += bonus.Value * sign; break;
                case StatType.Reload: _minAttackTime += bonus.Value * sign; break;
                case StatType.TurretRotSpeed: _turretRotSpeed += bonus.Value * sign; break;
                case StatType.ForwardSpeed: _forwardSpeed += bonus.Value * sign; break;
                case StatType.BackwardSpeed: _backwardSpeed += bonus.Value * sign; break;
                case StatType.Acceleration: _acceleration += bonus.Value * sign; break;
                case StatType.RotSpeed: _rotSpeed += bonus.Value * sign; break;
            }
        }
    }
}
