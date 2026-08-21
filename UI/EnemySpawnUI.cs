using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 오른쪽에 적 스폰 현황을 보여주는 UI
/// </summary>
public class EnemySpawnUI : MonoBehaviour
{
    [SerializeField] Image[] _enemyIcons;           // 적 아이콘
    [SerializeField] Sprite[] _tankSprites;         // TankID별 스프라이트 (순서 중요)

    const float PUNCH_SCALE = 1.2f;
    const float PUNCH_UP_DURATION = 0.05f;
    const float PUNCH_HOLD_DURATION = 0.02f;
    const float SHRINK_DURATION = 0.1f;

    RectTransform[] _iconRectTransforms;
    Coroutine _spawnAnimationRoutine;

    void Awake()
    {
        _iconRectTransforms = new RectTransform[_enemyIcons.Length];
        for (int i = 0; i < _enemyIcons.Length; i++)
        {
            if (_enemyIcons[i].TryGetComponent(out RectTransform rectTransform) == false)
            {
                Debug.LogWarning($"[EnemySpawnUI] RectTransform이 없습니다. (index:{i})");
                continue;
            }
            _iconRectTransforms[i] = rectTransform;
        }
    }

    /// <summary>
    /// 스테이지 시작 시 아이콘 전부 생성
    /// </summary>
    public void Initialize(List<int> spawnList)
    {
        for (int i = 0; i < spawnList.Count; i++)
        {
            _enemyIcons[i].enabled = true;
            _enemyIcons[i].sprite = GetSprite(spawnList[i]);

            if (_iconRectTransforms[i] != null)
            {
                _iconRectTransforms[i].localScale = Vector3.one;
            }
        }
    }

    /// <summary>
    /// 적 스폰시 해당 아이콘 사라짐
    /// </summary>
    public void SetEnemySpawn(int order)
    {
        if (order >= _enemyIcons.Length) return;

        if (_spawnAnimationRoutine != null)
        {
            StopCoroutine(_spawnAnimationRoutine);
        }

        _spawnAnimationRoutine = StartCoroutine(PlayDisappearAnimation(order));
    }

    /// <summary>
    /// 적 아이콘 사라짐 연출: 살짝 확대 → 유지 → 축소하며 사라짐
    /// </summary>
    IEnumerator PlayDisappearAnimation(int order)
    {
        RectTransform rectTransform = _iconRectTransforms[order];

        if (rectTransform == null)
        {
            _enemyIcons[order].enabled = false;
            _spawnAnimationRoutine = null;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < PUNCH_UP_DURATION)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / PUNCH_UP_DURATION);
            rectTransform.localScale = Vector3.one * Mathf.Lerp(1f, PUNCH_SCALE, t);
            yield return null;
        }
        rectTransform.localScale = Vector3.one * PUNCH_SCALE;

        yield return new WaitForSeconds(PUNCH_HOLD_DURATION);

        elapsed = 0f;
        while (elapsed < SHRINK_DURATION)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / SHRINK_DURATION);
            rectTransform.localScale = Vector3.one * Mathf.Lerp(PUNCH_SCALE, 0f, t);
            yield return null;
        }
        rectTransform.localScale = Vector3.zero;

        _enemyIcons[order].enabled = false;
        rectTransform.localScale = Vector3.one;

        _spawnAnimationRoutine = null;
    }

    /// <summary>
    /// tankID에 맞게 스프라이트 가져옴
    /// </summary>
    Sprite GetSprite(int tankID)
    {
        int id = tankID - 201; // TODO:보스는 301부터 시작이니까 바꿔야함

        if (id < 0 || id >= _tankSprites.Length)
        {
            Debug.LogWarning($"TankSprite 없음:{tankID}");
            return null;
        }

        return _tankSprites[id];
    }
}