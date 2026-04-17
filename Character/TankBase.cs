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

    [SerializeField] string _playerShellPrefabPath = "PlayerShell"; // 플레이어만 쓸 전용 프리팹 HACK: 포탄 사라지는거 해결중

    protected virtual bool ShowMuzzleEffect => true; // 포신 이펙트 보여줄지 여부(플레이어 저격 모드엔 안 보임)

    protected virtual void Awake()
    {
        _model = GetComponent<TankModel>();
        //TankData data = DataManager.Instance.GetTankData(_tankID); // 아직 데이터 가져오는거 안 만듦
        _model.Initialize();

        // Pool 생성
        GameManager.Instance.PoolManager.GetPool(_playerShellPrefabPath); // 플레이어 전용 포탄 풀 생성 HACK: 포탄 사라지는거 해결중
        GameManager.Instance.PoolManager.GetPool(_shellPrefabPath);
    }

    public virtual void Attack()
    {
        // 저격 모드엔 포신 이펙트 안 보이게 함
        if (ShowMuzzleEffect)
        {
            // 포신 이펙트 생성
            GameManager.Instance.EffectSpawner.SpawnEffect(EffectType.TinyExplosion, _firePoint.position);
        }

        GameObject shellGo;
        // 플레이어 전용 포탄 HACK: 포탄 사라지는거 해결중
        if (gameObject.name == "Player")
        {
            // 플레이어 전용 포탄 생성
            shellGo = GameManager.Instance.PoolManager.GetFromPool(_playerShellPrefabPath);
        }
        else // 일반 포탄
        {
            shellGo = GameManager.Instance.PoolManager.GetFromPool(_shellPrefabPath);
        }

        //Rigidbody shellRigid = shellGo.GetComponent<Rigidbody>();
        //shellRigid.position = _firePoint.position;
        //shellRigid.rotation = _firePoint.rotation;

        shellGo.transform.position = _firePoint.position;
        shellGo.transform.rotation = _firePoint.rotation;
        shellGo.SetActive(true);

        // 포탄 초기화
        Shell shell = shellGo.GetComponent<Shell>();
        shell.Initialize(_model.ShellDamage, _model.ShellSpeed, _model.ExplosionRadius, _model.HitLayer, gameObject.layer);
    }
}