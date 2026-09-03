using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 멀티플레이 게임 관리 — 플레이어 스폰 담당
/// </summary>
public class NetworkGameManager : NetworkBehaviour
{
    [SerializeField] GameObject _playerPrefab;
    [SerializeField] GameObject _droppedItemMultiPrefab;
    [SerializeField] GameObject _shellMultiPrefab;

    // 모든 클라이언트의 씬 로드 완료 여부
    bool _waitForSceneLoaded;

    HashSet<ulong> _readyForNextStageClientIds = new(); // 상점에서 다음 스테이지 준비 완료한 클라이언트 목록
    HashSet<ulong> _stageLoadedClientIds = new(); // 스테이지 전환 시 씬 로드 완료 보고한 클라이언트 목록
    Dictionary<ulong, int> _playerLives = new(); // 전원 사망 판정용:각 클라이언트의 최신 목숨 캐시(호스트만 판정에 사용, NotifyLifeChanged가 호출될 때마다 갱신)
    bool _allPlayersDeadNotified; // 전원 사망 중복 알림 방지(호스트 전용, 재도전 시 리셋됨)

    StageScene _stageScene;

    public static NetworkGameManager Instance { get; private set; }
    public StageScene StageScene => _stageScene;

    public event Action<PlayerTank> OnLocalPlayerSpawned; // 로컬 플레이어 스폰 완료 알림
    public event Action OnAllClientsReady; // 모든 클라이언트 씬 로드 완료(로딩창/적 스폰 동시 시작용, 로컬 신호)
    public event Action<int, int> OnPlayerLifeChanged; // 목숨 UI 갱신용(playerIndex, life)
    public event Action<int> OnPlayerLeft; // 목숨 UI 슬롯 비활성화용(playerIndex = 나간 클라이언트의 clientId)
    public event Action<int, int> OnNextStageReadyCountChanged; // 상점 다음 스테이지 준비 인원 변경(readyCount, totalCount)
    public event Action OnAllReadyForNextStage; // 전원 준비 완료 — 다음 스테이지로 이동 신호
    public event Action<bool> OnShopActiveChanged; // 로컬 상점 UI 열림/닫힘 알림(순수 로컬 신호, 네트워크 전파 없음)
    public event Action OnRestartRequested; // 재도전 동기화 — 호스트 재도전 신호
    public event Action OnAllPlayersDead; // 전원 사망 동기화 — 접속한 모든 클라이언트의 목숨이 0이 됨(비호스트만 실제로 반응함, 호스트는 판정 시점에 이미 로컬 처리)
    public event Action<ulong, string> OnChatMessageReceived; // 채팅 메시지 수신(senderClientId, message) — 전원(호스트 포함) 동일하게 수신
    public event Action<float> OnBaseShieldActivated; // 기지 무적 발동(퀵슬롯 UI 등 로컬 연출용)
    public event Action<float> OnEMPFieldActivated;   // EMP 신규 발동(퀵슬롯 UI 등 로컬 연출용)
    public event Action OnAirSupportActivated; // 폭탄 발동(대상 유무와 무관하게 사용 시점마다 정확히 한 번씩 발행, 사용음 재생용)

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += OnLoadEventCompleted;
        NetworkManager.Singleton.OnClientDisconnectCallback += HandleClientDisconnect;

        // 클라이언트 측 DroppedItem_Multi 스폰/디스폰을 Pool 시스템으로 위임
        if (_droppedItemMultiPrefab == null)
        {
            Debug.LogWarning("DroppedItemMultiPrefab이 연결되지 않았습니다.");
        }
        else
        {
            NetworkManager.Singleton.PrefabHandler.AddHandler(_droppedItemMultiPrefab, new NetworkPoolPrefabHandler("DroppedItem_Multi"));
        }

