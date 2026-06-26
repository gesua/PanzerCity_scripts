using System;
using UnityEngine;

public abstract class TutorialStep : MonoBehaviour
{
    [SerializeField] string _messageKey; // 로컬라이제이션 키 (UI_TUT_01)

    Action _onComplete;
    TutorialHintUI _hintUI;

    // TutorialManager가 스텝을 시작할 때 호출
    public void Init(Action onComplete, TutorialHintUI hintUI)
    {
        _onComplete = onComplete;
        _hintUI = hintUI;

        if (!string.IsNullOrEmpty(_messageKey))
        {
            Debug.Log($"{_messageKey} 메시지 출력");
            _hintUI.Show(_messageKey);
        }
    }

    // 각 스텝 구현체가 조건 충족 시 호출
    protected void Complete()
    {
        _hintUI?.Hide();
        _onComplete?.Invoke();
    }
}