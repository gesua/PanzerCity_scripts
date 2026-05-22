using System.Collections;
using UnityEngine;

/// <summary>
/// 경고 창
/// CanvasGroup 만들기 귀찮아서 그냥 HUD에 붙임
/// </summary>
public class WarningUI : MonoBehaviour
{
    [SerializeField] GameObject _warningUI;
    [SerializeField] float _displayDuration = 3f;

    bool _showWarning;
    float _timer; // 코루틴 쓰기 싫어서 만든 타이머

    public void Show()
    {
        _showWarning = true;
        _timer = 0;
        _warningUI.SetActive(true);
    }

    private void Update()
    {
        if (_showWarning == false) return;

        _timer += Time.deltaTime;
        if (_timer > _displayDuration)
        {
            _timer = 0;
            _showWarning = false;
            _warningUI.SetActive(false);
        }
    }
}
