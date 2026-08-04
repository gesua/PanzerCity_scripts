using UnityEngine;

/// <summary>
/// 튜토리얼 총괄
/// </summary>
public class TutorialManager : MonoBehaviour
{
    [SerializeField] TutorialStep[] _steps;
    [SerializeField] TutorialHintUI _mainHintUI; // 진행도 관련 메시지
    [SerializeField] TutorialHintUI _tipUI; // 덜 중요한 팁

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
        _steps[index].Init(OnStepComplete, _mainHintUI, _tipUI);
    }

    void OnStepComplete()
    {
        //Debug.Log($"튜토리얼 스텝 {_currentIndex + 1} 완료");
        _currentIndex++;
        StartStep(_currentIndex);
    }
}