using System.Collections;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

/// <summary>
/// 멀티플레이 게임 화면 채팅 UI — Enter로 입력창 열고 닫기, 5초 무활동 시 페이드아웃
/// 싱글플레이에서는 NetworkGameManager가 존재하지 않으므로 자동 비활성화됨
/// </summary>
public class ChatUI : MonoBehaviour
{
    [SerializeField] CanvasGroup _panelGroup; // 로그 + 입력창 전체를 감싸는 페이드/입력 차단 대상
    [SerializeField] TMP_InputField _inputField;
    [SerializeField] TMP_Text _logText; // 대화 이력이 계속 쌓이는 텍스트
    [SerializeField] ScrollRect _scrollRect; // 선택:할당돼 있으면 새 줄마다 맨 아래로 자동 스크롤
    [SerializeField] InputSystemHandler _inputSystemHandler; // 채팅 입력 중 탱크 조작 차단용

    const int MaxMessageLength = 100;
    const float FadeOutDelay = 5f; // 마지막 활동 후 페이드가 시작되기까지 대기 시간
    const float FadeOutDuration = 1f; // 페이드아웃에 걸리는 시간

    Coroutine _fadeRoutine;
    int _lastCloseFrame = -1; // 입력창을 닫은 바로 그 프레임에 Enter가 재감지되어 다시 열리는 것 방지

    void Start()
    {
        // 싱글플레이는 NetworkGameManager가 아예 존재하지 않으므로 여기서 완전히 비활성화
        if (NetworkGameManager.Instance == null)
        {
            gameObject.SetActive(false);
            return;
        }

        _panelGroup.alpha = 0f;
        _panelGroup.interactable = false;
        _panelGroup.blocksRaycasts = false;

        _inputField.characterLimit = MaxMessageLength;
        _inputField.onSubmit.AddListener(HandleSubmit);
        _inputField.onEndEdit.AddListener(HandleEndEdit);

        NetworkGameManager.Instance.OnChatMessageReceived += HandleMessageReceived;
        // 목숨이 EliminatedLife가 되는 것만 걸러서 탈락 안내 메시지로 표시(전용 브로드캐스트 없이 기존 동기화 재사용)
        NetworkGameManager.Instance.OnPlayerLifeChanged += HandlePlayerEliminated;
    }

    void OnDisable()
    {
        if (NetworkGameManager.Instance == null) return;

        NetworkGameManager.Instance.OnChatMessageReceived -= HandleMessageReceived;
        NetworkGameManager.Instance.OnPlayerLifeChanged -= HandlePlayerEliminated;
    }

    void Update()
    {
        if (_inputField.isFocused) return; // 입력 중엔 onSubmit/onEndEdit 쪽에서 처리함
        if (Time.frameCount == _lastCloseFrame) return; // 방금 닫힌 바로 그 프레임엔 재오픈 방지
        if (IsEnterPressed() == false) return;

        OpenInput();
    }

    bool IsEnterPressed()
    {
        if (Keyboard.current == null) return false;

        return Keyboard.current[Key.Enter].wasPressedThisFrame || Keyboard.current[Key.NumpadEnter].wasPressedThisFrame;
    }

    /// <summary>
    /// 채팅 입력창 열기 — 패널을 즉시 보이게 하고 입력 포커스 이동
    /// </summary>
    void OpenInput()
    {
        RefreshActivity();
        _panelGroup.interactable = true;
        _panelGroup.blocksRaycasts = true;
        _inputField.ActivateInputField();
        _inputSystemHandler.SetInputDisabled(true); // 채팅 입력 중 탱크 조작 차단
    }

    /// <summary>
    /// Enter로 실제 제출된 경우에만 TMP_InputField가 호출함(자체 판정) — 좌클릭/우클릭으로 포커스를 잃거나
    /// ESC로 취소한 경우엔 호출되지 않으므로, 별도의 Enter 프레임 판정 없이도 정확히 전송 시점만 걸러짐
    /// (New Input System으로 Enter를 직접 재판별하면 한글 조합/키패드 Enter에서 프레임이 어긋나는 문제가 있었음)
    /// </summary>
    void HandleSubmit(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return;

        NetworkGameManager.Instance.RequestChatMessageServerRpc(text);
    }

