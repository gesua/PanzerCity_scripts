using UnityEngine;

public class KillTutorial : TutorialStep
{
    [SerializeField] TankModel[] _dummyTanks;
    [SerializeField] GameObject _gate; // 문 오브젝트

    int _aliveCount;

    private void Awake()
    {
        _aliveCount = _dummyTanks.Length;

        for (int i = 0; i < _dummyTanks.Length; i++)
            _dummyTanks[i].OnDead += OnTankDead;
    }

    private void OnDisable()
    {
        // 구독 해제
        for (int i = 0; i < _dummyTanks.Length; i++)
        {
            if (_dummyTanks[i] != null)
                _dummyTanks[i].OnDead -= OnTankDead;
        }
    }

    void OnTankDead(HitData hitData)
    {
        _aliveCount--;

        if (_aliveCount <= 0)
        {
            _gate.SetActive(false);
            Complete();
        }
    }
}
