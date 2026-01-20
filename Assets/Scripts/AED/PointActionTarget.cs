using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

[DisallowMultipleComponent]
public sealed class PointFlagMoveTarget : MonoBehaviour
{
    [Header("Scenario")]
    [SerializeField] private ScenarioDirector scenario;

    [Tooltip("Must be TRUE to allow interaction. Leave empty to allow anytime.")]
    [SerializeField] private string requiredFlag;

    [Tooltip("Raised when the player successfully points/holds on this target.")]
    [SerializeField] private string raiseFlag;

    [Tooltip("Delay before raising raiseFlag (sync dog leaving / scenario progress).")]
    [SerializeField] private float raiseFlagDelaySeconds = 0f;

    [Header("Behavior")]
    [SerializeField] private bool onlyOnce = true;

    [Header("Movement (Optional)")]
    [Tooltip("If null, will try to find a NavMeshAgent in this object or children.")]
    [SerializeField] private NavMeshAgent agent;

    [Tooltip("If assigned, agent will move here after the delay.")]
    [SerializeField] private Transform moveTo;

    [Tooltip("Wait this long after confirm before starting movement (for VO line time).")]
    [SerializeField] private float moveStartDelaySeconds = 1.5f;

    [Tooltip("If true, we warp the agent onto the NavMesh if it's slightly off.")]
    [SerializeField] private bool warpToNavMeshIfNeeded = true;

    [Tooltip("When close enough to moveTo, optionally disable the whole NPC.")]
    [SerializeField] private bool disableAfterArrive = false;

    [SerializeField] private float arriveDistance = 0.7f;

    [Header("Animator (Optional)")]
    [Tooltip("If null, will try to find an Animator in this object or children.")]
    [SerializeField] private Animator animator;

    [Tooltip("Bool parameter set TRUE while moving and FALSE when arrived. Leave empty to disable.")]
    [SerializeField] private string movingBoolParam = "IsWalking";

    [Tooltip("Optional float param for speed (blend trees). Leave empty to disable.")]
    [SerializeField] private string speedFloatParam = "";

    [Tooltip("Minimum speed before we consider this 'moving' for animation.")]
    [SerializeField] private float movingSpeedThreshold = 0.05f;

    [Header("Facing (Optional)")]
    [Tooltip("If assigned, we rotate this transform to face movement direction (use your visual root, not the agent root).")]
    [SerializeField] private Transform visualRootToRotate;

    [Tooltip("How fast to rotate toward movement direction.")]
    [SerializeField] private float turnSpeed = 720f;

    [Header("Audio (Optional)")]
    [Tooltip("If null, will try to find an AudioSource in this object or children.")]
    [SerializeField] private AudioSource audioSource;

    [SerializeField] private AudioClip confirmClip;

    [Tooltip("Delay before playing confirmClip after confirm.")]
    [SerializeField] private float audioDelaySeconds = 0.2f;

    [Header("Events")]
    public UnityEvent onConfirmed; // invoked right after confirmClip (if any)

    [Header("Debug")]
    [SerializeField] private bool log = false;

    private bool _done;
    private Coroutine _routine;
    private Coroutine _arriveRoutine;

    private void Awake()
    {
        if (scenario == null)
            scenario = FindFirstObjectByType<ScenarioDirector>();

        if (agent == null)
            agent = GetComponentInChildren<NavMeshAgent>(true);

        if (animator == null)
            animator = GetComponentInChildren<Animator>(true);

        if (audioSource == null)
            audioSource = GetComponentInChildren<AudioSource>(true);

        if (audioSource != null)
            audioSource.playOnAwake = false;

        // Ensure idle state at start
        SetMovingAnim(false);
    }

    private void Update()
    {
        // Optional: feed speed float continuously for blend trees
        if (animator != null && agent != null && agent.enabled && !string.IsNullOrWhiteSpace(speedFloatParam))
        {
            animator.SetFloat(speedFloatParam, agent.velocity.magnitude);
        }

        // Optional: rotate visual to face velocity direction (prevents moon-walking if rig is OK)
        if (visualRootToRotate != null && agent != null && agent.enabled)
        {
            Vector3 v = agent.velocity;
            v.y = 0f;

            if (v.sqrMagnitude > 0.0001f)
            {
                Quaternion target = Quaternion.LookRotation(v.normalized, Vector3.up);
                visualRootToRotate.rotation = Quaternion.RotateTowards(
                    visualRootToRotate.rotation,
                    target,
                    turnSpeed * Time.deltaTime
                );
            }
        }
    }

