using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.UI;

/// <summary>
/// 세이브 슬롯 1개의 UI
/// </summary>
public class SaveSlotPanel : MonoBehaviour
{
    const string CreateKey = "UI_BTN_CREATE"; // 빈 슬롯
    const string StartKey = "UI_BTN_START_SLOT";   // 저장된 슬롯
    const string EmptySlotNameKey = "UI_SAVE_SLOT_NAME"; // 빈 슬롯 이름

    [SerializeField] Button _selectButton;
    [SerializeField] Button _nameEditButton;
    [SerializeField] Button _deleteButton;
    [SerializeField] GameObject _hideGroup; // 빈 슬롯일 때 꺼놓을 그룹
    [SerializeField] Image _selectButtonImage; // _selectButton의 Image
    [SerializeField] Sprite _emptySprite;      // 빈 슬롯용
    [SerializeField] Sprite _filledSprite;     // 저장된 슬롯용
    [SerializeField] TextMeshProUGUI _selectButtonText; // _selectButton의 글자("생성"/"시작")
    [SerializeField] TMP_InputField _nameInput;
    [SerializeField] TextMeshProUGUI _stageText;
    [SerializeField] TextMeshProUGUI _lifeText;
    [SerializeField] TextMeshProUGUI _playTimeText;

    [Header("----- 팝업 애니메이션 -----")]
    [SerializeField] CanvasGroup _canvasGroup;
    [SerializeField] float _popInDuration = 0.35f; // 통통 튀며 나타나는데 걸리는 시간
    [SerializeField] float _popOutDuration = 0.2f; // 오그라들며 사라지는데 걸리는 시간

    public event Action OnSelectClicked;
    public event Action OnDeleteClicked;
    public event Action<string> OnNameEndEdit;

    RectTransform _rectTransform; // 스케일 애니메이션 대상, _canvasGroup과 동일 오브젝트에서 조회
    Coroutine _popRoutine; // 진행 중인 팝업 애니메이션(중복 실행 방지용)

    void Awake()
    {
        _selectButton.onClick.AddListener(() => OnSelectClicked?.Invoke());
        _deleteButton.onClick.AddListener(() => OnDeleteClicked?.Invoke());

        if (_canvasGroup.TryGetComponent<RectTransform>(out _rectTransform) == false) return;
    }

    /// <summary>
    /// 이름 편집 버튼 — 입력 가능하게 풀고 포커스
    /// </summary>
    public void OnClickNameEdit()
    {
        _nameInput.interactable = true;
        _nameInput.ActivateInputField();
    }

    /// <summary>
    /// 이름 편집 종료(엔터 또는 포커스 벗어남) — 다시 잠금
    /// </summary>
    public void OnNameInputEndEdit(string newName)
    {
        string actualName = _nameInput.text; // 빈 문자 와서 인자 대신 직접 읽기
        StartCoroutine(LockNameInputNextFrame());

        OnNameEndEdit?.Invoke(actualName);
    }

    /// <summary>
    /// TMP_InputField가 OnDeselect 처리 중에 곧바로 interactable을 끄면 충돌하므로 한 프레임 늦춰서 잠금
    /// </summary>
    IEnumerator LockNameInputNextFrame()
    {
        yield return null;
        _nameInput.interactable = false;
    }

    /// <summary>
    /// 슬롯 UI 갱신
    /// </summary>
    public void Refresh(SaveData data)
    {
        bool isEmpty = data.IsEmpty;

        _hideGroup.SetActive(isEmpty == false);
        _selectButtonImage.sprite = (isEmpty) ? _emptySprite : _filledSprite;
        _nameInput.interactable = false; // 갱신될 때마다 편집 모드는 잠금 상태로

        // 생성, 시작 글자
        string key = (isEmpty) ? CreateKey : StartKey;
        _selectButtonText.text = new LocalizedString("Localization", key).GetLocalizedString();

        // 빈 슬롯 이름 채우기
        if (isEmpty)
        {
            _nameInput.SetTextWithoutNotify(new LocalizedString("Localization", EmptySlotNameKey).GetLocalizedString());
            return;
        }

        _nameInput.SetTextWithoutNotify(data.SaveName);
        _stageText.text = $"{data.CurrentStageID - 7100}";
        _lifeText.text = $"{data.Life}";
        _playTimeText.text = data.PlayTime.ToTimeString();
    }

