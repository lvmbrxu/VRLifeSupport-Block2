using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public sealed class DogDistractionController : MonoBehaviour
{
    [Header("Scenario")]
    [SerializeField] private ScenarioDirector scenario;
    [SerializeField] private string startFlag = "StartedCPR";
    [SerializeField] private string stopFlag  = "MadeSpaceGesture";

    [Header("Dog")]
    [SerializeField] private GameObject dogRoot;      // root of the dog prefab in scene
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private Animator animator;
    [SerializeField] private AudioSource barkSource;

    [Header("Points")]
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private Transform approachPoint; // where dog comes to bark
    [SerializeField] private Transform exitPoint;     // where dog runs away

    [Header("Timing")]
    [SerializeField] private float minDelay = 5f;
    [SerializeField] private float maxDelay = 15f;

    [Header("Animator Params")]
    [Tooltip("Bool that controls Bark state. Walk->Bark when true, Bark->Walk when false.")]
    [SerializeField] private string barkBoolParam = "IsBarking";

    [Tooltip("Optional float parameter for blend trees (if you have it). If not used, leave empty.")]
    [SerializeField] private string speedFloatParam = "Speed";

    [Header("Movement Tuning")]
    [SerializeField] private float arriveDistance = 0.6f; // how close counts as arrived
    [SerializeField] private bool hideDogWhenExited = true;

    [Header("Debug")]
    [SerializeField] private bool log = true;
    
    [SerializeField] private string arrivedFlag = "DogArrived";
    private bool _arrivedRaised;


    private Coroutine routine;
    private bool started;
    private bool leaving;

    private void Awake()
    {
        if (dogRoot == null) dogRoot = gameObject;
        if (agent == null) agent = dogRoot.GetComponentInChildren<NavMeshAgent>(true);
        if (animator == null) animator = dogRoot.GetComponentInChildren<Animator>(true);
        if (barkSource == null) barkSource = dogRoot.GetComponentInChildren<AudioSource>(true);

        // start hidden/off until triggered
        if (dogRoot != null) dogRoot.SetActive(false);

        // Ensure bark audio isn't playing
        if (barkSource != null)
        {
            barkSource.loop = true;     // assume bark clip is a loop
            barkSource.playOnAwake = false;
            barkSource.Stop();
        }
    }

    private void OnEnable()
    {
        if (scenario == null) scenario = FindFirstObjectByType<ScenarioDirector>();
        if (scenario != null) scenario.FlagsChanged += Evaluate;
        Evaluate();
    }

    private void OnDisable()
    {
        if (scenario != null) scenario.FlagsChanged -= Evaluate;
    }

    private void Update()
    {
        // Drive optional animator speed param
        if (animator != null && !string.IsNullOrEmpty(speedFloatParam))
        {
            float spd = (agent != null && agent.enabled) ? agent.velocity.magnitude : 0f;
            animator.SetFloat(speedFloatParam, spd);
        }
    }

    private void Evaluate()
    {
        if (scenario == null) return;

        // Start sequence after CPR begins
        if (!started && scenario.HasFlag(startFlag))
        {
            started = true;
            if (routine != null) StopCoroutine(routine);
            routine = StartCoroutine(StartAfterDelay());
        }

        // Make dog leave after gesture
        if (started && !leaving && scenario.HasFlag(stopFlag))
        {
            leaving = true;
            if (routine != null) StopCoroutine(routine);
            routine = StartCoroutine(LeaveSequence());
        }
    }

    private IEnumerator StartAfterDelay()
    {
        float delay = Random.Range(minDelay, maxDelay);
        if (log) Debug.Log($"[Dog] Will come in {delay:F1}s", this);

        yield return new WaitForSeconds(delay);

        // Spawn / enable
        if (dogRoot != null) dogRoot.SetActive(true);

        if (spawnPoint != null)
        {
            dogRoot.transform.SetPositionAndRotation(spawnPoint.position, spawnPoint.rotation);
        }

        // Start walking toward approach point
        SetBarking(false);
        MoveTo(approachPoint);

        // Wait until arrival or until we get the stop flag
        while (!leaving && !HasArrived(approachPoint))
            yield return null;

        if (leaving) yield break;

        if (!_arrivedRaised && scenario != null)
        {
            _arrivedRaised = true;
            scenario.RaiseFlag(arrivedFlag);
        }

        // Arrived: start barking (anim + loop audio)
        SetBarking(true);


        // Stay barking until leaving becomes true (gesture flag)
        while (!leaving)
            yield return null;
    }

    private IEnumerator LeaveSequence()
    {
        if (log) Debug.Log("[Dog] Leaving", this);

        // Stop barking, start walking away
        SetBarking(false);
        MoveTo(exitPoint);

        while (!HasArrived(exitPoint))
            yield return null;

        if (hideDogWhenExited && dogRoot != null)
            dogRoot.SetActive(false);
    }

    private void MoveTo(Transform target)
    {
        if (agent == null || !agent.enabled || target == null) return;

        agent.isStopped = false;
        agent.SetDestination(target.position);
    }

    private bool HasArrived(Transform target)
    {
        if (agent == null || !agent.enabled || target == null) return true;
        if (agent.pathPending) return false;

        float stopping = Mathf.Max(arriveDistance, agent.stoppingDistance + 0.05f);
        return agent.remainingDistance <= stopping;
    }

    private void SetBarking(bool on)
    {
        if (animator != null && !string.IsNullOrEmpty(barkBoolParam))
            animator.SetBool(barkBoolParam, on);

        if (barkSource != null)
        {
            if (on)
            {
                if (!barkSource.isPlaying)
                    barkSource.Play();
            }
            else
            {
                if (barkSource.isPlaying)
                    barkSource.Stop();
            }
        }
    }
}
