using System.Collections;
using UnityEngine;

/// <summary>
/// 미니맵 물 아이콘
/// </summary>
public class WaterIcon : MonoBehaviour
{
    public SpriteRenderer _spriteRenderer;
    public Sprite[] _sprites;               

    void Start()
    {
        StartCoroutine(SwitchRoutine());
    }

    /// <summary>
    /// 1초마다 스프라이트 교체
    /// </summary>
    IEnumerator SwitchRoutine()
    {
        while (true)
        {
            for (int i = 0; i < _sprites.Length; i++)
            {
                _spriteRenderer.sprite = _sprites[i];
                yield return new WaitForSeconds(1f);
            }
        }
    }
}
