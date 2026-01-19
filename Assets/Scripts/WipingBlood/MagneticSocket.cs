using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

[DisallowMultipleComponent]
public sealed class MagneticSocketSnap : MonoBehaviour
{
    [Header("Socket")]
    [SerializeField] private XRSocketInteractor socket;

    [Header("Feel")]
    [Tooltip("Delay before we steal from hand (lets hover feel natural).")]
    [SerializeField, Min(0f)] private float snapDelay = 0.05f;

    [Tooltip("If true, steals pad even if it's currently grabbed.")]
    [SerializeField] private bool stealFromHand = true;

    [Header("Debug")]
    [SerializeField] private bool log;

    private IXRSelectInteractable _hovered;
    private Coroutine _snapRoutine;

    private void Awake()
    {
        if (socket == null) socket = GetComponent<XRSocketInteractor>();
    }

    private void OnEnable()
    {
        if (socket == null) return;

        socket.hoverEntered.AddListener(OnHoverEntered);
        socket.hoverExited.AddListener(OnHoverExited);
    }

    private void OnDisable()
    {
        if (socket == null) return;

        socket.hoverEntered.RemoveListener(OnHoverEntered);
        socket.hoverExited.RemoveListener(OnHoverExited);

        if (_snapRoutine != null) StopCoroutine(_snapRoutine);
        _snapRoutine = null;
        _hovered = null;
    }

    private void OnHoverEntered(HoverEnterEventArgs args)
    {
        _hovered = (IXRSelectInteractable)args.interactableObject; // IXRHoverInteractable, but also IXRSelectInteractable for grabbables
        if (_hovered == null) return;

        if (_snapRoutine != null) StopCoroutine(_snapRoutine);
        _snapRoutine = StartCoroutine(SnapAfterDelay(_hovered));
    }

    private void OnHoverExited(HoverExitEventArgs args)
    {
        if (args.interactableObject == _hovered)
        {
            _hovered = null;
            if (_snapRoutine != null) StopCoroutine(_snapRoutine);
            _snapRoutine = null;
        }
    }

    private IEnumerator SnapAfterDelay(IXRSelectInteractable target)
    {
        if (snapDelay > 0f)
            yield return new WaitForSeconds(snapDelay);

        if (target == null || target != _hovered)
            yield break;

        if (socket == null || !socket.isActiveAndEnabled)
            yield break;

        var mgr = socket.interactionManager;
        if (mgr == null)
            yield break;

        // Can this socket select it?
        if (!socket.CanSelect(target))
            yield break;

        // If grabbed by a hand, steal it
        if (target.isSelected)
        {
            var currentInteractor = target.firstInteractorSelecting;

            // Already in this socket?
            if ((XRSocketInteractor)currentInteractor == socket)
                yield break;

            if (stealFromHand && currentInteractor != null)
            {
                mgr.SelectExit(currentInteractor, target);
                yield return null; // one frame helps stability
            }
            else
            {
                yield break;
            }
        }

        mgr.SelectEnter(socket, target);

        if (log)
            Debug.Log($"[MagneticSocketSnap] Snapped {(target.transform != null ? target.transform.name : "Target")} into {socket.name}", this);
    }
}