    /// <summary>
    /// 입력창 편집 종료 처리 — 원인 무관(Enter 제출, ESC 취소, 클릭으로 포커스 이탈 전부 포함) 창 닫기/정리만 담당
    /// 전송 여부는 HandleSubmit이 이미 판단했으므로 여기선 텍스트 내용을 보지 않음
    /// </summary>
    void HandleEndEdit(string text)
    {
        _inputField.text = string.Empty;
        _inputField.DeactivateInputField();
        _panelGroup.interactable = false;
        _panelGroup.blocksRaycasts = false;
        _inputSystemHandler.SetInputDisabled(false); // 채팅 종료 — 탱크 조작 차단 해제
        _lastCloseFrame = Time.frameCount;

        RefreshActivity(); // 닫기 자체도 활동으로 간주해 페이드 타이머 리셋
    }

    /// <summary>
    /// 채팅 메시지 수신(서버 릴레이) — 발신자 닉네임을 조회해 색상과 함께 로그에 한 줄 추가
    /// </summary>
    void HandleMessageReceived(ulong senderClientId, string message)
    {
        TryGetOwner(senderClientId, out PlayerNetworkOwner senderOwner);
        int playerIndex = (senderOwner != null) ? senderOwner.PlayerIndex : -1;

        string nickname = ChatFormatUtility.SanitizeForRichText(ResolveNickname(senderClientId, senderOwner, playerIndex));
        string safeMessage = ChatFormatUtility.SanitizeForRichText(message);
        string color = ChatFormatUtility.ResolveColor(playerIndex);
        AppendLog($"<color={color}>{nickname}:{safeMessage}</color>");
        RefreshActivity();
    }

    /// <summary>
    /// 발신자 닉네임 조회 — 조회 실패 시 룸 자리 번호(#P)로 대체, 자리 조회마저 실패하면 clientId 기반으로 대체
    /// 다른 접속자와 닉네임이 겹치면 표시명 뒤에 (#P)를 붙여 구분함
    /// </summary>
    string ResolveNickname(ulong clientId, PlayerNetworkOwner owner, int playerIndex)
    {
        string fallback = (playerIndex >= 0) ? $"{playerIndex + 1}P" : $"{clientId + 1}P";
        string nickname = (owner != null) ? owner.Nickname : string.Empty;

        if (string.IsNullOrEmpty(nickname)) return fallback;

        return (HasDuplicateNickname(clientId, nickname)) ? $"{nickname}({fallback})" : nickname;
    }

