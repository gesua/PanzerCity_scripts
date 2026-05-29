using UnityEngine;

/// <summary>
/// 매니저 관리하는 매니저
/// 리소스 매니저, 풀 매니저, 이펙트스포너(이펙트 관리) 등 갖고 있음
/// </summary>
public class GameManager : Singleton<GameManager>
{
    ResourceManager _resourceManager;
    PoolManager _poolManager;
    DataManager _dataManager;
    EffectManager _effectManager;
    AudioManager _audioManager;
    OptionManager _optionManager;
    PlayerData _playerData;
    LoadingUI _loadingUI;

    public ResourceManager ResourceManager => _resourceManager;
    public PoolManager PoolManager => _poolManager;
    public DataManager DataManager => _dataManager;
    public EffectManager EffectManager => _effectManager;
    public AudioManager AudioManager => _audioManager;
    public OptionManager OptionManager => _optionManager;
    public PlayerData PlayerData => _playerData;
    public LoadingUI LoadingUI => _loadingUI;

    protected override void Awake()
    {
        base.Awake();

        _resourceManager = gameObject.GetOrAddComponent<ResourceManager>();
        _poolManager = gameObject.GetOrAddComponent<PoolManager>();
        _dataManager = gameObject.GetOrAddComponent<DataManager>();
        _effectManager = gameObject.GetOrAddComponent<EffectManager>();
        _optionManager = gameObject.GetComponent<OptionManager>();
        _playerData = gameObject.GetOrAddComponent<PlayerData>();

        _poolManager.Initialize(_resourceManager);
        _dataManager.Initialize();
        _effectManager.Initialize();
        _optionManager.Initialize();
        _playerData.Initialize(0, 3);

        GameObject loadingUIPrefab = Resources.Load<GameObject>("UI/LoadingUI");
        GameObject loadingGo = Instantiate(loadingUIPrefab);
        DontDestroyOnLoad(loadingGo);
        _loadingUI = loadingGo.GetComponent<LoadingUI>();

        /*
        GameObject audioManagerPrefab = Resources.Load<GameObject>("AudioManager");
        GameObject audioManagerGo = Instantiate(audioManagerPrefab);
        DontDestroyOnLoad(audioManagerGo);
        _audioManager = audioManagerGo.GetComponent<AudioManager>();
        */
    }
}
