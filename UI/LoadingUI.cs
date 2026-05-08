using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 로딩UI 관리
/// </summary>
public class LoadingUI : MonoBehaviour
{
    [SerializeField] Canvas _canvas;
    [SerializeField] Image _loadingBar;

    public void Show()
    {
        _canvas.enabled = true;
    }

    public void Hide()
    {
        _canvas.enabled = false;
    }

    public IEnumerator UpdateProgress(AsyncOperation op)
    {
        while (op.progress < 0.9f)
        {
            _loadingBar.fillAmount = op.progress; // 0.9까지만 올라감(이미지로도 0.9까지가 좋음)
            yield return null;
        }
        _loadingBar.fillAmount = 0.9f;
    }

    public IEnumerator UpdateProgress(AsyncOperation op1, AsyncOperation op2)
    {
        while (op1.progress < 0.9f || op2.progress < 0.9f)
        {
            _loadingBar.fillAmount = (op1.progress + op2.progress) / 2f;
            yield return null;
        }
        _loadingBar.fillAmount = 0.9f;
    }
}
