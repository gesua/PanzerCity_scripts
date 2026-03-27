using UnityEngine;

[RequireComponent(typeof(TankModel))]
public abstract class TankBase : MonoBehaviour, IAttackable
{
    protected TankModel _model;
    [SerializeField] protected Transform _firePoint; // 포탄 생성 위치

    public bool CanAttack { get; protected set; } = true;

    protected virtual void Awake()
    {
        _model = GetComponent<TankModel>();
        _model.Initialize();
    }

    public virtual void Attack()
    {
        if (!CanAttack) return;

        // HACK:포탄 생성 등 공통 로직
        //StartCoroutine(ReloadRoutine());
    }

    /*
    IEnumerator ReloadRoutine()
    {
        CanAttack = false;
        yield return new WaitForSeconds(GetReloadTime());
        CanAttack = true;
    }
    */
}