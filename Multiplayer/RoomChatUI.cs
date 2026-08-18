using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 로비/룸 화면 채팅 UI — 입력창이 항상 포커스 상태로 유지됨(Enter로 열고 닫는 게임 화면 ChatUI와 다른 부분)
/// 전투 중이 아니라 별도로 조작을 뺏길 대상이 없으므로, 열고 닫기·페이드 없이 상시 표시함
/// 발신자 표시명(닉네임/색상/중복 구분)은 서버(RoomChatRelay)가 이미 완성해서 보내주므로 여기선 그대로 붙이기만 함
/// </summary>
public class RoomChatUI : MonoBehaviour
{
    public static RoomChatUI Instance { get; private set; }

    [SerializeField] TMP_InputField _inputField;
    [SerializeField] TMP_Text _logText;
    [SerializeField] ScrollRect _scrollRect;

    const int MaxMessageLength = 100;
    const float BottomScrollThreshold = 0.02f; // 이 값 이하면 "맨 아래(최신)를 보고 있다"고 판단

    RoomChatRelay _relay;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        _inputField.characterLimit = MaxMessageLength;
        _inputField.onSubmit.AddListener(HandleSubmit);
        _inputField.onEndEdit.AddListener(HandleEndEdit);

        _inputField.ActivateInputField(); // 항상 포커스 상태로 시작

        // 룸에서 나가는 3가지 경로 전부 구독 — 다음 룸에 들어갈 때(BindRelay)까지 기다리지 않고 나가는 즉시 이전 대화 이력 초기화
        LobbyManager.Instance.OnKicked += HandleLeftRoom;
        LobbyManager.Instance.OnLeftLobby += HandleLeftRoom;
        LobbyManager.Instance.OnHostLeft += HandleLeftRoom;
    }

    void OnDisable()
    {
        LobbyManager.Instance.OnKicked -= HandleLeftRoom;
        LobbyManager.Instance.OnLeftLobby -= HandleLeftRoom;
        LobbyManager.Instance.OnHostLeft -= HandleLeftRoom;

        if (_relay == null) return;

        _relay.OnChatMessageReceived -= HandleMessageReceived;
    }

    /// <summary>
    /// 룸에서 나가는 모든 경로(직접 나가기/호스트 퇴장/강퇴)에서 공통으로 호출 — 이전 룸의 대화 이력을 즉시 비움
    /// </summary>
    void HandleLeftRoom()
    {
        _logText.text = string.Empty;
    }

    /// <summary>
    /// RoomChatRelay가 스폰될 때 스스로 호출해서 연결함(런타임 동적 스폰이라 Inspector로 미리 연결 불가)
    /// 새 룸에 들어와 새 릴레이가 바인딩되는 것이므로, 이전 릴레이 구독을 정리하고 이전 룸의 대화 이력도 초기화함
    /// </summary>
    public void BindRelay(RoomChatRelay relay)
    {
        if (_relay != null)
        {
            _relay.OnChatMessageReceived -= HandleMessageReceived;
        }

        _relay = relay;
        _relay.OnChatMessageReceived += HandleMessageReceived;

        _logText.text = string.Empty;
    }

    /// <summary>
    /// Enter로 실제 제출된 경우에만 TMP_InputField가 호출함(자체 판정) — 클릭 이탈/ESC와 자연스럽게 구분됨
    /// </summary>
    void HandleSubmit(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return;
        if (_relay == null) return; // 아직 룸 채팅 릴레이가 스폰되기 전이면 무시(방어적 가드)

        _relay.RequestChatMessageServerRpc(text);
    }

    /// <summary>
    /// 입력창 편집 종료 처리 — 원인 무관(Enter 제출, ESC 취소, 다른 UI 클릭으로 포커스 이탈 전부 포함)
    /// 텍스트만 비우고 즉시 다시 포커스를 줘서 항상 입력 가능한 상태를 유지함
    /// </summary>
    void HandleEndEdit(string text)
    {
        _inputField.text = string.Empty;
        _inputField.ActivateInputField();
    }

    /// <summary>
    /// 채팅 메시지 수신(서버 릴레이) — 이미 완성된 한 줄을 그대로 로그에 추가
    /// </summary>
    void HandleMessageReceived(string formattedLine)
    {
        AppendLog(formattedLine);
    }

    void AppendLog(string line)
    {
        // 새 메시지가 오기 직전에 이미 맨 아래(최신)를 보고 있었는지 — 위로 스크롤해서 이전 채팅을 보는 중이면 건드리지 않기 위함
        bool wasAtBottom = (_scrollRect == null) || (_scrollRect.verticalNormalizedPosition <= BottomScrollThreshold);

        _logText.text = (string.IsNullOrEmpty(_logText.text)) ? line : $"{_logText.text}\n{line}";

        if (_scrollRect == null) return;
        if (wasAtBottom == false) return; // 스크롤을 올려 이전 채팅을 보는 중이면 새 메시지가 와도 위치 유지

        Canvas.ForceUpdateCanvases();
        _scrollRect.verticalNormalizedPosition = 0f; // 맨 아래로
    }
}