    /// <summary>
    /// 슬롯을 즉시 숨김 상태(스케일 0, 알파 0, 클릭 불가)로 세팅
    /// 순차 팝업 시작 전, 아직 차례가 안 된 슬롯이 잠깐 보였다 사라지는 깜빡임을 막기 위해 사용
    /// </summary>
    public void SetHiddenImmediate()
    {
        if (_canvasGroup == null) return;
        if (_rectTransform == null) return;

        _canvasGroup.alpha = 0f;
        _canvasGroup.interactable = false;
        _canvasGroup.blocksRaycasts = false;
        _rectTransform.localScale = Vector3.zero;
    }

    /// <summary>
    /// 슬롯이 통통 튀며 나타나는 연출(스케일 오버슈트 + 페이드인) 시작
    /// 진행 중이던 애니메이션이 있으면 정지 후 재시작
    /// </summary>
    public Coroutine PopIn()
    {
        if (_canvasGroup == null) return null;
        if (_rectTransform == null) return null;

        if (_popRoutine != null) StopCoroutine(_popRoutine);
        _popRoutine = StartCoroutine(PopInRoutine());
        return _popRoutine;
    }

    /// <summary>
    /// 슬롯이 오그라들며 사라지는 연출(스케일 축소 + 페이드아웃) 시작
    /// 진행 중이던 애니메이션이 있으면 정지 후 재시작
    /// </summary>
    public Coroutine PopOut()
    {
        if (_canvasGroup == null) return null;
        if (_rectTransform == null) return null;

        if (_popRoutine != null) StopCoroutine(_popRoutine);
        _popRoutine = StartCoroutine(PopOutRoutine());
        return _popRoutine;
    }

    /// <summary>
    /// 스케일 0 → 1 Back-Ease-Out(오버슈트) + 알파 0 → 1 페이드인
    /// </summary>
    IEnumerator PopInRoutine()
    {
        _canvasGroup.alpha = 0f;
        _canvasGroup.interactable = false; // 애니메이션 도중 클릭 방지
        _canvasGroup.blocksRaycasts = false;
        _rectTransform.localScale = Vector3.zero;

        float elapsedTime = 0f;

        while (elapsedTime < _popInDuration)
        {
            elapsedTime += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsedTime / _popInDuration);

            // Back-Ease-Out 곡선 - 목표치를 살짝 넘었다가 되돌아오는 통통 튀는 느낌
            const float overshoot = 1.70158f;
            float shiftedT = t - 1f;
            float curve = shiftedT * shiftedT * ((overshoot + 1f) * shiftedT + overshoot) + 1f;

            _canvasGroup.alpha = Mathf.Clamp01(t * 2f); // 알파는 스케일보다 빠르게 채워서 등장 초반부터 잘 보이게
            _rectTransform.localScale = Vector3.one * curve;

            yield return null;
        }

        // 애니메이션 종료 후 확실하게 최종값 셋팅 및 클릭 활성화
        _canvasGroup.alpha = 1f;
        _canvasGroup.interactable = true;
        _canvasGroup.blocksRaycasts = true;
        _rectTransform.localScale = Vector3.one;
        _popRoutine = null;
    }

    /// <summary>
    /// 스케일 1 → 0 Ease-In(가속) + 알파 1 → 0 페이드아웃
    /// </summary>
    IEnumerator PopOutRoutine()
    {
        _canvasGroup.interactable = false; // 닫히기 시작하면 즉시 클릭 방지
        _canvasGroup.blocksRaycasts = false;

        Vector3 startScale = _rectTransform.localScale;
        float elapsedTime = 0f;

        while (elapsedTime < _popOutDuration)
        {
            elapsedTime += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsedTime / _popOutDuration);

            // Ease-In Cubic 곡선 - 점점 빨라지며 사라짐
            float curve = t * t * t;

            _canvasGroup.alpha = 1f - curve;
            _rectTransform.localScale = Vector3.Lerp(startScale, Vector3.zero, curve);

            yield return null;
        }

        _canvasGroup.alpha = 0f;
        _rectTransform.localScale = Vector3.zero;
        _popRoutine = null;
    }
}