    /// <summary>
    /// clientId로 접속 중인 플레이어의 PlayerNetworkOwner 조회 — 실패 시 false
    /// </summary>
    bool TryGetOwner(ulong clientId, out PlayerNetworkOwner owner)
    {
        owner = null;
        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out NetworkClient client) == false) return false;
        if (client.PlayerObject == null) return false;

        return client.PlayerObject.TryGetComponent(out owner);
    }

    /// <summary>
    /// 현재 접속 중인 다른 클라이언트 중 같은 닉네임이 있는지 확인
    /// </summary>
    bool HasDuplicateNickname(ulong clientId, string nickname)
    {
        foreach (ulong otherClientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            if (otherClientId == clientId) continue;

            TryGetOwner(otherClientId, out PlayerNetworkOwner otherOwner);
            string otherNickname = (otherOwner != null) ? otherOwner.Nickname : string.Empty;
            if (otherNickname == nickname) return true;
        }

        return false;
    }

    /// <summary>
    /// 목숨 변경 중 완전히 탈락(EliminatedLife)한 경우만 걸러서 채팅창에 안내 메시지 표시
    /// 전용 브로드캐스트 없이, 이미 전원에게 동기화되는 OnPlayerLifeChanged에 얹혀서 각자 로컬로 판단
    /// </summary>
    void HandlePlayerEliminated(int playerIndex, int life)
    {
        if (life != PlayerNetworkOwner.EliminatedLife) return;
        if (TryGetOwnerByPlayerIndex(playerIndex, out ulong clientId, out PlayerNetworkOwner owner) == false) return;

        string displayName = ChatFormatUtility.SanitizeForRichText(ResolveEliminatedDisplayName(clientId, owner, playerIndex));
        string message = LocalizationSettings.StringDatabase.GetLocalizedString("Localization", "UI_MP_MSG_PLAYER_OUT", arguments: new object[] { displayName });

        AppendLog(message);
        RefreshActivity();

        ShowSpectateHintIfEliminatedIsMe(clientId);
    }

    /// <summary>
    /// 탈락 안내 전용 표시명 조회 — 닉네임 중복 여부와 무관하게 항상 "닉네임(#P)" 형식으로 자리를 붙임
    /// (겹칠 때만 붙이는 일반 채팅 ResolveNickname과 달리, 탈락 알림은 매치 내내 봐온 탱크 색상/자리와
    /// 항상 바로 연결되는 게 유용해서 매번 붙임)
    /// </summary>
    string ResolveEliminatedDisplayName(ulong clientId, PlayerNetworkOwner owner, int playerIndex)
    {
        string fallback = (playerIndex >= 0) ? $"{playerIndex + 1}P" : $"{clientId + 1}P";
        string nickname = (owner != null) ? owner.Nickname : string.Empty;

        return (string.IsNullOrEmpty(nickname)) ? fallback : $"{nickname}({fallback})";
    }

    /// <summary>
    /// playerIndex로 해당 플레이어의 PlayerNetworkOwner를 역으로 조회(최대 4명이라 선형 탐색으로 충분함)
    /// </summary>
    bool TryGetOwnerByPlayerIndex(int playerIndex, out ulong clientId, out PlayerNetworkOwner owner)
    {
        foreach (NetworkClient client in NetworkManager.Singleton.ConnectedClientsList)
        {
            if (client.PlayerObject == null) continue;
            if (client.PlayerObject.TryGetComponent(out PlayerNetworkOwner candidateOwner) == false) continue;
            if (candidateOwner.PlayerIndex != playerIndex) continue;

            clientId = client.ClientId;
            owner = candidateOwner;
            return true;
        }

        clientId = 0;
        owner = null;
        return false;
    }

    /// <summary>
    /// 관전 대상 전환(Q/E) 안내는 방금 탈락한 그 플레이어 본인 화면에만 표시 — 탈락 순간이 실제로 관전을 시작하는
    /// 시점이라 그때 보여줌(3명 미만이면 전환할 대상 자체가 없으므로 생략)
    /// </summary>
    void ShowSpectateHintIfEliminatedIsMe(ulong eliminatedClientId)
    {
        if (eliminatedClientId != NetworkManager.Singleton.LocalClientId) return;
        if (NetworkManager.Singleton.ConnectedClientsIds.Count < 3) return;

        string message = LocalizationSettings.StringDatabase.GetLocalizedString("Localization", "UI_SPECTATE_CHANGE");

        AppendLog(message);
        RefreshActivity();
    }

    void AppendLog(string line)
    {
        _logText.text = (string.IsNullOrEmpty(_logText.text)) ? line : $"{_logText.text}\n{line}";

        if (_scrollRect == null) return;

        Canvas.ForceUpdateCanvases();
        _scrollRect.verticalNormalizedPosition = 0f; // 맨 아래로
    }

    /// <summary>
    /// 활동 기록 — 패널을 즉시 보이게 하고 페이드 타이머를 리셋
    /// </summary>
    void RefreshActivity()
    {
        if (_fadeRoutine != null)
        {
            StopCoroutine(_fadeRoutine);
            _fadeRoutine = null;
        }

        _panelGroup.alpha = 1f;
        _fadeRoutine = StartCoroutine(FadeOutAfterDelay());
    }

    IEnumerator FadeOutAfterDelay()
    {
        yield return new WaitForSeconds(FadeOutDelay);

        // 입력 중이면 페이드하지 않고 대기(타이핑 중 사라지는 것 방지)
        while (_inputField.isFocused)
        {
            yield return null;
        }

        float elapsed = 0f;
        float startAlpha = _panelGroup.alpha;
        while (elapsed < FadeOutDuration)
        {
            elapsed += Time.deltaTime;
            _panelGroup.alpha = Mathf.Lerp(startAlpha, 0f, elapsed / FadeOutDuration);
            yield return null;
        }

        _panelGroup.alpha = 0f;
        _fadeRoutine = null;
    }
}