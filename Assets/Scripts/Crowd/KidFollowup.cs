using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[DisallowMultipleComponent]
public sealed class KidFollowUpController : MonoBehaviour
{
    [Header("Scenario")]
    [SerializeField] private ScenarioDirector scenario;

    [Tooltip("When this flag is raised, we spawn the kid and make them come in.")]
    [SerializeField] private string startFlag = "CrowdCleared"; // change if you want

    [Tooltip("Raised when the kid has arrived at the target point.")]
    [SerializeField] private string kidArrivedFlag = "KidArrived";

    [Tooltip("When this flag is raised (by pointing at the female again), the kid leaves.")]
    [SerializeField] private string kidHandledFlag = "KidHandled";

    [Header("Start Delay (NEW)")]
    [Tooltip("Random delay after startFlag before the kid spawns/moves in.")]
    [SerializeField] private float minStartDelay = 2f;

    [Tooltip("Random delay after startFlag before the kid spawns/moves in.")]
    [SerializeField] private float maxStartDelay = 6f;

    [Header("Kid NPC")]
    [Tooltip("Kid root object (can be disabled at start).")]
    [SerializeField] private GameObject kidRoot;

    [Tooltip("If null, we auto-find in children.")]
    [SerializeField] private NavMeshAgent agent;

    [Tooltip("If null, we auto-find in children.")]
    [SerializeField] private Animator animator;

    [Tooltip("If null, we auto-find in children.")]
    [SerializeField] private AudioSource voiceSource;

    [Header("Points")]
    [Tooltip("Where kid starts when spawned (optional). If null, uses current position.")]
    [SerializeField] private Transform spawnPoint;

    [Tooltip("Kid runs to this point and stands there.")]
    [SerializeField] private Transform arrivePoint;

    [Tooltip("Kid leaves to this point.")]
    [SerializeField] private Transform exitPoint;

    [Header("Movement")]
    [SerializeField] private float arriveDistance = 0.7f;
    [SerializeField] private bool warpToNavMeshIfNeeded = true;
    [SerializeField] private bool disableKidAfterExit = true;

    [Header("Animator (Optional)")]
    [Tooltip("Bool param used to switch to running animation. Leave empty to not control animator.")]
    [SerializeField] private string isRunningBoolParam = "IsRunning";

    [Header("Voicelines")]
    [Tooltip("Plays these after the kid arrives (in order, no overlap).")]
    [SerializeField] private List<AudioClip> arrivalLines = new List<AudioClip>();

    [Tooltip("Plays one of these when the kid leaves.")]
    [SerializeField] private List<AudioClip> leavingLines = new List<AudioClip>();

    [Tooltip("Delay before starting arrival VO after reaching arrive point.")]
    [SerializeField] private float arrivalVoiceDelay = 0.2f;

    [Tooltip("Delay before leaving movement starts after leave VO begins.")]
    [SerializeField] private float leaveMoveDelay = 0.2f;

    [Header("Debug")]
    [SerializeField] private bool log = false;

    private bool _started;
    private bool _handled;
    private Coroutine _routine;

    // Animator param existence cache (avoid spam)
    private bool _checkedAnim;
    private bool _hasIsRunningParam;

    private void Awake()
    {
        if (scenario == null)
            scenario = FindFirstObjectByType<ScenarioDirector>();

        if (kidRoot == null)
        {
            Debug.LogError("[KidFollowUpController] kidRoot is not assigned.", this);
            enabled = false;
            return;
        }

        if (agent == null)
            agent = kidRoot.GetComponentInChildren<NavMeshAgent>(true);

        if (animator == null)
            animator = kidRoot.GetComponentInChildren<Animator>(true);

        if (voiceSource == null)
            voiceSource = kidRoot.GetComponentInChildren<AudioSource>(true);

        if (voiceSource != null)
            voiceSource.playOnAwake = false;

        // Start disabled (recommended)
        kidRoot.SetActive(false);
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
        if (scenario == null) return;

        if (!_started && scenario.HasFlag(startFlag))
        {
            _started = true;
            if (_routine != null) StopCoroutine(_routine);
            _routine = StartCoroutine(ArriveRoutine());
        }

        if (_started && !_handled && scenario.HasFlag(kidHandledFlag))
        {
            _handled = true;
            if (_routine != null) StopCoroutine(_routine);
            _routine = StartCoroutine(LeaveRoutine());
        }
    }

