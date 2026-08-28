using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 버튼 호버 시 매니저에게 활성화를 요청하고, 매니저의 지시에 따라 반짝임 파티클을 재생/정지하는 컴포넌트
/// 실제 재생/정지 여부는 이 컴포넌트가 아니라 TitleButtonSparkleManager가 결정함 (마지막 호버 버튼 유지 로직 때문)
/// </summary>
public class ButtonSparkleEffect : MonoBehaviour, IPointerEnterHandler
{
    [SerializeField] ParticleSystem _sparkleParticle; // 버튼 자식으로 배치된 반짝임 파티클
    [SerializeField] TitleButtonSparkleManager _manager; // 반짝임 활성 상태를 총괄하는 매니저

    /// <summary>
    /// 커서가 버튼 위로 진입했을 때 매니저에게 활성화 요청 (직접 재생하지 않음)
    /// </summary>
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_manager == null) return;

        _manager.RequestActivate(this);
    }

    /// <summary>
    /// 매니저가 이 버튼을 활성화할 때 호출 — 파티클 재생 시작
    /// </summary>
    public void Play()
    {
        if (_sparkleParticle == null) return;

        _sparkleParticle.Play();
    }

    /// <summary>
    /// 매니저가 다른 버튼으로 활성화를 넘길 때 호출 — 신규 방출만 정지 (이미 떠있는 파티클은 자연스럽게 소멸)
    /// </summary>
    public void Stop()
    {
        if (_sparkleParticle == null) return;

        _sparkleParticle.Stop(true, ParticleSystemStopBehavior.StopEmitting);
    }
}