using UnityEngine;
using UnityEngine.Pool;

/// <summary>
/// 플레이어와 적이 사용할 TankBase
/// 데이터테이블에서 _tankID로 TankModel을 불러옴
/// 공격 기능이 들어있음
/// </summary>
[RequireComponent(typeof(TankModel))]
public abstract class TankBase : MonoBehaviour, IAttackable
{
    [SerializeField] int _tankID; // TankData에서 데이터 가져올 ID
    [Header("----- 컴포넌트 -----")]
    [SerializeField] Transform _firePoint; // 포탄 생성 위치
    [SerializeField] Effect _engineEffect; // 엔진 이펙트

    protected TankModel _model;
    string _shellPrefabPath = "Shell"; // 포탄 프리팹 위치
    bool _isEffectPlaying; // 엔진 이펙트 재생 여부

    protected virtual bool ShowMuzzleEffect => true; // 포신 이펙트 보여줄지 여부(플레이어 저격 모드엔 안 보임)

    protected virtual void Awake()
    {
        _model = GetComponent<TankModel>();
        TankData data = GameManager.Instance.DataManager.GetTankData(_tankID);
        if (data != null) _model.Initialize(data);

        // Pool 생성
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

        // 포탄 생성
        GameObject shellGo = GameManager.Instance.PoolManager.GetFromPool(_shellPrefabPath);
        shellGo.transform.position = _firePoint.position;
        shellGo.transform.rotation = _firePoint.rotation;

        // 포탄 초기화
        Shell shell = shellGo.GetComponent<Shell>();

        shell.Initialize(_model, gameObject.layer);
    }

    protected void SetEngineEffect(bool isMoving)
    {
        if (isMoving && _isEffectPlaying == false)
        {
            _isEffectPlaying = true;
            _engineEffect.Play();
        }
        else if (isMoving == false && _isEffectPlaying)
        {
            _isEffectPlaying = false;
            _engineEffect.Stop();
        }
    }
}