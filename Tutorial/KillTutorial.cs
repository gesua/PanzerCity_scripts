using System.Collections;
using UnityEngine;

public class KillTutorial : TutorialStep
{
    [SerializeField] DummyTank[] _dummyTanks;
    [SerializeField] GameObject _gate; // 열릴 오브젝트
    [SerializeField] float _hintDelay = 5f;    // 힌트 표시까지 대기 시간

    int _aliveCount;

    private void OnEnable()
    {
        _aliveCount = _dummyTanks.Length;

        for (int i = 0; i < _dummyTanks.Length; i++)
            _dummyTanks[i].OnDummyDead += OnTankDead;

        StartCoroutine(HintRoutine());
    }

    private void OnDisable()
    {
        // 구독 해제
        for (int i = 0; i < _dummyTanks.Length; i++)
        {
            if (_dummyTanks[i] != null)
                _dummyTanks[i].OnDummyDead -= OnTankDead;
        }
    }

    /// <summary>
    /// 일정 시간 후 더미 탱크의 화살표 활성화
    /// </summary>
    IEnumerator HintRoutine()
    {
        yield return new WaitForSeconds(_hintDelay);

        for (int i = 0; i < _dummyTanks.Length; i++)
        {
            if (_dummyTanks[i] != null)
                _dummyTanks[i].ShowArrow();
        }
    }

    void OnTankDead()
    {
        _aliveCount--;

        if (_aliveCount <= 0)
        {
            if (_gate != null) _gate.SetActive(false);
            Complete();
        }
    }
}