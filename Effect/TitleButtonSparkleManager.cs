using UnityEngine;

/// <summary>
/// 타이틀 화면 버튼들의 반짝임 파티클 활성 상태를 총괄하는 매니저
/// 항상 하나의 버튼만 반짝이며, 새 버튼이 호버되면 이전 버튼은 꺼지고 새 버튼이 켜짐
/// 커서가 버튼 밖으로 나가도 별도 처리를 하지 않으므로, 마지막으로 호버된 버튼이 계속 반짝임 상태를 유지함
/// </summary>
public class TitleButtonSparkleManager : MonoBehaviour
{
    [SerializeField] Effect _logoSparkle; // 로고 파티클
    [SerializeField] ButtonSparkleEffect[] _sparkleButtons; // 순서대로 등록, 0번째가 타이틀 진입 시 기본 활성화되는 버튼

    ButtonSparkleEffect _currentActiveButton; // 현재 반짝이고 있는 버튼
    bool _isPaused; // 세이브 슬롯 UI 등 다른 패널이 열려 반짝임이 일시 정지된 상태인지 여부

    /// <summary>
    /// 타이틀 진입 시 배열의 첫 번째 버튼을 기본으로 활성화
    /// </summary>
    void Start()
    {
        if (_sparkleButtons.Length == 0) return;

        _currentActiveButton = _sparkleButtons[0];
        _currentActiveButton.Play();
    }

    /// <summary>
    /// 버튼이 호버되면 ButtonSparkleEffect가 호출 — 이전 활성 버튼을 끄고 새 버튼을 켬
    /// </summary>
    public void RequestActivate(ButtonSparkleEffect hoveredButton)
    {
        if (_isPaused) return; // 다른 패널에 의해 일시 정지된 상태에서는 호버 요청 무시

        if (hoveredButton == _currentActiveButton) return; // 이미 활성 상태인 버튼이면 무시

        if (_currentActiveButton != null) _currentActiveButton.Stop();

        _currentActiveButton = hoveredButton;
        _currentActiveButton.Play();
    }

    /// <summary>
    /// 세이브 슬롯 UI 등 다른 패널이 열릴 때 호출 — 현재 활성 버튼의 반짝임을 멈춤
    /// </summary>
    public void PauseAll()
    {
        _isPaused = true;

        if (_currentActiveButton != null) _currentActiveButton.Stop();
        _logoSparkle.Stop();
    }

    /// <summary>
    /// 다른 패널이 닫힐 때 호출 — 일시 정지 전 활성 상태였던 버튼을 다시 반짝이게 함
    /// </summary>
    public void ResumeAll()
    {
        _isPaused = false;

        if (_currentActiveButton != null) _currentActiveButton.Play();
        _logoSparkle.Play();
    }
}