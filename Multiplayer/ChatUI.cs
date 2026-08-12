using System.Collections;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
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
        _inputField.onEndEdit.AddListener(HandleEndEdit);

        NetworkGameManager.Instance.OnChatMessageReceived += HandleMessageReceived;
    }

    void OnDisable()
    {
        if (NetworkGameManager.Instance == null) return;

        NetworkGameManager.Instance.OnChatMessageReceived -= HandleMessageReceived;
    }

    void Update()
    {
        if (_inputField.isFocused) return; // 입력 중엔 onEndEdit 쪽에서 텍스트 유무만으로 전송 여부를 판단함
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
    /// 입력창 편집 종료 처리 — 내용이 있으면 전송, 빈 문자열이면 창만 닫음
    /// Enter 여부는 별도로 판별하지 않음:한글 조합 중 Enter나 키패드 Enter는 TMP_InputField 내부 처리 시점과
    /// New Input System의 wasPressedThisFrame 프레임이 어긋나 감지가 누락될 수 있어, 텍스트 유무만으로 판단함
    /// (클릭 등으로 창을 벗어나 편집이 끝난 경우에도 남아있던 내용은 그대로 전송됨)
    /// </summary>
    void HandleEndEdit(string text)
    {
        bool shouldSend = string.IsNullOrWhiteSpace(text) == false;

        _inputField.text = string.Empty;

        if (shouldSend)
        {
            NetworkGameManager.Instance.RequestChatMessageServerRpc(text);
        }

        _inputField.DeactivateInputField();
        _panelGroup.interactable = false;
        _panelGroup.blocksRaycasts = false;
        _inputSystemHandler.SetInputDisabled(false); // 채팅 종료 — 탱크 조작 차단 해제
        _lastCloseFrame = Time.frameCount;

        RefreshActivity(); // 전송/닫기 자체도 활동으로 간주해 페이드 타이머 리셋
    }

    /// <summary>
    /// 채팅 메시지 수신(서버 릴레이) — 발신자 닉네임을 조회해 로그에 한 줄 추가
    /// </summary>
    void HandleMessageReceived(ulong senderClientId, string message)
    {
        string nickname = ResolveNickname(senderClientId);
        AppendLog($"{nickname}:{message}");
        RefreshActivity();
    }

    /// <summary>
    /// clientId로 접속 중인 플레이어의 닉네임 조회 — 조회 실패 시 P1~P4로 대체 표시
    /// </summary>
    string ResolveNickname(ulong clientId)
    {
        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out NetworkClient client) == false)
        {
            return $"P{clientId + 1}";
        }

        if (client.PlayerObject == null) return $"P{clientId + 1}";
        if (client.PlayerObject.TryGetComponent(out PlayerNetworkOwner owner) == false) return $"P{clientId + 1}";

        string nickname = owner.Nickname;
        return (string.IsNullOrEmpty(nickname)) ? $"P{clientId + 1}" : nickname;
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