using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 마우스 커서가 버튼 위에 올라가면 반짝임 파티클을 재생하는 컴포넌트
/// </summary>
public class ButtonSparkleEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] ParticleSystem _sparkleParticle; // 버튼 자식으로 배치된 반짝임 파티클 (에디터에서 직접 연결)

    /// <summary>
    /// 커서가 버튼 위로 진입했을 때 파티클 재생 시작
    /// </summary>
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_sparkleParticle == null) return;

        _sparkleParticle.Play();
    }

    /// <summary>
    /// 커서가 버튼에서 벗어났을 때 신규 방출만 정지 (이미 떠있는 파티클은 자연스럽게 소멸)
    /// </summary>
    public void OnPointerExit(PointerEventData eventData)
    {
        if (_sparkleParticle == null) return;

        _sparkleParticle.Stop(true, ParticleSystemStopBehavior.StopEmitting);
    }
}