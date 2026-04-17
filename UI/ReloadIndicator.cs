using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 재장전 진행을 표시하는 UI 컴포넌트
/// </summary>
public class ReloadIndicator : MonoBehaviour
{
    [SerializeField] Image _baseImage;      // 진행될 때 뒷판 이미지
    [SerializeField] Image _fillImage;      // 진행 이미지
    [SerializeField] Image _completeImage;  // 장전 완료 이미지

    public void UpdateReload(float current, float max)
    {
        float ratio = current / max;
        _fillImage.fillAmount = ratio;
        _baseImage.fillAmount = 1 - ratio; // 뒷판 그냥 띄우면 이상하게 보임

        // 장전 완료되면 이미지 변경
        if (ratio >= 1f)
        {
            _fillImage.enabled = false;
            _baseImage.enabled = false;
            _completeImage.enabled = true;
        }
        else if (_completeImage.enabled) // 장전 중인데 완료 이미지가 켜져있으면 끔(1번만 들어오게 하는 용도)
        {
            _fillImage.enabled = true;
            _baseImage.enabled = true;
            _completeImage.enabled = false;
        }
    }
}