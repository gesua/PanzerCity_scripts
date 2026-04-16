// TankDestructionEffect.cs
using UnityEngine;

/// <summary>
/// 적 탱크 파괴됐을 때 연출
/// 파괴된 모델로 교체 후 부품 다 날려버리기
/// </summary>
public class EnemyTankDestructionEffect : MonoBehaviour
{
    [SerializeField] GameObject _destroyedModel; // 파괴된 모델 프리팹
    [SerializeField] float _disappearDelay = 1f; // 사라지는 시간
    [SerializeField] float _explosionForce = 5f; // 폭발력

    public void Play()
    {
        // 현재 위치/회전값 그대로 파괴 모델 생성
        GameObject destroyed = Instantiate(_destroyedModel, transform.position, transform.rotation);

        // 자식 rigidbody 전부 날리기
        Rigidbody[] parts = destroyed.GetComponentsInChildren<Rigidbody>();
        foreach (Rigidbody part in parts)
        {
            Vector3 dir = (part.transform.position - transform.position).normalized;
            part.AddForce(dir * _explosionForce, ForceMode.Impulse);
        }

        Destroy(destroyed, _disappearDelay);
    }
}