using System;
using UnityEngine;

public abstract class TutorialStep : MonoBehaviour
{
    [SerializeField] string _messageKey; // 진행도 메시지 키
    [SerializeField] string _tipKey;     // 팁 메시지 키

    Action _onComplete;
    TutorialHintUI _mainHintUI;
    TutorialHintUI _tipUI;

    // TutorialManager가 스텝을 시작할 때 호출
    public void Init(Action onComplete, TutorialHintUI mainHintUI, TutorialHintUI tipUI)
    {
        _onComplete = onComplete;
        _mainHintUI = mainHintUI;
        _tipUI = tipUI;

        if (string.IsNullOrEmpty(_messageKey) == false)
            _mainHintUI.Show(_messageKey);

        if (string.IsNullOrEmpty(_tipKey) == false)
            _tipUI.Show(_tipKey);
    }

    // 각 스텝 구현체가 조건 충족 시 호출
    protected void Complete()
    {
        _mainHintUI?.Hide();
        _tipUI?.Hide();
        _onComplete?.Invoke();
    }
}