        // 클라이언트 측 Shell_Multi 스폰을 Pool 시스템으로 위임
        // (디스폰은 destroy:false라 Destroy()가 호출되지 않으므로, 클라이언트 반환은 ShellNetworkOwner.OnNetworkDespawn()이 담당)
        if (_shellMultiPrefab == null)
        {
            Debug.LogWarning("ShellMultiPrefab이 연결되지 않았습니다.");
        }
        else
        {
            NetworkManager.Singleton.PrefabHandler.AddHandler(_shellMultiPrefab, new NetworkPoolPrefabHandler("Shell_Multi"));
        }
    }

    public override void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            // Shutdown 처리 도중엔 SceneManager 같은 하위 서브시스템이 Singleton 자체보다 먼저 사라질 수 있어 별도 체크
            if (NetworkManager.Singleton.SceneManager != null)
            {
                NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -= OnLoadEventCompleted;
            }

            NetworkManager.Singleton.OnClientDisconnectCallback -= HandleClientDisconnect;
        }

        base.OnDestroy();
    }

    /// <summary>
    /// 스테이지 준비 완료 — GameScene이 명시적으로 호출
    /// (GameScene과 NetworkGameManager가 각자 sceneLoaded를 구독하면 실행 순서가 보장되지 않아서
    /// 씬 로드 이벤트에 의존하지 않고 직접 호출받는 방식으로 처리)
    /// </summary>
    public void OnStageReady(StageScene stage)
    {
        // 서버/클라이언트 공통 — 각자 로컬 StageScene 참조(클라이언트도 RPC 수신 시 자기 EnemySpawner에 접근해야 함)
        _stageScene = stage;

        // 서버만 플레이어 스폰 대기 처리
        if (IsServer == false) return;

        _waitForSceneLoaded = true;
    }

    /// <summary>
    /// 모든 클라이언트가 씬 로드를 완료하면 호출
    /// </summary>
    private void OnLoadEventCompleted(
        string sceneName,               // 어떤 씬이 완료됐는지
        LoadSceneMode loadSceneMode,    // 로드 모드
        List<ulong> clientsCompleted,   // 완료한 클라이언트 ID 목록
        List<ulong> clientsTimedOut)    // 시간 초과로 완료 못한 클라이언트 ID 목록
    {
        // 서버만 처리
        if (IsServer == false) return;

        // Stage 준비를 기다리는 중이 아니면 무시
        if (_waitForSceneLoaded == false) return;

        // Stage 씬만 처리
        if (_stageScene == null || sceneName != _stageScene.gameObject.scene.name)
        {
            return;
        }

        _waitForSceneLoaded = false;

        SpawnAllPlayers();

        // 전원 씬 로드 완료 시점 — 로딩창 종료 및 적 스폰을 동시에 시작하라는 신호
        NotifyAllClientsReadyClientRpc();
    }

    /// <summary>
    /// 클라이언트 연결 종료 처리(서버 전용) — 다음 스테이지/스테이지 로드 준비 상태 정리 + 남은 탱크 정리
    /// 로딩화면 대기 중이든 게임 플레이 중이든 동일하게 호출됨(NetworkManager 레벨 콜백이라 시점에 상관없이 발동)
    /// </summary>
    void HandleClientDisconnect(ulong clientId)
    {
        Debug.Log($"[HandleClientDisconnect] clientId={clientId}, ShutdownInProgress={NetworkManager.Singleton.ShutdownInProgress}, IsListening={NetworkManager.Singleton.IsListening}, ConnectedClients={NetworkManager.Singleton.ConnectedClients.Count}");

        if (IsServer == false) return; // 서버만 처리
        if (NetworkManager.Singleton.ShutdownInProgress) return; // 호스트 자체 종료 중엔 무의미한 처리 — RPC 실패 방지

        _playerLives.Remove(clientId); // 전원 사망 판정 캐시 정리

        RemoveFromNextStageReady(clientId);
        RemoveFromStageLoaded(clientId);

        // 나간 클라이언트의 탱크 정리 — 콜백이 불리는 시점에 따라 프레임워크가 이미 정리했을 수도 있어 방어적으로 체크
        // (소유자 연결 종료 시 자동 파괴되는 게 기본 동작이지만, 정리가 누락되는 경우가 보고돼 있어 명시적으로 처리)
        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out NetworkClient client) &&
            client.PlayerObject != null &&
            client.PlayerObject.IsSpawned)
        {
            client.PlayerObject.Despawn(true);
        }

        NotifyPlayerLeftClientRpc((int)clientId); // 목숨 UI 슬롯 비활성화 신호(전원에게 전파)
        CheckAllPlayersDead(clientId); // 나간 클라이언트를 제외한 나머지 기준으로 전원사망 재판정(유일한 생존자였을 경우 대비)
    }

    /// <summary>
    /// 클라이언트 연결 종료를 전원에게 알림 — 목숨 UI에서 해당 슬롯을 비활성화하는 용도
    /// </summary>
    [ClientRpc]
    void NotifyPlayerLeftClientRpc(int playerIndex)
    {
        OnPlayerLeft?.Invoke(playerIndex);
    }

    /// <summary>
    /// 다음 스테이지 준비 목록에서 나간 클라이언트 정리 — 준비 완료 상태로 나가서 카운트가 부풀어 있으면 바로잡고,
    /// 나간 클라이언트가 유일한 미준비자였다면 남은 인원 기준으로 즉시 다음 스테이지 진행
    /// </summary>
    void RemoveFromNextStageReady(ulong clientId)
    {
        _readyForNextStageClientIds.Remove(clientId);

        int readyCount = _readyForNextStageClientIds.Count;
        int totalCount = GetConnectedCountExcluding(clientId);
        NotifyNextStageReadyCountClientRpc(readyCount, totalCount);

        if (totalCount > 0 && readyCount >= totalCount)
        {
            _readyForNextStageClientIds.Clear(); // 다음 스테이지 상점을 위해 초기화
            NotifyAllReadyForNextStageClientRpc();
        }
    }

    /// <summary>
    /// 스테이지 로드 완료 목록에서 나간 클라이언트 정리 — 위와 동일한 이유로 카운트를 바로잡음
    /// </summary>
    void RemoveFromStageLoaded(ulong clientId)
    {
        _stageLoadedClientIds.Remove(clientId);

        int loadedCount = _stageLoadedClientIds.Count;
        int totalCount = GetConnectedCountExcluding(clientId);

        if (totalCount > 0 && loadedCount >= totalCount)
        {
            _stageLoadedClientIds.Clear(); // 다음 전환을 위해 초기화
            NotifyAllClientsReadyClientRpc();
        }
    }

    /// <summary>
    /// 현재 접속 인원에서 특정 클라이언트를 제외한 총원 계산
    /// disconnect 콜백이 불리는 시점에 따라 ConnectedClientsIds가 나가는 클라이언트를 아직 포함하고 있을 수 있어 방어적으로 제외
    /// </summary>
    int GetConnectedCountExcluding(ulong excludedClientId)
    {
        int count = 0;
        foreach (ulong id in NetworkManager.Singleton.ConnectedClientsIds)
        {
            if (id == excludedClientId) continue;
            count++;
        }
        return count;
    }

    /// <summary>
    /// 스테이지 전환 씬 로드 완료 보고 — 각 클라이언트가 자기 쪽 다음 스테이지 로드를 마치면 호출(GameScene이 호출)
    /// 스테이지 전환은 일반 SceneManager로 로드되어 위 OnLoadEventCompleted(NGO 동기화 전용)가 발동하지 않으므로 별도 경로로 처리
    /// SpawnAllPlayers는 호출하지 않음 — 전환 시점엔 플레이어가 이미 스폰되어 있음
    /// </summary>
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void RequestStageLoadedServerRpc(RpcParams rpcParams = default)
    {
        ulong senderId = rpcParams.Receive.SenderClientId;
        _stageLoadedClientIds.Add(senderId);

        if (_stageLoadedClientIds.Count >= NetworkManager.Singleton.ConnectedClientsIds.Count)
        {
            _stageLoadedClientIds.Clear(); // 다음 전환을 위해 초기화
            NotifyAllClientsReadyClientRpc();
        }
    }

    /// <summary>
    /// 모든 클라이언트에게 스테이지 시작 신호 전달(호스트 포함 전원에게 전달됨)
    /// </summary>
    [ClientRpc]
    void NotifyAllClientsReadyClientRpc()
    {
        OnAllClientsReady?.Invoke();
    }

    /// <summary>
    /// 적 스폰 UI 동기화 — 서버가 적을 스폰할 때마다 호출(EnemySpawner가 호출)
    /// 스폰 리스트 자체(OnSpawnListReady)는 클라이언트도 로컬로 동일하게 계산 가능해서 이미 정상 동작하지만,
    /// 개별 스폰 시점(OnEnemySpawned, UI 아이콘 제거용)은 서버만 알 수 있어서 별도 전달이 필요함
    /// </summary>
    public void NotifyEnemySpawned(int spawnedIndex)
    {
        NotifyEnemySpawnedClientRpc(spawnedIndex);
    }

    [ClientRpc]
    void NotifyEnemySpawnedClientRpc(int spawnedIndex)
    {
        // 호스트 자신은 서버 로컬에서 EnemySpawner.OnEnemySpawned가 이미 직접 발행했으므로 중복 방지
        if (IsServer) return;

        if (_stageScene == null) return;
        _stageScene.EnemySpawner.ReceiveEnemySpawned(spawnedIndex);
    }

    /// <summary>
    /// HQ 파괴 동기화 — 서버가 판정을 마친 뒤 호출(HQ가 호출)
    /// </summary>
    public void NotifyHQDestroyed()
    {
        NotifyHQDestroyedClientRpc();
    }

    [ClientRpc]
    void NotifyHQDestroyedClientRpc()
    {
        // 호스트 자신은 서버 로컬에서 HQ.TakeHit()이 이미 직접 처리했으므로 중복 방지
        if (IsServer) return;

        if (_stageScene == null) return;
        _stageScene.HQ.TriggerDestruction();
    }

    /// <summary>
    /// 모든 적 격파 알림 동기화 — 서버가 전멸 판정을 마친 직후 호출(StageScene이 호출)
    /// 클리어 배너 표시용 — 3초 대기 후의 최종 결과(NotifyStageCleared)와는 별도로 즉시 전파
    /// </summary>
    public void NotifyAllEnemiesDefeated()
    {
        NotifyAllEnemiesDefeatedClientRpc();
    }

    [ClientRpc]
    void NotifyAllEnemiesDefeatedClientRpc()
    {
        // 호스트 자신은 서버 로컬에서 StageScene.HandleAllEnemiesDefeated가 이미 직접 처리했으므로 중복 방지
        if (IsServer) return;

        if (_stageScene == null) return;
        _stageScene.TriggerAllEnemiesDefeatedNotify();
    }

    /// <summary>
    /// 스테이지 클리어 동기화 — 서버가 클리어 판정(3초 대기 포함)을 마친 뒤 호출(StageScene이 호출)
    /// </summary>
    public void NotifyStageCleared()
    {
        NotifyStageClearedClientRpc();
    }

    [ClientRpc]
    void NotifyStageClearedClientRpc()
    {
        // 호스트 자신은 서버 로컬에서 StageClearRoutine이 이미 직접 처리했으므로 중복 방지
        if (IsServer) return;

        if (_stageScene == null) return;
        _stageScene.TriggerStageClear();
    }

    /// <summary>
    /// 포탄 폭발 범위 피해 동기화 — 서버가 폭발 판정을 마친 뒤 호출(Shell이 호출)
    /// 벽/큐브 파괴, 폭발 피해를 받는 대상(경전차 등)의 판정을 클라이언트에도 동일하게 재현시킴
    /// HitData의 AtkTank(MonoBehaviour 참조)는 RPC로 못 보내서 isPlayerAttack(bool)만 별도 전달
    /// hitNetworkObjectIds:서버가 실제로 맞혔다고 확인한 네트워크 오브젝트 id — 클라가 좌표+반경으로 직접 재판정하면
    /// NetworkTransform 보간 오차로 이동하는 대상(적 등)의 생사가 서버와 갈릴 수 있어서, 이 id들만 직접 지정해 처리함
    /// </summary>
    public void NotifyExplosionDamage(Vector3 position, float radius, int hitLayerValue, int damage, bool isPlayerAttack, ulong[] hitNetworkObjectIds)
    {
        NotifyExplosionDamageClientRpc(position, radius, hitLayerValue, damage, isPlayerAttack, hitNetworkObjectIds);
    }

    [ClientRpc]
    void NotifyExplosionDamageClientRpc(Vector3 position, float radius, int hitLayerValue, int damage, bool isPlayerAttack, ulong[] hitNetworkObjectIds)
    {
        // 호스트 자신은 서버 로컬에서 Shell.Explode()가 이미 직접 판정했으므로 중복 방지
        if (IsServer) return;

        HitData hitData = new HitData(damage, position, isPlayerAttack);
        LayerMask hitLayer = hitLayerValue;

        // 정적인(비네트워크) 파괴물만 로컬 판정으로 처리 — 네트워크 오브젝트는 아래에서 서버가 지정한 id로 직접 처리하므로 여기선 스킵
        Shell.ApplyExplosionDamage(position, radius, hitLayer, hitData, out _, skipNetworkObjects: true);

        // 서버가 실제로 맞혔다고 확인해준 네트워크 오브젝트에게만 정확히 적용(로컬 재판정 없이 그대로 신뢰)
        foreach (ulong id in hitNetworkObjectIds)
        {
            if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(id, out NetworkObject targetObject) == false) continue;
            if (targetObject.TryGetComponent(out IExplosionDamageable damageable) == false) continue;

            damageable.TakeHit(hitData, radius, position);
        }
    }

    /// <summary>
    /// 포탄 직격 데미지 동기화 — 서버가 직격 판정을 마친 뒤 호출(Shell이 호출)
    /// 폭발 판정과 달리 특정 대상 하나만 지정해야 해서, 위치 재탐색 대신 NetworkObjectId로 대상을 직접 지정함
    /// HitData의 AtkTank(MonoBehaviour 참조)는 RPC로 못 보내서 isPlayerAttack(bool)만 별도 전달
    /// </summary>
    public void NotifyDirectHitDamage(ulong targetNetworkObjectId, int damage, bool isPlayerAttack, Vector3 hitPosition)
    {
        NotifyDirectHitDamageClientRpc(targetNetworkObjectId, damage, isPlayerAttack, hitPosition);
    }

    [ClientRpc]
    void NotifyDirectHitDamageClientRpc(ulong targetNetworkObjectId, int damage, bool isPlayerAttack, Vector3 hitPosition)
    {
        // 호스트 자신은 서버 로컬에서 Shell.OnTriggerEnter()가 이미 직접 판정했으므로 중복 방지
        if (IsServer) return;

        // 대상이 이미 디스폰/파괴됐으면 무시(과거 신호가 뒤늦게 도착한 경우 방어)
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(targetNetworkObjectId, out NetworkObject targetObject) == false) return;

        // 실제로 맞은 부위와 무관하게 자식 HitZone을 다시 거치면 부위 보너스가 중복 적용되므로,
        // HitZone을 거치지 않고 탱크 본체(TankBase)를 직접 찾아 서버가 이미 계산한 최종 데미지를 그대로 적용
        if (targetObject.TryGetComponent(out TankBase targetTank) == false) return;

        HitData hitData = new HitData(damage, hitPosition, isPlayerAttack);
        targetTank.TakeHit(ref hitData);
    }

    /// <summary>
    /// 목숨 UI 동기화 — 각 클라이언트가 자기 로컬에서 NetworkVariable 변경을 감지해서 호출(PlayerNetworkOwner가 호출)
    /// NetworkVariable 자체가 이미 네트워크 동기화를 처리하므로 여기서는 로컬 이벤트 발행만 담당
    /// senderClientId(사망 판정용 안정적 키)와 playerIndex(UI 표시 슬롯)는 서로 다른 값이라 각각 받음
    /// </summary>
    public void NotifyLifeChanged(ulong senderClientId, int playerIndex, int life)
    {
        OnPlayerLifeChanged?.Invoke(playerIndex, life);

        // 전원 사망 판정:NetworkVariable 읽기 권한이 Everyone이라 호스트 로컬에도 전원의 값 변경이 모두 들어옴
        // 호스트만 판정해서 신호를 전파(각자 판정하면 도착 순서에 따라 클라이언트마다 판정 시점이 어긋날 수 있음)
        if (IsServer == false) return;

        _playerLives[senderClientId] = life;
        CheckAllPlayersDead();
    }

    /// <summary>
    /// 전원 사망 판정(호스트 전용) — 접속한 모든 클라이언트가 완전히 패배(EliminatedLife)했으면 신호 전파
    /// 목숨 0은 "마지막 목숨으로 아직 생존 중"인 상태라 여기 해당 안 됨 — 그 상태에서 한 번 더 죽어야 EliminatedLife로 내려감(PlayerNetworkOwner.MarkEliminated)
    /// excludeClientId:연결 종료 처리 중 호출된 경우, 콜백 시점에 따라 ConnectedClientsIds에 나가는 클라이언트가 아직 남아있을 수 있어 판정에서 제외하기 위함(기본값은 아무도 제외하지 않음)
    /// </summary>
    void CheckAllPlayersDead(ulong excludeClientId = ulong.MaxValue)
    {
        foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            if (clientId == excludeClientId) continue;

            // 아직 캐시에 없는(값을 한 번도 안 보낸) 클라이언트는 생존으로 간주해 판정 보류
            if (_playerLives.TryGetValue(clientId, out int life) == false) return;
            if (life != PlayerNetworkOwner.EliminatedLife) return;
        }

        NotifyAllPlayersDead();
    }

    /// <summary>
    /// 전원 사망 동기화 — 호스트 자신은 판정 즉시 로컬 처리, 비호스트에는 신호 전파
    /// </summary>
    void NotifyAllPlayersDead()
    {
        if (_allPlayersDeadNotified) return; // 이미 알린 상태면 중복 전파 방지

        _allPlayersDeadNotified = true;

        OnAllPlayersDead?.Invoke(); // 호스트 자신의 로컬 처리

        NotifyAllPlayersDeadClientRpc();
    }

    [ClientRpc]
    void NotifyAllPlayersDeadClientRpc()
    {
        // 호스트 자신은 판정 시점에 이미 로컬로 처리했으므로 중복 방지
        if (IsServer) return;

        OnAllPlayersDead?.Invoke();
    }

    /// <summary>
    /// 기지 무적 아이템 동기화 — 클라이언트가 아이템 사용 시 요청(GameScene이 호출)
    /// </summary>
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void RequestBaseShieldServerRpc(float duration)
    {
        if (_stageScene == null) return;

        _stageScene.BaseWall.ActivateShield(duration); // 서버(호스트) 자신의 로컬 적용
        OnBaseShieldActivated?.Invoke(duration);

        NotifyBaseShieldClientRpc(duration);
    }

    [ClientRpc]
    void NotifyBaseShieldClientRpc(float duration)
    {
        // 호스트 자신은 위에서 이미 직접 처리했으므로 중복 방지
        if (IsServer) return;

        if (_stageScene == null) return;
        _stageScene.BaseWall.ActivateShield(duration);
        OnBaseShieldActivated?.Invoke(duration);
    }

    /// <summary>
    /// 적 멈춤 아이템 동기화 — 클라이언트가 아이템 사용 시 요청(GameScene이 호출)
    /// AI 정지 판정은 서버 권위 이동이 NetworkTransform으로 그대로 반영되므로 별도 전파가 필요 없고,
    /// 이펙트/깜빡임 같은 시각 연출만 대상 목록과 함께 클라이언트에 전달함
    /// </summary>
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void RequestEMPFieldServerRpc(float duration)
    {
        if (_stageScene == null) return;

        _stageScene.EnemySpawner.StartEMPField(duration); // 서버 권위 판정 + 호스트 자신의 연출
        OnEMPFieldActivated?.Invoke(duration);

        ulong[] enemyIds = _stageScene.EnemySpawner.GetActiveEnemyNetworkObjectIds();
        NotifyEMPVisualClientRpc(enemyIds, duration, true); // 새 연출 시작
    }

    [ClientRpc]
    void NotifyEMPVisualClientRpc(ulong[] enemyNetworkObjectIds, float duration, bool isNewTrigger)
    {
        // 호스트 자신은 위에서 이미 직접 처리했으므로 중복 방지
        if (IsServer) return;

        if (_stageScene == null) return;

        // 신호를 보낸 시점에 생존해 있던 적들만 대상 — 판정 없이 이펙트/깜빡임 연출만 재생
        List<EnemyTank> targets = new List<EnemyTank>();
        foreach (ulong id in enemyNetworkObjectIds)
        {
            if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(id, out NetworkObject targetObject) == false) continue;
            if (targetObject.TryGetComponent(out EnemyTank enemyTank) == false) continue;

            targets.Add(enemyTank);
        }

        _stageScene.EnemySpawner.PlayEMPVisual(targets, duration, isNewTrigger);

        if (isNewTrigger) OnEMPFieldActivated?.Invoke(duration); // 늦게 합류한 적 알림(NotifyLateEMPVisual)에서는 재발행 안 함
    }

    /// <summary>
    /// EMP 지속 중 새로 스폰된 적의 시각 연출 동기화 — 서버가 호출(EnemySpawner가 호출)
    /// 대상이 하나뿐이라 별도 ClientRpc를 만들지 않고 기존 NotifyEMPVisualClientRpc를 재사용함
    /// </summary>
    public void NotifyLateEMPVisual(ulong enemyNetworkObjectId, float remainingDuration)
    {
        NotifyEMPVisualClientRpc(new ulong[] { enemyNetworkObjectId }, remainingDuration, false); // 기존 연출에 합류
    }

    /// <summary>
    /// 폭탄 아이템 동기화 — 클라이언트가 아이템 사용 시 요청(GameScene이 호출)
    /// </summary>
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void RequestAirSupportServerRpc()
    {
        if (_stageScene == null) return;

        OnAirSupportActivated?.Invoke(); // 호스트 자신의 로컬 처리(사용음 재생용)

        _stageScene.EnemySpawner.DestroyAllEnemies(); // 서버 권위 처리 + 호스트 자신의 로컬 연출(내부에서 대상 브로드캐스트까지 호출함)
    }

    /// <summary>
    /// 폭탄 처치 대상 동기화 — 서버가 처치를 마친 뒤 호출(EnemySpawner가 호출)
    /// TakeDamage 호출 자체는 전파되지 않아서, 각 클라이언트가 동일한 사망 연출(폭발/시체/디스폰 타이머)을
    /// 로컬로 재생하도록 대상 NetworkObjectId만 전달하고 클라이언트가 직접 TakeDamage를 재호출함
    /// </summary>
    public void NotifyAirSupportKill(ulong[] targetNetworkObjectIds)
    {
        NotifyAirSupportKillClientRpc(targetNetworkObjectIds);
    }

    [ClientRpc]
    void NotifyAirSupportKillClientRpc(ulong[] targetNetworkObjectIds)
    {
        // 호스트 자신은 서버 로컬에서 이미 직접 처리했으므로 중복 방지
        if (IsServer) return;

        OnAirSupportActivated?.Invoke(); // 비호스트 클라이언트의 로컬 처리(사용음 재생용)

        // 호스트 쪽과 동일하게 일괄 처치 마스크 적용(개별 3D 파괴음/드랍음이 겹쳐 커지는 것 방지)
        GameManager.Instance.AudioManager.StartMassKillMode();

        foreach (ulong id in targetNetworkObjectIds)
        {
            if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(id, out NetworkObject targetObject) == false) continue;
            if (targetObject.TryGetComponent(out TankModel tankModel) == false) continue;

            // AtkTank(공격 주체)를 특정할 수 없어 false로 전달 — HitData.AtkTank는 null로 처리되어 서버와 동일하게 아이템 격파로 집계됨
            tankModel.TakeDamage(new HitData(9999, targetObject.transform.position, false));
        }

        GameManager.Instance.AudioManager.EndMassKillMode();
    }

    /// <summary>
    /// 다음 스테이지 준비 요청 — 상점에서 나가기 버튼 클릭 시 각 클라이언트가 호출(ShopUI가 호출)
    /// </summary>
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void RequestNextStageReadyServerRpc(RpcParams rpcParams = default)
    {
        ulong senderId = rpcParams.Receive.SenderClientId;
        _readyForNextStageClientIds.Add(senderId);

        int readyCount = _readyForNextStageClientIds.Count;
        int totalCount = NetworkManager.Singleton.ConnectedClientsIds.Count;
        NotifyNextStageReadyCountClientRpc(readyCount, totalCount);

        if (readyCount >= totalCount)
        {
            _readyForNextStageClientIds.Clear(); // 다음 스테이지 상점을 위해 초기화
            NotifyAllReadyForNextStageClientRpc();
        }
    }

    /// <summary>
    /// 다음 스테이지 준비 인원 변경 동기화 — 전원(호스트 포함)에게 카운트 표시용으로 전달
    /// </summary>
    [ClientRpc]
    void NotifyNextStageReadyCountClientRpc(int readyCount, int totalCount)
    {
        OnNextStageReadyCountChanged?.Invoke(readyCount, totalCount);
    }

    /// <summary>
    /// 전원 준비 완료 동기화 — 전원(호스트 포함)에게 다음 스테이지 이동 신호 전달
    /// </summary>
    [ClientRpc]
    void NotifyAllReadyForNextStageClientRpc()
    {
        OnAllReadyForNextStage?.Invoke();
    }

    /// <summary>
    /// 로컬 상점 UI 열림/닫힘 알림(ShopUI가 호출) — 다른 플레이어 탱크를 로컬 화면에서만 숨기고 복원하기 위한 순수 로컬 신호
    /// 상점 UI는 클라이언트마다 독립적으로 열고 닫으므로 네트워크 전파가 필요 없음(NotifyLifeChanged와 동일한 패턴)
    /// </summary>
    public void NotifyShopActiveChanged(bool active)
    {
        OnShopActiveChanged?.Invoke(active);
    }

    /// <summary>
    /// 연결된 모든 클라이언트에 플레이어 스폰
    /// </summary>
    void SpawnAllPlayers()
    {
        int index = 0;
        foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            SpawnPlayer(clientId, index);
            index++;
        }
    }

    /// <summary>
    /// 해당 클라이언트에 플레이어 스폰
    /// </summary>
    void SpawnPlayer(ulong clientId, int spawnIndex)
    {
        if (_playerPrefab == null)
        {
            Debug.LogWarning("PlayerPrefab이 연결되지 않았습니다.");
            return;
        }

        Vector3 spawnPos = _stageScene.GetSpawnPoint(spawnIndex);
        GameObject player = Instantiate(_playerPrefab, spawnPos, Quaternion.identity);

        if (player.TryGetComponent(out NetworkObject networkObject))
        {
            networkObject.SpawnAsPlayerObject(clientId);
        }
    }

    /// <summary>
    /// 로컬 플레이어 스폰 완료 알림(PlayerNetworkOwner가 호출)
    /// </summary>
    public void NotifyLocalPlayerSpawned(PlayerTank player)
    {
        OnLocalPlayerSpawned?.Invoke(player);
    }

    /// <summary>
    /// 재도전 동기화 — 호스트가 재도전 버튼 클릭 시 호출(GameScene이 호출)
    /// </summary>
    public void NotifyRestart()
    {
        _playerLives.Clear(); // 전원 사망 판정 캐시 초기화(재도전으로 목숨이 복구되므로 이전 스테이지의 0 값이 남아있으면 안 됨)
        _allPlayersDeadNotified = false; // 재도전 시 다음 전원사망 판정이 다시 가능하도록 리셋

        NotifyRestartClientRpc();
    }

    [ClientRpc]
    void NotifyRestartClientRpc()
    {
        // 호스트 자신은 버튼 클릭 시 이미 로컬로 처리했으므로 중복 방지
        if (IsServer) return;

        OnRestartRequested?.Invoke();
    }

    /// <summary>
    /// 채팅 메시지 전송 요청 — 클라이언트가 채팅을 입력했을 때 호출(ChatUI가 호출)
    /// </summary>
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void RequestChatMessageServerRpc(string message, RpcParams rpcParams = default)
    {
        ulong senderId = rpcParams.Receive.SenderClientId;
        NotifyChatMessageClientRpc(senderId, message);
    }

    /// <summary>
    /// 채팅 메시지 수신 동기화 — 전원(호스트 포함)에게 발신자 clientId와 메시지 전달
    /// </summary>
    [ClientRpc]
    void NotifyChatMessageClientRpc(ulong senderClientId, string message)
    {
        OnChatMessageReceived?.Invoke(senderClientId, message);
    }
}