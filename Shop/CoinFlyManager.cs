using System.Collections;
using UnityEngine;

/// <summary>
/// 상점에서 물건 구입 시 동전이 날아가는 UI 연출을 담당
/// </summary>
public class CoinFlyManager : MonoBehaviour
{
    [Header("----- 컴포넌트 -----")]
    [SerializeField] GameObject _coinPrefab; // UI 동전 프리팹
    [SerializeField] Transform _coinContainer; // 동전들이 생성될 부모 컨테이너

    [Header("----- 런타임 데이터 -----")]
    [SerializeField] float _flyDuration = 0.6f; // 날아가는 시간
    [SerializeField] int _pricePerCoin = 100; // 동전 1개당 가격

    /// <summary>
    /// 동전 날아가는 연출을 실행합니다.
    /// </summary>
    /// <param name="startWorldPos">출발 위치 (월드 좌표)</param>
    /// <param name="endWorldPos">도착 위치 (월드 좌표)</param>
    /// <param name="price">구입 가격</param>
    public void PlayEffect(Vector3 startWorldPos, Vector3 endWorldPos, int price)
    {
        // 가격에 비례하여 생성할 동전 개수 산정
        //int coinCount = Mathf.Clamp(price / _pricePerCoin, _minCoins, _maxCoins);
        int coinCount = price / _pricePerCoin;
        StartCoroutine(SpawnCoinsRoutine(startWorldPos, endWorldPos, coinCount));
    }

    IEnumerator SpawnCoinsRoutine(Vector3 startPos, Vector3 endPos, int count)
    {
        for (int i = 0; i < count; i++)
        {
            // 동전 생성 (최적화가 필요하다면 추후 GameManager의 PoolManager를 활용하세요)
            GameObject coin = Instantiate(_coinPrefab, _coinContainer);
            coin.transform.position = startPos;

            // 각각의 동전이 날아가는 코루틴 실행
            StartCoroutine(FlyCoinRoutine(coin.transform, startPos, endPos));

            // 동전이 한 번에 날아가지 않고 차르륵 날아가도록 짧은 딜레이 추가
            yield return new WaitForSeconds(0.05f);
        }
    }

    IEnumerator FlyCoinRoutine(Transform coinTr, Vector3 startPos, Vector3 endPos)
    {
        float elapsed = 0f;

        // 베지어 곡선을 위한 제어점(Control Point) 생성 (위로 퍼졌다가 모이는 포물선 효과)
        float randomX = Random.Range(-150f, 150f);
        float randomY = Random.Range(50f, 200f);
        Vector3 controlPoint = startPos + (endPos - startPos) / 2f + new Vector3(randomX, randomY, 0f);

        Vector3 initialScale = Vector3.one * 0.5f; // 처음엔 살짝 작게
        Vector3 targetScale = Vector3.one;

        while (elapsed < _flyDuration)
        {
            if (coinTr == null) yield break; // 비행 도중 씬 전환 등으로 파괴되었을 경우 방어

            elapsed += Time.deltaTime;
            float t = elapsed / _flyDuration;

            // Ease-Out 효과 (처음엔 빠르게 퍼지고 나중에 부드럽게 모임)
            float easeT = 1f - Mathf.Pow(1f - t, 3f);

            // 2차 베지어 곡선 계산
            Vector3 m1 = Vector3.Lerp(startPos, controlPoint, easeT);
            Vector3 m2 = Vector3.Lerp(controlPoint, endPos, easeT);
            coinTr.position = Vector3.Lerp(m1, m2, easeT);

            // 스케일 연출
            coinTr.localScale = Vector3.Lerp(initialScale, targetScale, easeT);

            yield return null;
        }

        // 도착 후 동전 파괴
        if (coinTr != null)
        {
            // TODO:여기서 '짤랑' 사운드 재생
            Destroy(coinTr.gameObject);
        }
    }
}