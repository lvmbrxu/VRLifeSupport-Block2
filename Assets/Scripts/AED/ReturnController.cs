using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[DisallowMultipleComponent]
public sealed class AedRunnerReturnController : MonoBehaviour
{
    [Header("Scenario")]
    [SerializeField] private ScenarioDirector scenario;
    [SerializeField] private string startReturnFlag = "CrowdCleared";
    [SerializeField] private string aedArrivedFlag = "AedArrived";

    [Header("NPC")]
    [SerializeField] private GameObject kidRoot;          // NPC root to enable/disable
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private Animator animator;

    [Header("Points")]
    [SerializeField] private Transform returnSpawnPoint;  // where kid comes from
    [SerializeField] private Transform deliverPoint;      // where kid stops near you/victim

    [Header("Timing")]
    [SerializeField] private float minDelay = 2f;
    [SerializeField] private float maxDelay = 6f;
    [SerializeField] private float arriveDistance = 0.8f;

    [Header("Audio (optional)")]
    [SerializeField] private AudioSource voiceSource;
    [SerializeField] private AudioClip arrivedVoiceline;
    [SerializeField] private float voiceDelay = 0.2f;

    [Header("Debug")]
    [SerializeField] private bool log = true;

    private bool _started;

    private void Awake()
    {
        if (scenario == null) scenario = FindFirstObjectByType<ScenarioDirector>();
        if (kidRoot == null) kidRoot = gameObject;

        if (agent == null) agent = kidRoot.GetComponentInChildren<NavMeshAgent>(true);
        if (animator == null) animator = kidRoot.GetComponentInChildren<Animator>(true);

        // Start hidden until needed (optional)
        // kidRoot.SetActive(false);
    }

    private void OnEnable()
    {
        if (scenario != null) scenario.FlagsChanged += Evaluate;
        Evaluate();
    }

    private void OnDisable()
    {
        if (scenario != null) scenario.FlagsChanged -= Evaluate;
    }

    private void Evaluate()
    {
        if (_started) return;
        if (scenario == null) return;

        if (scenario.HasFlag(startReturnFlag))
        {
            _started = true;
            StartCoroutine(ReturnRoutine());
        }
    }

    private IEnumerator ReturnRoutine()
    {
        float delay = Random.Range(minDelay, maxDelay);
        if (log) Debug.Log($"[AED Runner] Returning in {delay:F1}s", this);
        yield return new WaitForSeconds(delay);

        if (kidRoot != null) kidRoot.SetActive(true);

        // Reset position to spawn
        if (returnSpawnPoint != null && kidRoot != null)
        {
            kidRoot.transform.SetPositionAndRotation(returnSpawnPoint.position, returnSpawnPoint.rotation);
        }

        if (agent != null && deliverPoint != null)
        {
            agent.isStopped = false;
            agent.ResetPath();
            agent.SetDestination(deliverPoint.position);

            SetWalking(true);

            while (agent.pathPending) yield return null;

            while (agent.remainingDistance > Mathf.Max(arriveDistance, agent.stoppingDistance + 0.05f))
                yield return null;

            agent.isStopped = true;
            SetWalking(false);
        }

        // Voiceline then raise flag
        if (voiceSource != null && arrivedVoiceline != null)
        {
            yield return new WaitForSeconds(voiceDelay);
            voiceSource.PlayOneShot(arrivedVoiceline);
            yield return new WaitForSeconds(arrivedVoiceline.length);
        }

        if (scenario != null)
            scenario.RaiseFlag(aedArrivedFlag);

        if (log) Debug.Log("[AED Runner] AED arrived flag raised", this);
    }

    private void SetWalking(bool walking)
    {
        if (animator == null) return;
        // Use whatever your animator expects:
        animator.SetBool("Walking", walking);
        // Or animator.Play("Walk"); etc.
    }
}
