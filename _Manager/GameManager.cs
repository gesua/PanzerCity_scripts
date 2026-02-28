using UnityEngine;

/// <summary>
/// 매니저 관리하는 매니저
/// </summary>
public class GameManager : Singleton<GameManager>
{
    ResourceManager _resourceManager;
    PoolManager _poolManager;

    public ResourceManager ResourceManager => _resourceManager;
    public PoolManager PoolManager => _poolManager;

    protected override void Awake()
    {
        base.Awake();

        _resourceManager = gameObject.GetOrAddComponent<ResourceManager>();
        _poolManager = gameObject.GetOrAddComponent<PoolManager>();
    }
}
