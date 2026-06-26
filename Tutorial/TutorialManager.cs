using UnityEngine;

/// <summary>
/// 튜토리얼 총괄
/// </summary>
public class TutorialManager : MonoBehaviour
{
    [SerializeField] TutorialStep[] _steps;
    [SerializeField] TutorialHintUI _hintUI;

    int _currentIndex = 0;

    void Start()
    {
        StartStep(_currentIndex);
    }

    void StartStep(int index)
    {
        if (index >= _steps.Length)
        {
            //Debug.Log("튜토리얼 완료");
            return;
        }

        // 현재 스텝만 활성화, 나머지는 비활성화
        for (int i = 0; i < _steps.Length; i++)
        {
            _steps[i].gameObject.SetActive(i == index);
        }

        //Debug.Log($"튜토리얼 스텝 {index + 1} 시작");
        _steps[index].Init(OnStepComplete, _hintUI);
    }

    void OnStepComplete()
    {
        //Debug.Log($"튜토리얼 스텝 {_currentIndex + 1} 완료");
        _currentIndex++;
        StartStep(_currentIndex);
    }

    void ShowMessage(string msg)
    {
        // UI 매니저와 연결해서 메시지 출력
        Debug.Log(msg);
    }
}