using UnityEngine;

public class KillTutorial : TutorialStep
{
    [SerializeField] DummyTank[] _dummyTanks;
    [SerializeField] GameObject _gate; // 열릴 오브젝트

    int _aliveCount;

    private void OnEnable()
    {
        _aliveCount = _dummyTanks.Length;

        for (int i = 0; i < _dummyTanks.Length; i++)
            _dummyTanks[i].OnDummyDead += OnTankDead;
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