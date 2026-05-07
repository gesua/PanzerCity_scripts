using UnityEngine;

public enum EffectType // 파일 이름과 동일하게 해야함
{
    // _Tanks 이펙트
    CompleteShellExplosion, // 연기 일어남(포탄용)
    TankExplosion,          // 빛나는 바늘이 퍼져나감 [미사용]
    // UnityTechnologies 이펙트
    TinyExplosion,          // 포신 폭발용
    SmallExplosion,         // 탱크 폭발용
    Twinkle,                // 반짝(적 스폰용)
    PressurisedSteam,       // 탱크 엔진 흰 연기 [직접 붙여서 사용중]
    TinyFlames,             // 탱크 파괴 후 잔불
    SmokeEffect,            // 탱크 잔해 검은 연기 [미사용]
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
        _poolManager.GetPool(GetPrefabPath(EffectType.CompleteShellExplosion));
        _poolManager.GetPool(GetPrefabPath(EffectType.TankExplosion));
        _poolManager.GetPool(GetPrefabPath(EffectType.TinyExplosion));
        _poolManager.GetPool(GetPrefabPath(EffectType.SmallExplosion));
        _poolManager.GetPool(GetPrefabPath(EffectType.Twinkle));
        _poolManager.GetPool(GetPrefabPath(EffectType.PressurisedSteam));
        _poolManager.GetPool(GetPrefabPath(EffectType.TinyFlames));
        _poolManager.GetPool(GetPrefabPath(EffectType.SmokeEffect));
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
    public void SpawnEffect(EffectType effectType, Vector3 pos)
    {
        // Pool에서 이펙트 경로에 따라 게임 오브젝트 꺼내옴
        GameObject effectGo = _poolManager.GetFromPool(GetPrefabPath(effectType));

        // Pool에서 가져온 게임 오브젝트 설정
        effectGo.transform.position = pos;

        // 이펙트 재생
        if (effectGo.GetComponentInChildren<Effect>() is Effect effect)
        {
            effect.Play();
        }
    }
}