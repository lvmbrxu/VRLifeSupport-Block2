using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[DisallowMultipleComponent]
public sealed class AedRunnerController : MonoBehaviour
{
    [Header("Scenario")]
    [SerializeField] private ScenarioDirector scenario;
    [Tooltip("Raised when the player points at this kid to request AED.")]
    [SerializeField] private string requestFlag = "RequestedAED";
    [Tooltip("Raised when kid arrives back and gives AED (used to spawn AED + cloth).")]
    [SerializeField] private string aedArrivedFlag = "AedArrived";

    [Header("NPC")]
    [SerializeField] private GameObject kidRoot;
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private Animator animator;

    [Header("Points")]
    [SerializeField] private Transform pickupPoint;   // run to AED
    [SerializeField] private Transform deliverPoint;  // run back to victim/player
    [SerializeField] private Transform optionalSpawnPoint; // optional reset position

    [Header("Timing")]
    [SerializeField, Min(0f)] private float pickupWaitSeconds = 1.0f;  // "grabs AED" moment
    [SerializeField, Min(0.1f)] private float arriveDistance = 0.8f;
    [SerializeField] private float minReturnDelay = 5f;   // comes back at some point
    [SerializeField] private float maxReturnDelay = 12f;

    [Header("Animation Params")]
    [SerializeField] private string isWalkingBool = "IsWalking";
    [SerializeField] private string giveTrigger = "GiveAED";

    [Header("Audio (optional)")]
    [SerializeField] private AudioSource voiceSource;
    [SerializeField] private AudioClip giveVoiceline;
    [SerializeField, Min(0f)] private float voiceDelay = 0.2f;

    [Header("Debug")]
    [SerializeField] private bool log = true;

    private bool _started;

    private void Awake()
    {
        if (scenario == null) scenario = FindFirstObjectByType<ScenarioDirector>();
        if (kidRoot == null) kidRoot = gameObject;

        if (agent == null) agent = kidRoot.GetComponentInChildren<NavMeshAgent>(true);
        if (animator == null) animator = kidRoot.GetComponentInChildren<Animator>(true);
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

        if (scenario.HasFlag(requestFlag))
        {
            _started = true;
            StartCoroutine(RunSequence());
        }
    }

    private IEnumerator RunSequence()
    {
        if (kidRoot != null) kidRoot.SetActive(true);

        if (agent == null || pickupPoint == null || deliverPoint == null)
        {
            Debug.LogError("[AED Runner] Missing agent or points.", this);
            yield break;
        }

        // 1) RUN TO PICKUP
        if (log) Debug.Log("[AED Runner] Running to pickup point", this);
        yield return RunTo(pickupPoint.position);

        // 2) WAIT AS IF PICKING UP AED
        if (pickupWaitSeconds > 0f)
            yield return new WaitForSeconds(pickupWaitSeconds);

        // 3) DELAY "COMES BACK AT SOME POINT"
        float backDelay = Random.Range(minReturnDelay, maxReturnDelay);
        if (log) Debug.Log($"[AED Runner] Returning in {backDelay:F1}s", this);
        yield return new WaitForSeconds(backDelay);

        // 4) RUN BACK TO DELIVER
        if (log) Debug.Log("[AED Runner] Running back to deliver point", this);
        yield return RunTo(deliverPoint.position);

        // 5) GIVE AED ANIM
        SetRunning(false);
        if (animator != null)
            animator.SetTrigger(giveTrigger);

        // voice line (optional)
        if (voiceSource != null && giveVoiceline != null)
        {
            yield return new WaitForSeconds(voiceDelay);
            voiceSource.PlayOneShot(giveVoiceline);
            yield return new WaitForSeconds(giveVoiceline.length);
        }

        // 6) RAISE FLAG -> SPAWN AED + CLOTH ETC
        if (scenario != null)
            scenario.RaiseFlag(aedArrivedFlag);

        if (log) Debug.Log("[AED Runner] AedArrived flag raised.", this);

        // finishes in Standing (animator transition will handle it)
    }

    private IEnumerator RunTo(Vector3 destination)
    {
        agent.isStopped = false;
        agent.ResetPath();
        agent.SetDestination(destination);

        SetRunning(true);

        while (agent.pathPending)
            yield return null;

        while (agent.enabled && agent.remainingDistance > Mathf.Max(arriveDistance, agent.stoppingDistance + 0.05f))
            yield return null;

        agent.isStopped = true;
        SetRunning(false);
    }

    private void SetRunning(bool running)
    {
        if (animator != null && !string.IsNullOrEmpty(isWalkingBool))
            animator.SetBool(isWalkingBool, running);
    }
}
