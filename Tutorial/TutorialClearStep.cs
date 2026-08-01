using System.Collections;
using UnityEngine;

/// <summary>
/// 튜토리얼 클리어 스텝
/// 스테이지 클리어 연출 및 상점 오픈
/// 상점에서 별도 메시지/팁 표시
/// </summary>
public class TutorialClearStep : TutorialStep
{
    [SerializeField] string _shopMessageKey;  // 상점 진행도 메시지 키
    [SerializeField] string _shopTipKey;      // 상점 팁 메시지 키
    [SerializeField] float _clearDelay = 5f;  // 클리어 연출까지 대기 시간
    [SerializeField] float _shopDelay = 3f;   // 클리어 연출 후 상점 오픈까지 대기 시간

    private void OnEnable()
    {
        StartCoroutine(ClearRoutine());
    }

    IEnumerator ClearRoutine()
    {
        yield return new WaitForSeconds(_clearDelay);

        GameScene gameScene = FindAnyObjectByType<GameScene>();
        if (gameScene == null) yield break;

        // 클리어 연출
        gameScene.ShowTutorialClearEffect();

        yield return new WaitForSeconds(_shopDelay);

        // 상점 오픈
        gameScene.TutorialClear();

        // _tipUI Y값 이동
        if (_tipUI != null)
        {
            if (_tipUI.TryGetComponent(out RectTransform tipRect))
            {
                if (tipRect != null)
                    tipRect.anchoredPosition += new Vector2(0f, 400f);
            }
        }

        // 상점 메시지로 갱신
        if (!string.IsNullOrEmpty(_shopMessageKey))
            _mainHintUI?.Show(_shopMessageKey);

        if (!string.IsNullOrEmpty(_shopTipKey))
            _tipUI?.Show(_shopTipKey);
    }
}
