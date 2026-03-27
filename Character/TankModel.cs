using System;
using UnityEngine;

/// <summary>
/// 탱크 기본 데이터
/// </summary>
public class TankModel : MonoBehaviour
{
    [Header("----- 이동 -----")]
    [SerializeField] float _forwardSpeed = 5f;  // 전진 속력
    [SerializeField] float _backwardSpeed = 2f; // 후진 속력
    [SerializeField] float _rotSpeed = 100f;    // 회전 속력
    [SerializeField] float _acceleration = 10f; // 가속도(속도 증가율)
    [SerializeField] float _deceleration = 5f;  // 감속도(키를 놓았을 때 천천히 멈추는 속도)

    [Header("----- 포탑 -----")]
    [SerializeField] float _turretRotSpeed = 100f; // 포탑 회전 속력

    [Header("----- 피격 -----")]
    [SerializeField] int _maxHp = 1;    // 최대 체력
    [SerializeField] int _currentHp;    // 현재 체력

    [Header("----- 공격 -----")]
    [SerializeField] int _damage = 1;   // 공격력
    [SerializeField] float _reloadMinTime = 1.0f; // AI용 최소 재장전 시간(이거 플레이어는 어쩌지)
    [SerializeField] float _reloadMaxTime = 1.0f; // AI용 최대 재장전 시간

    public float ForwardSpeed => _forwardSpeed;
    public float BackwardSpeed => _backwardSpeed;
    public float RotSpeed => _rotSpeed;
    public float Deceleration => _deceleration;
    public float Acceleration => _acceleration;
    public float TurretRotSpeed => _turretRotSpeed;
    public int MaxHp => _maxHp;
    public int CurrentHp => _currentHp;
    public bool IsAlive => _currentHp > 0; // 살아있는지 여부
    public int Damage => _damage;

    /// <summary>
    /// 체력 변경 이벤트(현재 체력, 최대 체력)
    /// </summary>
    public event Action<int, int> OnHpChanged;
    public event Action OnDead;

    public void Initialize()
    {
        _currentHp = _maxHp;
        OnHpChanged?.Invoke(_currentHp, _maxHp);
    }

    public void TakeDamage(int damage)
    {
        if (IsAlive == false) return;

        _currentHp = Mathf.Clamp(_currentHp - damage, 0, _maxHp);

        OnHpChanged?.Invoke(_currentHp, _maxHp);

        if (IsAlive == false)
        {
            OnDead?.Invoke();
        }
    }
}
