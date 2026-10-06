using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// 게임씬 Global Volume(SceneEffect가 쓰는 것과 동일한 프로필)의 ColorAdjustments를
/// OptionManager에 등록해서 밝기(감마) 옵션이 실제 화면에 반영되게 함
/// Global Volume 게임오브젝트에 직접 붙임(자기 자신의 Volume 컴포넌트를 그대로 사용)
/// SceneEffect와 마찬가지로 게임씬 로컬 오브젝트라 씬을 나가면 파괴되므로,
/// 파괴 시점에 OptionManager에 들고 있는 참조도 함께 해제해야 함(안 하면 다음 조작 시 예외 발생 위험)
/// </summary>
[RequireComponent(typeof(Volume))]
public class ScenePostProcessing : MonoBehaviour
{
    Volume _volume;
    ColorAdjustments _colorAdjustments;

    void Awake()
    {
        TryGetComponent(out _volume);

        if (_volume.profile.TryGet(out _colorAdjustments) == false)
        {
            Debug.LogWarning("[ScenePostProcessing] Volume 프로필에 ColorAdjustments override가 없습니다.");
            return;
        }

        GameManager.Instance.OptionManager.RegisterColorAdjustments(_colorAdjustments);
    }

    void OnDestroy()
    {
        // 씬 전환 등으로 GameManager가 먼저 사라진 상태(게임 종료 등)를 대비한 방어적 가드
        if (GameManager.Instance == null) return;
        if (_colorAdjustments == null) return;

        GameManager.Instance.OptionManager.UnregisterColorAdjustments(_colorAdjustments);
    }
}