using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class PokePhoneSequence : MonoBehaviour
{
    public Transform pose1Target;
    public Transform pose2Target;
    public Renderer phoneRenderer;
    public Material step1Material;
    public Material step2Material;
    public float moveSpeed = 2f;

    private int step = 0;

    public void OnPoke()
    {
        Debug.Log("Poke detected → step: " + step);

        switch (step)
        {
            case 0:
                phoneRenderer.material = step1Material;
                StartCoroutine(MoveTo(pose1Target));
                step = 1;
                break;

            case 1:
                phoneRenderer.material = step2Material;
                StartCoroutine(MoveTo(pose2Target));
                step = 2;
                break;
        }
    }

    private System.Collections.IEnumerator MoveTo(Transform target)
    {
        if (target == null)
        {
            Debug.LogError("Target is NULL!");
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