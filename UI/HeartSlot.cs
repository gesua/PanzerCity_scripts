using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// HP 하트 이미지 관리
/// </summary>
public class HeartSlot : MonoBehaviour
{
    [SerializeField] Image filled;
    [SerializeField] Image empty;

    public void SetState(bool isFilled)
    {
        filled.enabled = isFilled;
        empty.enabled = !isFilled;
    }
}