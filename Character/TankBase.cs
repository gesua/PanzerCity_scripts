using UnityEngine;

/// <summary>
/// 플레이어와 적이 사용할 TankBase
/// 데이터테이블에서 _tankID로 TankModel을 불러옴
/// 공격 기능이 들어있음
/// </summary>
[RequireComponent(typeof(TankModel))]
public abstract class TankBase : MonoBehaviour, IAttackable
{
    int _tankID; // TankData에서 데이터 가져올 ID
    protected TankModel _model;
    [SerializeField] string _shellPrefabPath = "Shell"; // 포탄 프리팹 위치
    [SerializeField] Transform _firePoint; // 포탄 생성 위치

    public bool CanAttack { get; protected set; } = true;

    protected virtual void Awake()
    {
        _model = GetComponent<TankModel>();
        //TankData data = DataManager.Instance.GetTankData(_tankID); // 아직 데이터 가져오는거 안 만듦
        _model.Initialize();

        // Pool 생성
        GameManager.Instance.PoolManager.GetPool(_shellPrefabPath);
    }

    public virtual void Attack()
    {
        if (!CanAttack) return;

        // 포신 이펙트 생성
        GameManager.Instance.EffectSpawner.SpawnEffect(EffectType.TinyExplosion, _firePoint.position);

        // 포탄 생성
        GameObject shellGo = GameManager.Instance.PoolManager.GetFromPool(_shellPrefabPath);
        shellGo.transform.position = _firePoint.position;
        shellGo.transform.rotation = _firePoint.rotation;

        // 포탄 초기화
        Shell shell = shellGo.GetComponent<Shell>();
        shell.Initialize(_model.ShellDamage, _model.ShellSpeed, _model.ExplosionRadius, _model.HitLayer, gameObject.layer);
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