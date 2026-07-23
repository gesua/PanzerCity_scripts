using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 피격 방향 표시기
/// 플레이어를 공격한 적의 방향을 알려주는 UI
/// </summary>
public class HitDirectionIndicator : MonoBehaviour
{
    [SerializeField] Transform _player;
    [SerializeField] float _duration = 3f; // 지속 시간
    [SerializeField] float _radius = 400f; // 중앙에서 화살표까지 거리

    string _iconPath = "UI/HitDirImg";

    private void Awake()
    {
        GameManager.Instance.PoolManager.GetPool(_iconPath);
    }

    /// <summary>
    /// 멀티플레이:씬의 UI를 프리팹이 미리 참조할 수 없어서, 로컬 플레이어 스폰 후 GameScene이 런타임에 주입
    /// </summary>
    public void SetPlayer(Transform player)
    {
        _player = player;
    }

    public void Show(HitData hitData)
    {
        if (hitData.AtkTank == null) return; // 공격자 참조를 알 수 없으면 방향을 계산할 수 없으므로 스킵(멀티플레이 역참조 실패 등)

        GameObject iconGo = GameManager.Instance.PoolManager.GetFromPool(_iconPath);
        iconGo.transform.SetParent(transform, false);

        if (iconGo.TryGetComponent(out RectTransform arrow))
        {
            StartCoroutine(ShowRoutine(hitData.AtkTank.transform, arrow));
        }
        else // 프리팹에 RectTransform이 없다면 에러를 방지하고 바로 풀로 돌려보냄
        {
            iconGo.DestroyOrReturnToPool();
        }
    }

    IEnumerator ShowRoutine(Transform atkTank, RectTransform arrow)
    {
        if (arrow.TryGetComponent(out Image image) == false)
        {
            arrow.gameObject.DestroyOrReturnToPool();
            yield break; // Image가 없으면 코루틴을 즉시 종료
        }

        arrow.gameObject.SetActive(true);
        arrow.localScale = Vector3.one * 0.3f; // 크기 초기화(Pool엔 Canvas가 없어서 Scale이 점점 커짐)
        float elapsed = 0f;
        Vector3 dir = Vector3.zero; // 피격 방향

        while (elapsed < _duration)
        {
            elapsed += Time.deltaTime;

            // 피격 방향 계산 (카메라 기준)
            if (atkTank != null)
            {
                dir = atkTank.position - _player.position;
                dir.y = 0f;
            }

            if (dir == Vector3.zero) break;

            // 카메라 기준 각도로 변환
            Vector3 camForward = Camera.main.transform.forward;
            camForward.y = 0f;
            float angle = Vector3.SignedAngle(camForward, dir, Vector3.up);

            // 화살표 위치를 원형으로 배치
            float rad = angle * Mathf.Deg2Rad;
            arrow.anchoredPosition = new Vector2(Mathf.Sin(rad) * _radius, Mathf.Cos(rad) * _radius);

            // 화살표 회전
            arrow.localEulerAngles = new Vector3(0f, 0f, -angle);

            // 시간 지나면 서서히 사라지게
            float alpha = Mathf.Lerp(1f, 0f, elapsed / _duration);

            image.color = new Color(1f, 1f, 1f, alpha);

            yield return null;
        }

        arrow.gameObject.DestroyOrReturnToPool();
    }
}