    private IEnumerator ArriveRoutine()
    {
        // NEW: random delay before kid appears
        float delay = Random.Range(minStartDelay, maxStartDelay);
        if (log) Debug.Log($"[Kid] Will enter in {delay:F1}s", this);
        if (delay > 0f) yield return new WaitForSeconds(delay);

        kidRoot.SetActive(true);

        // place at spawn
        if (spawnPoint != null)
            kidRoot.transform.SetPositionAndRotation(spawnPoint.position, spawnPoint.rotation);

        EnsureOnNavMesh();

        // move in
        SetRunning(true);
        MoveTo(arrivePoint);

        while (!_handled && !HasArrived(arrivePoint))
            yield return null;

        if (_handled) yield break;

        SetRunning(false);

        if (scenario != null && !string.IsNullOrWhiteSpace(kidArrivedFlag))
            scenario.RaiseFlag(kidArrivedFlag);

        if (arrivalVoiceDelay > 0f)
            yield return new WaitForSeconds(arrivalVoiceDelay);

        // play arrival lines sequentially (no overlap)
        yield return PlaySequential(voiceSource, arrivalLines);
    }

    private IEnumerator LeaveRoutine()
    {
        // play one leave line (optional)
        if (voiceSource != null && leavingLines != null && leavingLines.Count > 0)
        {
            var clip = leavingLines[Random.Range(0, leavingLines.Count)];
            if (clip != null)
            {
                voiceSource.Stop();
                voiceSource.PlayOneShot(clip);
            }
        }

        if (leaveMoveDelay > 0f)
            yield return new WaitForSeconds(leaveMoveDelay);

        EnsureOnNavMesh();

        SetRunning(true);
        MoveTo(exitPoint);

        while (!HasArrived(exitPoint))
            yield return null;

        SetRunning(false);

        if (disableKidAfterExit)
            kidRoot.SetActive(false);
    }

    private void MoveTo(Transform target)
    {
        if (agent == null || !agent.enabled || target == null) return;

        agent.isStopped = false;
        agent.ResetPath();
        agent.SetDestination(target.position);
    }

    private bool HasArrived(Transform target)
    {
        if (target == null) return true;

        if (agent == null || !agent.enabled)
            return Vector3.Distance(kidRoot.transform.position, target.position) <= arriveDistance;

        if (agent.pathPending) return false;

        float stop = Mathf.Max(arriveDistance, agent.stoppingDistance + 0.05f);
        return agent.remainingDistance <= stop;
    }

    private void EnsureOnNavMesh()
    {
        if (!warpToNavMeshIfNeeded || agent == null || !agent.enabled) return;
        if (agent.isOnNavMesh) return;

        if (NavMesh.SamplePosition(agent.transform.position, out var hit, 2f, NavMesh.AllAreas))
            agent.Warp(hit.position);
    }

    private void SetRunning(bool running)
    {
        if (animator == null) return;
        if (string.IsNullOrWhiteSpace(isRunningBoolParam)) return;

        if (!_checkedAnim)
        {
            _checkedAnim = true;
            _hasIsRunningParam = HasParam(animator, isRunningBoolParam, AnimatorControllerParameterType.Bool);

            if (!_hasIsRunningParam && log)
                Debug.LogWarning("[KidFollowUpController] Animator missing bool param: " + isRunningBoolParam, this);
        }

        if (_hasIsRunningParam)
            animator.SetBool(isRunningBoolParam, running);
    }

    private static bool HasParam(Animator a, string name, AnimatorControllerParameterType type)
    {
        if (a == null || string.IsNullOrWhiteSpace(name)) return false;
        var ps = a.parameters;
        for (int i = 0; i < ps.Length; i++)
            if (ps[i].name == name && ps[i].type == type)
                return true;
        return false;
    }

    private static IEnumerator PlaySequential(AudioSource src, List<AudioClip> clips)
    {
        if (src == null || clips == null || clips.Count == 0)
            yield break;

        for (int i = 0; i < clips.Count; i++)
        {
            var c = clips[i];
            if (c == null) continue;

            src.Stop();
            src.PlayOneShot(c);
            yield return new WaitForSeconds(c.length);
        }
    }
}
