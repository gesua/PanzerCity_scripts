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
    [SerializeField] RectTransform _arrowIcon; // 화살표 UI
    [SerializeField] float _duration = 2f; // 지속 시간

    Coroutine _routine;

    public void Show(Vector3 hitPoint)
    {
        if (_routine != null) StopCoroutine(_routine);
        _routine = StartCoroutine(ShowRoutine(hitPoint));
    }

    IEnumerator ShowRoutine(Vector3 hitPoint)
    {
        float elapsed = 0f;

        while (elapsed < _duration)
        {
            elapsed += Time.deltaTime;

            // 피격 방향 계산 (플레이어 기준)
            Vector3 dir = hitPoint - _player.position;
            dir.y = 0f;

            // 카메라 기준 각도로 변환
            float angle = Vector3.SignedAngle(_player.forward, dir, Vector3.up);

            // 화살표 회전
            _arrowIcon.localEulerAngles = new Vector3(0f, 0f, -angle);

            // 시간 지나면 서서히 사라지게
            float alpha = Mathf.Lerp(1f, 0f, elapsed / _duration);
            _arrowIcon.GetComponent<Image>().color = new Color(1f, 1f, 1f, alpha);

            yield return null;
        }

        _arrowIcon.gameObject.SetActive(false);
    }
}
