using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// 현재 게임 정보 UI
/// </summary>
public class GameInfoUI : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI _stageText; // 스테이지
    [SerializeField] TextMeshProUGUI _goldText; // 골드
    [SerializeField] float _goldRollDuration = 0.5f; // 골드 숫자가 바뀌는 시간
    [SerializeField] PlayerLifeSlot[] _lifeSlots; // 1P ~ 4P 목숨 슬롯(인덱스 = OwnerClientId, 싱글은 0번만 사용)

    int _currentDisplayGold = -1; // 현재 화면에 표시 중인 골드
    Coroutine _goldRollCoroutine;

    [System.Serializable]
    class PlayerLifeSlot
    {
        public GameObject Root;          // 슬롯 전체 켜고 끄기용
        public TextMeshProUGUI LifeText; // 목숨 숫자 표시
    }

    /// <summary>
    /// 스테이지 UI 세팅
    /// </summary>
    public void UpdateStage(int stage)
    {
        _stageText.text = stage.ToString();
    }

    /// <summary>
    /// 골드 UI 세팅
    /// </summary>
    public void UpdateGold(int gold)
    {
        // 초기화 전이거나 게임 오브젝트가 꺼져있을 때는 코루틴을 돌릴 수 없으므로 즉시 적용
        if (_currentDisplayGold == -1 || gameObject.activeInHierarchy == false)
        {
            _currentDisplayGold = gold;
            _goldText.text = gold.ToString();
            return;
        }

        // 이미 숫자가 굴러가고 있다면 중단하고 새 목표치로 다시 굴러가게 설정
        if (_goldRollCoroutine != null)
        {
            StopCoroutine(_goldRollCoroutine);
        }

        _goldRollCoroutine = StartCoroutine(RollGoldRoutine(gold));
    }

    IEnumerator RollGoldRoutine(int targetGold)
    {
        int startGold = _currentDisplayGold;
        float elapsed = 0f;

        while (elapsed < _goldRollDuration)
        {
            elapsed += Time.deltaTime;

            // Ease-Out 효과 (처음엔 빠르게, 끝으로 갈수록 천천히 카운트다운)
            float t = elapsed / _goldRollDuration;
            float easeT = 1f - Mathf.Pow(1f - t, 3f);

            _currentDisplayGold = Mathf.RoundToInt(Mathf.Lerp(startGold, targetGold, easeT));
            _goldText.text = _currentDisplayGold.ToString();

            yield return null;
        }

        // 연출 종료 시 최종 목표 값으로 정확히 확정
        _currentDisplayGold = targetGold;
        _goldText.text = _currentDisplayGold.ToString();
        _goldRollCoroutine = null;
    }

    /// <summary>
    /// 목숨 UI 세팅
    /// </summary>
    public void UpdateLife(int playerIndex, int life)
    {
        if (playerIndex < 0 || playerIndex >= _lifeSlots.Length) return;

        _lifeSlots[playerIndex].LifeText.text = life.ToString();
    }

    /// <summary>
    /// 멀티플레이:접속 인원 수만큼만 목숨 슬롯 활성화(나머지는 비활성)
    /// </summary>
    public void SetActivePlayerCount(int count)
    {
        for (int i = 0; i < _lifeSlots.Length; i++)
        {
            _lifeSlots[i].Root.SetActive(i < count);
        }
    }

    /// <summary>
    /// 멀티플레이:특정 플레이어의 목숨 슬롯 비활성화 — 게임 도중 해당 클라이언트가 연결 종료했을 때 사용
    /// </summary>
    public void HideLifeSlot(int playerIndex)
    {
        if (playerIndex < 0 || playerIndex >= _lifeSlots.Length) return;

        _lifeSlots[playerIndex].Root.SetActive(false);
    }

    /// <summary>
    /// 싱글플레이:멀티용 슬롯 크기가 작아서 빈 공간 대비 글자가 작아 보이므로 확대
    /// </summary>
    public void SetSingleplayerScale()
    {
        _lifeSlots[0].Root.transform.localScale = Vector3.one * 1.2f;
    }
}