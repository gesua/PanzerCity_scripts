using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 로딩UI 관리
/// </summary>
public class LoadingUI : MonoBehaviour
{
    [SerializeField] Image _loadingBar;
    [SerializeField] TMP_Text _waitingText; // 로딩 글자(멀티에서 다른 사람 기다리는 중 변경 용도)

    float _fillTime = 0.75f; // 로딩바 채우는 속도(초)
    Coroutine _fillRoutine; // 진행 중인 채우기 코루틴

    public void Show()
    {
        // 재사용 시 초기화
        if (_fillRoutine != null)
        {
            StopCoroutine(_fillRoutine);
            _fillRoutine = null;
        }
        _loadingBar.fillAmount = 0f;
        _waitingText.text = "Loading...";

        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    /// <summary>
    /// 로딩바를 마저 채우고, 다른 클라이언트를 기다리는 중임을 알리는 문구를 표시
    /// 자체 코루틴으로 동작함(호출자가 yield로 기다릴 필요 없음) — Hide()로 오브젝트가 비활성화되면 Unity가 자동으로 정지시킴
    /// </summary>
    public void ShowWaitingForOthers()
    {
        //_fillRoutine = StartCoroutine(FillToFullAndShowWaitingRoutine());

        _waitingText.text = "Synchronizing Players...";
        _loadingBar.fillAmount = 0.9f;
    }

    IEnumerator FillToFullAndShowWaitingRoutine()
    {
        _waitingText.text = "Waiting for other players...";

        float currentProgress = _loadingBar.fillAmount;

        _loadingBar.fillAmount = 0.9f;

        while (currentProgress < 0.9f)
        {
            currentProgress = Mathf.MoveTowards(currentProgress, 0.9f, (0.9f / _fillTime) * Time.deltaTime);
            _loadingBar.fillAmount = currentProgress;
            yield return null;
        }

        _fillRoutine = null;
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

            yield return null;
        }

        // 로딩 완료
        while (_loadingBar.fillAmount < 0.9f)
        {
            currentProgress = Mathf.MoveTowards(currentProgress, 0.9f, (1f / _fillTime) * Time.deltaTime);

            if (Mathf.Abs(currentProgress - 0.9f) < 0.001f)
            {
                _loadingBar.fillAmount = 0.9f;
            }

            _loadingBar.fillAmount = currentProgress;
            yield return null;
        }
    }

    /// <summary>
    /// 여러 단계로 나뉜 로딩을 하나의 연속된 바로 이어서 표시할 때 사용
    /// op.progress(0~0.9)를 [rangeStart, rangeEnd] 구간에 매핑해서 채움(currentProgress를 fillAmount에서 이어받아 앞 구간과 끊기지 않게 함)
    /// </summary>
    public IEnumerator UpdateProgress(AsyncOperation op, float rangeStart, float rangeEnd)
    {
        float currentProgress = _loadingBar.fillAmount;

        while (op.progress < 0.9f)
        {
            float targetProgress = Mathf.Lerp(rangeStart, rangeEnd, op.progress / 0.9f);

            currentProgress = Mathf.MoveTowards(currentProgress, targetProgress, (1f / _fillTime) * Time.deltaTime);

            if (Mathf.Abs(currentProgress - targetProgress) < 0.001f)
            {
                currentProgress = targetProgress;
            }

            _loadingBar.fillAmount = currentProgress;

            yield return null;
        }

        // 이 구간의 로딩 완료
        while (_loadingBar.fillAmount < rangeEnd)
        {
            currentProgress = Mathf.MoveTowards(currentProgress, rangeEnd, (1f / _fillTime) * Time.deltaTime);

            if (Mathf.Abs(currentProgress - rangeEnd) < 0.001f)
            {
                _loadingBar.fillAmount = rangeEnd;
            }

            _loadingBar.fillAmount = currentProgress;
            yield return null;
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