    public bool CanInteract()
    {
        if (_done && onlyOnce) return false;

        // If you ever run without scenario, allow it (handy for testing)
        if (scenario == null) return true;

        if (string.IsNullOrWhiteSpace(requiredFlag))
            return true;

        return scenario.HasFlag(requiredFlag.Trim());
    }

    public void Confirm()
    {
        if (!CanInteract())
        {
            if (log) Debug.Log($"[PointFlagMoveTarget] Blocked. Missing required flag: {requiredFlag}", this);
            return;
        }

        if (_done && onlyOnce) return;
        _done = true;

        if (_routine != null) StopCoroutine(_routine);
        _routine = StartCoroutine(ConfirmRoutine());
    }

    private IEnumerator ConfirmRoutine()
    {
        // Optional: play confirm clip
        if (audioSource != null && confirmClip != null)
        {
            if (audioDelaySeconds > 0f)
                yield return new WaitForSeconds(audioDelaySeconds);

            audioSource.PlayOneShot(confirmClip);
        }

        // Invoke events right after confirm (good for calling other scripts)
        onConfirmed?.Invoke();

        // Raise flag (optional)
        if (!string.IsNullOrWhiteSpace(raiseFlag) && scenario != null)
        {
            if (raiseFlagDelaySeconds > 0f)
                yield return new WaitForSeconds(raiseFlagDelaySeconds);

            scenario.RaiseFlag(raiseFlag.Trim());
            if (log) Debug.Log($"[PointFlagMoveTarget] Raised flag: {raiseFlag} (after {raiseFlagDelaySeconds:F2}s)", this);
        }

        // Wait before this NPC starts moving (optional)
        if (moveStartDelaySeconds > 0f)
            yield return new WaitForSeconds(moveStartDelaySeconds);

        // Move after wait
        if (agent != null && agent.enabled && moveTo != null)
        {
            EnsureOnNavMesh();

            agent.isStopped = false;
            agent.ResetPath();

            bool ok = agent.SetDestination(moveTo.position);
            if (log) Debug.Log($"[PointFlagMoveTarget] Move start -> {moveTo.name}, ok={ok}, onNavMesh={agent.isOnNavMesh}", this);

            // Start moving animation immediately
            SetMovingAnim(true);

            // Stop moving animation when arrived (and optionally disable)
            if (_arriveRoutine != null) StopCoroutine(_arriveRoutine);
            _arriveRoutine = StartCoroutine(ArriveWatcherRoutine(disableAfterArrive));
        }

        _routine = null;
    }

    private IEnumerator ArriveWatcherRoutine(bool disableOnArrive)
    {
        // Wait until we arrived (or agent gets disabled)
        while (agent != null && agent.enabled && moveTo != null)
        {
            if (!agent.pathPending)
            {
                float stop = Mathf.Max(arriveDistance, agent.stoppingDistance + 0.05f);

                // Consider velocity too (prevents stuck “moving true” at destination)
                bool closeEnough = agent.remainingDistance <= stop;
                bool slowEnough = agent.velocity.magnitude <= movingSpeedThreshold;

                if (closeEnough && slowEnough)
                    break;
            }

            yield return null;
        }

        SetMovingAnim(false);

        if (disableOnArrive)
            gameObject.SetActive(false);

        _arriveRoutine = null;
    }

    private void SetMovingAnim(bool moving)
    {
        if (animator == null) return;
        if (string.IsNullOrWhiteSpace(movingBoolParam)) return;

        animator.SetBool(movingBoolParam, moving);
    }

    private void EnsureOnNavMesh()
    {
        if (!warpToNavMeshIfNeeded || agent == null) return;
        if (agent.isOnNavMesh) return;

        if (NavMesh.SamplePosition(agent.transform.position, out var hit, 2f, NavMesh.AllAreas))
            agent.Warp(hit.position);
    }
}
