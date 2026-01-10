using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class PokePhoneSequence : MonoBehaviour
{
    public Transform pose1Target;
    public Transform pose2Target;
    public Renderer phoneRenderer;
    public Material step1Material;
    public Material step2Material;
    public float moveSpeed = 2f;

    private int step = 0;
    private Coroutine moveRoutine;

    // Hook this to XRSimpleInteractable -> Select Entered
    public void OnSelectEntered(SelectEnterEventArgs args)
    {
        // Only allow poke
        if (args.interactorObject is not XRPokeInteractor)
            return;

        Debug.Log("Poke detected → step: " + step);

        switch (step)
        {
            case 0:
                phoneRenderer.material = step1Material;
                StartMove(pose1Target);
                step = 1;
                break;

            case 1:
                phoneRenderer.material = step2Material;
                StartMove(pose2Target);
                step = 2;
                break;
        }
    }

    private void StartMove(Transform target)
    {
        if (moveRoutine != null)
            StopCoroutine(moveRoutine);

        moveRoutine = StartCoroutine(MoveTo(target));
    }

    private IEnumerator MoveTo(Transform target)
    {
        if (target == null)
        {
            Debug.LogWarning("Target is NULL!");
            yield break;
        }

        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * moveSpeed;
            transform.position = Vector3.Lerp(startPos, target.position, t);
            transform.rotation = Quaternion.Lerp(startRot, target.rotation, t);
            yield return null;
        }
    }
}