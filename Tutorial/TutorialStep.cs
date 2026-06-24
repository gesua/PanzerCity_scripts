using System;
using UnityEngine;

public abstract class TutorialStep : MonoBehaviour
{
    Action _onComplete;

    // TutorialManager가 스텝을 시작할 때 호출
    public void Init(Action onComplete)
    {
        _onComplete = onComplete;
    }

    // 각 스텝 구현체가 조건 충족 시 호출
    protected void Complete()
    {
        _onComplete?.Invoke();
    }
}
