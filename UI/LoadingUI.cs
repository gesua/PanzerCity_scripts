using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 로딩UI 관리
/// </summary>
public class LoadingUI : MonoBehaviour
{
    [SerializeField] Image _loadingBar;
    float _fillTime = 0.75f; // 로딩바 채우는 속도(초)

    public void Show()
    {
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    public IEnumerator UpdateProgress(AsyncOperation op)
    {
        float currentProgress = 0f;

        // 0.9까지만 올라감(이미지로도 0.9까지가 좋음)
        while (op.progress < 0.9f)
        {
            currentProgress = Mathf.MoveTowards(currentProgress, op.progress, (1f / _fillTime) * Time.deltaTime);

            if (Mathf.Abs(currentProgress - op.progress) < 0.001f)
            {
                currentProgress = op.progress;
            }

            _loadingBar.fillAmount = currentProgress;

            //_loadingBar.fillAmount = op.progress; 
            yield return null;
        }

        // 로딩 완료
        currentProgress = Mathf.MoveTowards(currentProgress, 0.9f, (1f / _fillTime) * Time.deltaTime);

        if (Mathf.Abs(currentProgress - 0.9f) < 0.001f)
        {
            _loadingBar.fillAmount = 0.9f;
        }
    }

    public IEnumerator UpdateProgress(AsyncOperation op1, AsyncOperation op2)
    {
        float currentProgress = 0f;

        /*
        Debug.Log($"로딩 : {op1.progress}, {op2.progress}");

        float test = 0;
        while (test < 1)
        {
            _loadingBar.fillAmount = test;
            test += 0.1f;
            yield return null;
        }
        */

        while (op1.progress < 0.9f || op2.progress < 0.9f)
        {
            float targetProgress = (op1.progress + op2.progress) / 2f; // 0.9f까지만 가게함

            currentProgress = Mathf.MoveTowards(currentProgress, targetProgress, (1f / _fillTime) * Time.deltaTime);

            //Debug.Log($"로딩 : {op1.progress}, {op2.progress}, {currentProgress}");

            if (Mathf.Abs(currentProgress - targetProgress) < 0.001f)
            {
                currentProgress = targetProgress;
            }

            _loadingBar.fillAmount = currentProgress;

            yield return null;
        }

        // 로딩 완료
        while (_loadingBar.fillAmount < 0.9f)
        {
            currentProgress = Mathf.MoveTowards(currentProgress, 0.9f, (1f / _fillTime) * Time.deltaTime);

            //Debug.Log($"로딩 : {op1.progress}, {op2.progress}, {currentProgress}");

            if (Mathf.Abs(currentProgress - 0.9f) < 0.001f)
            {
                _loadingBar.fillAmount = 0.9f;
            }

            _loadingBar.fillAmount = currentProgress;
            yield return null;
        }
    }
}
