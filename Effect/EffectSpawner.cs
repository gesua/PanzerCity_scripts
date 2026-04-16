using UnityEngine;

public enum EffectType // 파일 이름과 동일하게 해야함
{
    // _Tanks 이펙트
    CompleteShellExplosion,
    TankExplosion,
    // UnityTechnologies 이펙트
    TinyExplosion,
    SmallExplosion,
}

/// <summary>
/// VFX(비주얼 이펙트)를 원하는 위치에 스폰
/// GameManager에서 관리되고 있음
/// </summary>
public class EffectSpawner : MonoBehaviour
{
    PoolManager _poolManager;

    public void Initialize()
    {
        _poolManager = GameManager.Instance.PoolManager;
        _poolManager.GetPool(GetPrefabPath(EffectType.CompleteShellExplosion), 10);
        _poolManager.GetPool(GetPrefabPath(EffectType.TankExplosion), 10);
        _poolManager.GetPool(GetPrefabPath(EffectType.TinyExplosion), 10);
        _poolManager.GetPool(GetPrefabPath(EffectType.SmallExplosion), 10);
    }

    /// <summary>
    /// 이펙트 종류에 따른 프리팹 경로를 반환하는 함수
    /// </summary>
    string GetPrefabPath(EffectType effectType)
    {
        return $"Effect/{effectType.ToString()}";
    }

    /// <summary>
    /// 이펙트를 생성하고 재생하는 함수
    /// </summary>
    /// <param name="effectType">이펙트 종류</param>
    /// <param name="pos">재생할 위치</param>
    public void SpawnEffect(EffectType effectType, Vector3 pos, Transform parent = null)
    {
        // Pool에서 이펙트 경로에 따라 게임 오브젝트 꺼내옴
        GameObject effectGo = _poolManager.GetFromPool(GetPrefabPath(effectType));

        // Pool에서 가져온 게임 오브젝트 설정
        if (parent == null) parent = transform;
        effectGo.transform.SetParent(parent);
        effectGo.transform.position = pos;

        // 이펙트 재생
        if (effectGo.TryGetComponent(out Effect effect) == true)
        {
            effect.Play();
        }
    }
}