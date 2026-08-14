using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// 로비/룸 화면 채팅 UI — GameScene용 ChatUI와 인터랙션(Enter로 열고 닫기, 5초 페이드, 스크롤 위치 유지)은 동일하나
/// 발신자 표시명(닉네임/색상/중복 구분)은 서버(RoomChatRelay)가 이미 완성해서 보내주므로 여기선 그대로 붙이기만 함
/// InputSystemHandler(탱크 조작 차단)는 룸 단계엔 해당 사항 없으므로 참조하지 않음
/// </summary>
public class RoomChatUI : MonoBehaviour
{
    public static RoomChatUI Instance { get; private set; }

    [SerializeField] CanvasGroup _panelGroup;
    [SerializeField] TMP_InputField _inputField;
    [SerializeField] TMP_Text _logText;
    [SerializeField] ScrollRect _scrollRect;

    const int MaxMessageLength = 100;
    const float FadeOutDelay = 5f; // 마지막 활동 후 페이드가 시작되기까지 대기 시간
    const float FadeOutDuration = 1f; // 페이드아웃에 걸리는 시간
    const float BottomScrollThreshold = 0.02f; // 이 값 이하면 "맨 아래(최신)를 보고 있다"고 판단

    RoomChatRelay _relay;
    Coroutine _fadeRoutine;
    int _lastCloseFrame = -1; // 입력창을 닫은 바로 그 프레임에 Enter가 재감지되어 다시 열리는 것 방지

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        //_panelGroup.alpha = 0f;
        //_panelGroup.interactable = false;
        //_panelGroup.blocksRaycasts = false;

        _inputField.characterLimit = MaxMessageLength;
        _inputField.onSubmit.AddListener(HandleSubmit);
        _inputField.onEndEdit.AddListener(HandleEndEdit);
    }

    void OnDisable()
    {
        if (_relay == null) return;

        _relay.OnChatMessageReceived -= HandleMessageReceived;
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
        //RefreshActivity();
        _panelGroup.interactable = true;
        _panelGroup.blocksRaycasts = true;
        _inputField.ActivateInputField();
    }

    /// <summary>
    /// RoomChatRelay가 스폰될 때 스스로 호출해서 연결함(런타임 동적 스폰이라 Inspector로 미리 연결 불가)
    /// </summary>
    public void BindRelay(RoomChatRelay relay)
    {
        _relay = relay;
        _relay.OnChatMessageReceived += HandleMessageReceived;
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
    /// 입력창 편집 종료 처리 — 원인 무관(Enter 제출, ESC 취소, 클릭으로 포커스 이탈 전부 포함) 창 닫기/정리만 담당
    /// 전송 여부는 HandleSubmit이 이미 판단했으므로 여기선 텍스트 내용을 보지 않음
    /// </summary>
    void HandleEndEdit(string text)
    {
        _inputField.text = string.Empty;
        _inputField.DeactivateInputField();
        //_panelGroup.interactable = false;
        _panelGroup.blocksRaycasts = false;
        _lastCloseFrame = Time.frameCount;

        //RefreshActivity(); // 닫기 자체도 활동으로 간주해 페이드 타이머 리셋
    }

    /// <summary>
    /// 채팅 메시지 수신(서버 릴레이) — 이미 완성된 한 줄을 그대로 로그에 추가
    /// </summary>
    void HandleMessageReceived(string formattedLine)
    {
        AppendLog(formattedLine);
        //RefreshActivity();
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