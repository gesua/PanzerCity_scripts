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

        // 릴레이 연결 전까지는 입력 비활성화 — 연결이 늦는 클라이언트가 타이핑해도 전송이 조용히 씹히는 문제 방지
        // (OnEnable이 여기보다 먼저 실행되므로, 호스트처럼 이미 연결된 경우엔 비활성화하지 않고 바로 포커스를 잡음)
        if (_relay == null)
        {
            _inputField.interactable = false;
        }
        else
        {
            _inputField.ActivateInputField();
        }
    }

    void OnEnable()
    {
        // 룸에서 나가는 3가지 경로 전부 구독 — OnDisable에서 나갈 때마다 해제되므로, 재진입할 때마다 여기서 다시 구독해야 함
        // (Start()는 오브젝트 생애주기에 한 번만 실행돼서 거기 두면 두 번째 룸부터 구독이 안 됨 — 실제로 이 문제였음)
        LobbyManager.Instance.OnKicked += HandleLeftRoom;
        LobbyManager.Instance.OnLeftLobby += HandleLeftRoom;
        LobbyManager.Instance.OnHostLeft += HandleLeftRoom;

        // 호스트는 방을 만드는 순간 이 UI가 아직 비활성이라 RoomChatRelay.OnNetworkSpawn 쪽의 바인딩 시도가 씹힘
        // (참가자는 이미 UI가 켜진 채로 들어와서 해당 없음) — 활성화 시점에 이미 스폰된 릴레이가 있는지 거꾸로 확인
        if (RoomChatRelay.Instance == null) return;
        if (_relay == RoomChatRelay.Instance) return; // 이미 연결돼 있으면 중복 바인딩(로그 초기화 재발생) 방지

        BindRelay(RoomChatRelay.Instance);
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
    /// 릴레이 참조도 같이 정리해야 함 — 안 그러면 다음 룸에 들어가서 아직 연결 전인데도 입력이 활성 상태로 남아있고,
    /// 그 상태로 전송을 시도하면 이미 사라진 이전 릴레이를 호출하게 됨
    /// </summary>
    void HandleLeftRoom()
    {
        _logText.text = string.Empty;

        if (_relay != null)
        {
            _relay.OnChatMessageReceived -= HandleMessageReceived;
            _relay = null;
        }

        _inputField.interactable = false; // 다음 룸의 릴레이가 연결될 때까지 다시 비활성 상태로
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

        _inputField.interactable = true; // 연결 완료 — 입력 가능하게 전환
        _inputField.ActivateInputField(); // 여태 비활성이라 못 잡았던 포커스를 여기서 잡음
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