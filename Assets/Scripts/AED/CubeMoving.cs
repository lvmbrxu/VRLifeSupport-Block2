using System.Collections;
using UnityEngine;

public sealed class AedRunnerCube : MonoBehaviour
{
    [Header("Points")]
    [SerializeField] private Transform homePoint;
    [SerializeField] private Transform awayPoint;

    [Header("Timing")]
    [SerializeField] private float moveSpeed = 2.0f;
    [SerializeField] private float waitAwaySeconds = 8.0f;
    [SerializeField] private bool returnAfterWait = true;

    [Header("Optional: disable interaction while away")]
    [SerializeField] private Collider[] disableCollidersWhileAway;

    private Coroutine _routine;

    private void Awake()
    {
        if (homePoint == null)
        {
            // Create a home point at start position if not assigned
            GameObject hp = new GameObject($"{name}_HomePoint");
            hp.transform.position = transform.position;
            hp.transform.rotation = transform.rotation;
            homePoint = hp.transform;
        }
    }

    public void GoGetAed()
    {
        if (_routine != null) StopCoroutine(_routine);
        _routine = StartCoroutine(RunRoutine());
    }

    private IEnumerator RunRoutine()
    {
        SetCollidersEnabled(false);

        if (awayPoint != null)
            yield return MoveTo(awayPoint.position);

        if (returnAfterWait)
        {
            yield return new WaitForSeconds(waitAwaySeconds);

            if (homePoint != null)
                yield return MoveTo(homePoint.position);

            SetCollidersEnabled(true);
        }
    }

    private IEnumerator MoveTo(Vector3 target)
    {
        while ((transform.position - target).sqrMagnitude > 0.01f * 0.01f)
        {
            transform.position = Vector3.MoveTowards(transform.position, target, moveSpeed * Time.deltaTime);
            yield return null;
        }
    }

    private void SetCollidersEnabled(bool enabled)
    {
        if (disableCollidersWhileAway == null) return;
        for (int i = 0; i < disableCollidersWhileAway.Length; i++)
        {
            if (disableCollidersWhileAway[i] != null)
                disableCollidersWhileAway[i].enabled = enabled;
        }
    }
}
