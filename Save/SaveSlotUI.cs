using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// 세이브 슬롯 선택 화면
/// 빈 슬롯 선택 시 초기값으로 생성, 저장된 슬롯 선택 시 이어하기
/// </summary>
public class SaveSlotUI : MonoBehaviour
{
    [SerializeField] SaveSlotPanel[] _slots; // SaveManager의 슬롯 개수와 동일하게 인스펙터에서 설정
    [SerializeField] float _staggerDelay = 0.08f; // 슬롯이 하나씩 순차적으로 팝업되는 간격

    public event Action<int> OnSlotSelected; // 슬롯 선택됨(새 게임/이어하기 판단은 TitleScene에서)

    Coroutine _transitionRoutine; // 진행 중인 등장/퇴장 연출(중복 실행 방지용)

    void Awake()
    {
        for (int i = 0; i < _slots.Length; i++)
        {
            int slotIndex = i; // 클로저 캡처용 지역변수

            _slots[i].OnSelectClicked += () => HandleSlotClicked(slotIndex);
            _slots[i].OnDeleteClicked += () => HandleDeleteClicked(slotIndex);
            _slots[i].OnNameEndEdit += newName => HandleNameEndEdit(slotIndex, newName);
        }
    }

    /// <summary>
    /// 슬롯 선택 화면 표시 — 슬롯이 하나씩 순차적으로 통통 튀며 나타남
    /// </summary>
    public void Show()
    {
        gameObject.SetActive(true);
        RefreshAll();

        if (_transitionRoutine != null) StopCoroutine(_transitionRoutine);
        _transitionRoutine = StartCoroutine(ShowRoutine());
    }

    /// <summary>
    /// 슬롯 선택 화면 숨김 — 슬롯이 하나씩 순차적으로 오그라들며 사라진 뒤 비활성화
    /// </summary>
    public void Hide()
    {
        if (_transitionRoutine != null) StopCoroutine(_transitionRoutine);
        _transitionRoutine = StartCoroutine(HideRoutine());
    }

    /// <summary>
    /// 슬롯 전체 갱신
    /// </summary>
    void RefreshAll()
    {
        SaveManager saveManager = GameManager.Instance.SaveManager;

        for (int i = 0; i < _slots.Length; i++)
        {
            _slots[i].Refresh(saveManager.GetSlotData(i));
        }
    }

    /// <summary>
    /// 슬롯을 순서대로 하나씩 통통 튀며 나타나게 함
    /// </summary>
    IEnumerator ShowRoutine()
    {
        // 시작 전 전체를 먼저 숨김 상태로 세팅 — 아직 차례가 안 된 슬롯이 잠깐 보였다 사라지는 깜빡임 방지
        for (int i = 0; i < _slots.Length; i++)
        {
            _slots[i].SetHiddenImmediate();
        }

        for (int i = 0; i < _slots.Length; i++)
        {
            _slots[i].PopIn();
            yield return new WaitForSecondsRealtime(_staggerDelay);
        }

        _transitionRoutine = null;
    }

    /// <summary>
    /// 슬롯을 순서대로 하나씩 오그라들며 사라지게 한 뒤 화면 비활성화
    /// </summary>
    IEnumerator HideRoutine()
    {
        Coroutine lastPopOut = null;

        for (int i = 0; i < _slots.Length; i++)
        {
            lastPopOut = _slots[i].PopOut();
            yield return new WaitForSecondsRealtime(_staggerDelay);
        }

        // 마지막 슬롯의 애니메이션이 끝날 때까지 대기 후 비활성화
        if (lastPopOut != null) yield return lastPopOut;

        gameObject.SetActive(false);
        _transitionRoutine = null;
    }

    /// <summary>
    /// 슬롯 클릭 — 빈 슬롯이면 생성만 하고 끝, 저장된 슬롯이면 선택 이벤트 발행
    /// </summary>
    void HandleSlotClicked(int slotIndex)
    {
        SaveManager saveManager = GameManager.Instance.SaveManager;
        SaveData saveData = saveManager.GetSlotData(slotIndex);

        if (saveData.IsEmpty)
        {
            saveManager.CreateNewSlot(slotIndex);
            _slots[slotIndex].Refresh(saveManager.GetSlotData(slotIndex));
            _slots[slotIndex].OnClickNameEdit(); // 생성 직후 바로 이름 수정
            return;
        }

        OnSlotSelected?.Invoke(slotIndex);
    }

    /// <summary>
    /// 슬롯 삭제 버튼
    /// </summary>
    void HandleDeleteClicked(int slotIndex)
    {
        GameManager.Instance.SaveManager.DeleteSlot(slotIndex);
        _slots[slotIndex].Refresh(GameManager.Instance.SaveManager.GetSlotData(slotIndex));
    }

    /// <summary>
    /// 슬롯 이름 변경
    /// </summary>
    void HandleNameEndEdit(int slotIndex, string newName)
    {
        if (string.IsNullOrEmpty(newName)) return;

        GameManager.Instance.SaveManager.RenameSlot(slotIndex, newName);
    }
}