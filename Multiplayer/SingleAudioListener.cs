using UnityEngine;

/// <summary>
/// 씬 전환 중 남아있는 로비 AudioListener 정리
/// (클라이언트는 로비 폴링 지연으로 이전 씬 리스너가 늦게 꺼지는 타이밍 문제가 있어 추가)
/// </summary>
public class SingleAudioListener : MonoBehaviour
{
    void Awake()
    {
        if (LobbyManager.Instance.PendingLobbyAudioListener == null) return;

        LobbyManager.Instance.PendingLobbyAudioListener.enabled = false;
        LobbyManager.Instance.PendingLobbyAudioListener = null;
    }
}