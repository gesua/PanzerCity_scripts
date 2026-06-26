using UnityEngine;

public class MoveTutorial : TutorialStep
{
    bool _completed;

    private void OnTriggerEnter(Collider other)
    {
        if (_completed) return;

        if (other.CompareTag("PlayerHitZone"))
        {
            _completed = true;

            // 비활성화
            gameObject.SetActive(false);

            Complete();
        }
    }
}