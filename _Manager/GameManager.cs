using UnityEngine;

/// <summary>
/// 매니저 관리하는 매니저
/// 현재 리소스 매니저, 풀 매니저, 이펙트스포너(이펙트 관리) 가지고 있음
/// </summary>
public class GameManager : Singleton<GameManager>
{
    ResourceManager _resourceManager;
    PoolManager _poolManager;
    EffectSpawner _effectSpawner;

    public ResourceManager ResourceManager => _resourceManager;
    public PoolManager PoolManager => _poolManager;
    public EffectSpawner EffectSpawner => _effectSpawner;

    protected override void Awake()
    {
        base.Awake();

        _resourceManager = gameObject.GetOrAddComponent<ResourceManager>();
        _poolManager = gameObject.GetOrAddComponent<PoolManager>();
        _effectSpawner = gameObject.GetOrAddComponent<EffectSpawner>();

        _poolManager.Initialize(_resourceManager);
        _effectSpawner.Initialize();
    }
}
