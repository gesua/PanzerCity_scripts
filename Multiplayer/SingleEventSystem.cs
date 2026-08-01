using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 씬 전환 중 남아있는 로비 EventSystem 정리
/// (클라이언트는 Game_Multi 씬 로드가 이미 시작된 뒤에 로비 EventSystem이 꺼지는 타이밍이라 겹치는 경우가 있어 추가)
/// </summary>
public class SingleEventSystem : MonoBehaviour
{
    void Awake()
    {
        Debug.Log($"[SingleEventSystem] Awake, Pending={LobbyManager.Instance.PendingLobbyEventSystem}");
        if (LobbyManager.Instance.PendingLobbyEventSystem == null) return;

        LobbyManager.Instance.PendingLobbyEventSystem.gameObject.SetActive(false);
        LobbyManager.Instance.PendingLobbyEventSystem = null;
    }
}