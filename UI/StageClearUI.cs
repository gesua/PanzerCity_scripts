using System.Collections;
using UnityEngine;

/// <summary>
/// 스테이지 클리어 이미지 UI
/// </summary>
public class StageClearUI : MonoBehaviour
{
    [SerializeField] GameObject _stageClearPanel; // 전체 켜고 끌거
    [SerializeField] RectTransform _clearImage;
    [SerializeField] float _expandDuration = 0.5f;

    public IEnumerator Show()
    {
        _stageClearPanel.SetActive(true);
        _clearImage.localScale = new Vector3(0f, 1f, 1f);

        float elapsed = 0f;
        while (elapsed < _expandDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / _expandDuration);
            _clearImage.localScale = new Vector3(t, 1f, 1f);
            yield return null;
        }
        _clearImage.localScale = Vector3.one;
    }

    public void Hide()
    {
        _stageClearPanel.SetActive(false);
    }
}