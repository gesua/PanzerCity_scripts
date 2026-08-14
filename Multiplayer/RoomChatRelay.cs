using System;
using Unity.Netcode;

/// <summary>
/// 로비/룸 단계 채팅 릴레이 — Game_Multi 씬 로드 전에는 NetworkGameManager가 존재하지 않으므로 별도로 둠
/// LobbyManager.CreateLobbyAsync(StartHost 직후)에서 호스트가 수동으로 Spawn함
/// 클라이언트는 각자 로비 매핑(_clientIdToPlayerId)을 가질 수 없으므로, 서버가 닉네임/중복 구분/색상까지
/// 전부 붙인 완성된 한 줄을 만들어 브로드캐스트하는 방식(GameScene ChatUI와 달리 클라이언트가 직접 조회하지 않음)
/// </summary>
public class RoomChatRelay : NetworkBehaviour
{
    public event Action<string> OnChatMessageReceived; // 이미 발신자 표시명까지 합쳐진 완성 문자열

    public override void OnNetworkSpawn()
    {
        // RoomChatUI는 씬에 미리 배치돼있지만, 이 오브젝트는 런타임 동적 스폰이라 Inspector로 미리 연결할 수 없어
        // 스폰되는 시점에 스스로 찾아가서 연결함
        RoomChatUI.Instance?.BindRelay(this);
    }

    /// <summary>
    /// 채팅 메시지 전송 요청 — 클라이언트가 채팅을 입력했을 때 호출(RoomChatUI가 호출)
    /// </summary>
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void RequestChatMessageServerRpc(string message, RpcParams rpcParams = default)
    {
        ulong senderId = rpcParams.Receive.SenderClientId;
        string displayName = ResolveDisplayName(senderId);
        string safeMessage = ChatFormatUtility.SanitizeForRichText(message);
        string color = ChatFormatUtility.ResolveColor(senderId);

        NotifyChatMessageClientRpc($"<color={color}>{displayName}:{safeMessage}</color>");
    }

    /// <summary>
    /// 발신자 표시명 조회 — 다른 접속자와 닉네임이 겹치면 P#를 붙여 구분(GameScene ChatUI와 동일한 방식)
    /// </summary>
    string ResolveDisplayName(ulong clientId)
    {
        string fallback = $"{clientId + 1}P";
        string nickname = ChatFormatUtility.SanitizeForRichText(LobbyManager.Instance.GetNicknameByClientId(clientId));

        if (string.IsNullOrEmpty(nickname)) return fallback;

        return (HasDuplicateNickname(clientId, nickname)) ? $"{nickname}({fallback})" : nickname;
    }

    /// <summary>
    /// 현재 접속 중인 다른 클라이언트 중 같은 닉네임이 있는지 확인
    /// </summary>
    bool HasDuplicateNickname(ulong clientId, string nickname)
    {
        foreach (ulong otherClientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            if (otherClientId == clientId) continue;

            string otherNickname = ChatFormatUtility.SanitizeForRichText(LobbyManager.Instance.GetNicknameByClientId(otherClientId));
            if (otherNickname == nickname) return true;
        }

        return false;
    }

    /// <summary>
    /// 채팅 메시지 수신 동기화 — 전원(호스트 포함)에게 완성된 한 줄 전달
    /// </summary>
    [ClientRpc]
    void NotifyChatMessageClientRpc(string formattedLine)
    {
        OnChatMessageReceived?.Invoke(formattedLine);
    }
}