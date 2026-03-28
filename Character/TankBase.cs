using UnityEngine;

[RequireComponent(typeof(TankModel))]
public abstract class TankBase : MonoBehaviour, IAttackable
{
    protected TankModel _model;
    [SerializeField] string _shellPrefabPath = "Shell"; // 포탄 프리팹 위치
    [SerializeField] Transform _firePoint; // 포탄 생성 위치

    public bool CanAttack { get; protected set; } = true;

    protected virtual void Awake()
    {
        _model = GetComponent<TankModel>();
        _model.Initialize();

        // Pool 생성
        GameManager.Instance.PoolManager.GetPool(_shellPrefabPath);
    }

    public virtual void Attack()
    {
        if (!CanAttack) return;

        // 포탄 생성
        GameObject shellGo = GameManager.Instance.PoolManager.GetFromPool(_shellPrefabPath);
        shellGo.transform.position = _firePoint.position;
        shellGo.transform.rotation = _firePoint.rotation;

        // 포탄 초기화
        Shell shell = shellGo.GetComponent<Shell>();
        shell.Initialize(_model.ShellDamage, _model.ShellSpeed, _model.HitLayer, gameObject.layer);
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