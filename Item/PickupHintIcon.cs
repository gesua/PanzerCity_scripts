using UnityEngine;

/// <summary>
/// 줍기 힌트 아이콘 — 모바일에서는 키 표시([F]) 대신 줍기 버튼과 같은 이미지로 교체
/// PC는 "어떤 키를 누르는지", 모바일은 "어떤 버튼을 누르는지"를 알려주는 아이콘이라 서로 다른 그림을 씀
/// </summary>
public class PickupHintIcon : MonoBehaviour
{
    [SerializeField] GameObject _pcIcon; // PC용 아이콘
    [SerializeField] GameObject _mobileIcon; // 모바일용 아이콘

    void Start()
    {
        if (MobileUIController.IsMobile == false) return;

        _pcIcon.SetActive(false);
        _mobileIcon.SetActive(true);
    }
}